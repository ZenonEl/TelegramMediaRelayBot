// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Interfaces;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;

public class ArgumentBuilder : IArgumentBuilder
{
    private readonly ICredentialProvider _credentialProvider;
    private static readonly Regex _tokenRegex = new Regex(@"\{(?<token>[a-zA-Z0-9:_]+)\}", RegexOptions.Compiled);

    public ArgumentBuilder(ICredentialProvider credentialProvider)
    {
        _credentialProvider = credentialProvider;
    }

    public List<string> Build(
        List<string> templateList,
        ArgumentBuilderContext context,
        AuthenticationConfig? authConfig = null)
    {
        List<string> finalArgs = new List<string>();
        Dictionary<string, string?> values = PrepareValues(context, authConfig);

        for (int i = 0; i < templateList.Count; i++)
        {
            string template = templateList[i];
            if (string.IsNullOrWhiteSpace(template)) continue;

            MatchCollection matches = _tokenRegex.Matches(template);

            if (matches.Count == 0)
            {
                finalArgs.Add(template);
                continue;
            }

            string processedString = template;
            bool shouldDropArgument = false;

            foreach (Match match in matches)
            {
                string tokenKey = match.Groups["token"].Value;

                if (values.TryGetValue(tokenKey, out string? replacementValue) && !string.IsNullOrEmpty(replacementValue))
                {
                    processedString = processedString.Replace(match.Value, replacementValue);
                }
                else
                {
                    shouldDropArgument = true;
                    break;
                }
            }

            if (!shouldDropArgument)
            {
                finalArgs.Add(processedString);
            }
            else
            {
                Log.Debug("Dropping argument '{Template}' because token value is missing.", template);
                if (finalArgs.Count > 0)
                {
                    string lastArg = finalArgs[finalArgs.Count - 1];
                    if (lastArg == "--proxy" || lastArg == "--cookies" || lastArg == "--username" || lastArg == "--password")
                    {
                        finalArgs.RemoveAt(finalArgs.Count - 1);
                        Log.Debug("Also dropping orphaned flag '{Flag}'", lastArg);
                    }
                }
            }
        }

        return finalArgs;
    }

    private Dictionary<string, string?> PrepareValues(ArgumentBuilderContext context, AuthenticationConfig? authConfig)
    {
        Dictionary<string, string?> dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            { "Url", context.Url },
            { "OutputPath", context.OutputPath },
            { "Format", context.FormatSelection },
            { "Proxy", context.ProxyAddress },
            { "CookiesPath", context.CookiesPath }
        };

        if (authConfig != null)
        {
            string? cookiePath = _credentialProvider.GetCookieFilePath(authConfig.CookieFile);
            dict["Auth:CookiePath"] = cookiePath;
            if (string.IsNullOrEmpty(dict["CookiesPath"]))
            {
                dict["CookiesPath"] = cookiePath;
            }

            dict["Auth:User"] = _credentialProvider.ResolveSecret(authConfig.Username);
            dict["Auth:Password"] = _credentialProvider.ResolveSecret(authConfig.Password);
            dict["Auth:ApiToken"] = _credentialProvider.ResolveSecret(authConfig.ApiToken);
        }

        return dict;
    }
}
