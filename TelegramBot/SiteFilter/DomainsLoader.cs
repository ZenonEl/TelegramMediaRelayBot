// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Net;

namespace TelegramMediaRelayBot.TelegramBot.SiteFilter;

public interface IDomainsLoader
{
    public HashSet<string> LoadDomainsFromFile(string filePath);
}

public class DomainsLoader : IDomainsLoader
{
    public HashSet<string> LoadDomainsFromFile(string filePath)
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!System.IO.File.Exists(filePath))
            return domains;

        foreach (var line in System.IO.File.ReadLines(filePath))
        {
            string cleanLine = line.Trim();

            if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith("#"))
                continue;

            string[] parts = cleanLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1)
            {
                string possibleDomain = parts[parts.Length - 1];

                if (IsValidDomain(possibleDomain))
                {
                    domains.Add(possibleDomain);
                }
            }
        }

        return domains;
    }

    private bool IsValidDomain(string domain)
    {
        return domain.Contains(".") &&
            !IPAddress.TryParse(domain, out _);
    }
}
