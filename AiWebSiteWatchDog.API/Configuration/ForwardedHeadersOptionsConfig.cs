using System.Collections.Generic;

namespace AiWebSiteWatchDog.API.Configuration
{
    public class ForwardedHeadersOptionsConfig
    {
        public bool Enabled { get; set; } = true; // can be disabled entirely
        public int? ForwardLimit { get; set; } = 1; // how many proxy hops to trust
        public List<string> KnownProxies { get; set; } = new(); // IPv4/IPv6 literal strings
        public List<string> KnownNetworks { get; set; } = new(); // CIDR strings like "10.0.0.0/8"
    }
}
