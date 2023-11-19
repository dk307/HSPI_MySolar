#nullable enable

using System;

namespace Hspi
{
    internal interface IEnvoySettings
    {
        string? EnvoyHost { get; }
        string? Password { get; }
        string? UserName { get; }

        TimeSpan RefreshInterval { get; }
    }
}