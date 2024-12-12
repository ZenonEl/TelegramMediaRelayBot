using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using TikTokMediaRelayBot;

namespace TikTokMediaRelayBot.SitesConfig
{
    public class SiteConfig
    {
        public DefaultsConfig Defaults { get; set; }
        public List<Site> Sites { get; set; }
    }

public class DefaultsConfig
{
    public List<ElementAction> elements_path { get; set; }
}

    public class Site
    {
        public string Name { get; set; }
        public List<Getter> Getters { get; set; }
    }

    public class Getter
    {
        public List<string> Patterns { get; set; }
        public List<ElementAction> elements_path { get; set; }
    }

    public class SitesConfig
    {
        public static List<string> Domains { get; private set; } = new List<string>();
        public static Dictionary<string, Getter> DomainToGetter { get; private set; } = new Dictionary<string, Getter>();

        public static void LoadConfig()
        {
            var yaml = File.ReadAllText("sitessettings.yaml");

            var deserializer = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .Build();

            var config = deserializer.Deserialize<SiteConfig>(yaml);

            if (config.Defaults == null || config.Defaults.elements_path == null)
            {
                throw new Exception("Defaults.elements_path is missing in the configuration.");
            }

            foreach (var site in config.Sites)
            {
                foreach (var getter in site.Getters)
                {
                    if (getter.Patterns == null || getter.elements_path == null)
                    {
                        continue;
                    }

                    foreach (var pattern in getter.Patterns)
                    {
                        string domain = ExtractDomainFromPattern(pattern);
                        Domains.Add(domain);
                        DomainToGetter[domain] = getter;
                    }
                }
            }
        }

        private static string ExtractDomainFromPattern(string pattern)
        {
            return new Uri(pattern).Host;
        }

        public static Getter GetGetterByDomain(string domain)
        {
            if (DomainToGetter.ContainsKey(domain))
            {
                return DomainToGetter[domain];
            }
            return null;
        }
    }
}