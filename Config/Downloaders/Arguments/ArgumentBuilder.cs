namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;

public class ArgumentBuilder : IArgumentBuilder
{
    public List<string> Build(List<string> templateList, ArgumentBuilderContext context)
    {
        var finalArgs = new List<string>();

        for (int i = 0; i < templateList.Count; i++)
        {
            string currentToken = templateList[i];

            if (currentToken == "--proxy" || currentToken == "--cookies" || currentToken == "--output" || currentToken == "--user-agent")
            {
                string valueToken = (i + 1 < templateList.Count) ? templateList[i + 1] : string.Empty;
                string value = GetValueForToken(valueToken, context);
                Log.Debug("Value for token {Token}: {Value}", currentToken, value);

                if (!string.IsNullOrEmpty(value))
                {
                    finalArgs.Add(currentToken);
                    finalArgs.Add(value);
                }

                i++;
            }
            else
            {
                string value = GetValueForToken(currentToken, context);
                if (!string.IsNullOrEmpty(value))
                {
                    finalArgs.Add(value);
                }
            }
        }

        Log.Debug("Final arguments: {Args}", string.Join(" ", finalArgs));
        return finalArgs;
    }

    private string GetValueForToken(string token, ArgumentBuilderContext context)
    {
        return token switch
        {
            "{Url}" => context.Url,
            "{OutputPath}/video.%(ext)s" => context.OutputPath+"/video.%(ext)s",
            "{OutputPath}" => context.OutputPath,
            "{Proxy}" => context.ProxyAddress ?? string.Empty,
            "{CookiesPath}" => context.CookiesPath ?? string.Empty,
            "{Format}" => context.FormatSelection ?? string.Empty,
            _ => token
        };
    }
}