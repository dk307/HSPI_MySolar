#nullable enable

using System;

namespace Hspi
{
    internal interface IEnvoySettings
    {
        string? EnvoyHost { get; }
        bool InvertersWattsEnabled { get; }
        bool LifetimeKwhEnabled { get; }
        string? Password { get; }
        TimeSpan RefreshInterval { get; }
        bool SevenDaysKwhEnabled { get; }
        string? UserName { get; }
    }
}