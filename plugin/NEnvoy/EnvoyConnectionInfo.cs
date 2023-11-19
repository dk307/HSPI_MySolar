using System;

#nullable enable

namespace Hspi.NEnvoy
{
    public sealed record EnvoyConnectionInfo
    {
        public const string DefaultEnphaseBaseUri = "https://enlighten.enphaseenergy.com";
        public const string DefaultEntrezBaseUri = "https://entrez.enphaseenergy.com";

        public const string DefaultHost = "envoy";
        public static readonly TimeSpan DefaultSessionTimeout = TimeSpan.FromMinutes(5);

        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string EnvoyHost { get; init; } = DefaultHost;

        public readonly string EnphaseBaseUri = DefaultEnphaseBaseUri;
        public readonly string EnphaseEntrezBaseUri = DefaultEntrezBaseUri;
        public TimeSpan SessionTimeout { get; init; } = DefaultSessionTimeout;
    }
}