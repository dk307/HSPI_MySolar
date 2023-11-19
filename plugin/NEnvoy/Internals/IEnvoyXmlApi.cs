using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NEnvoy.Models;
using Refit;

#nullable enable

namespace NEnvoy.Internals;

public interface IEnvoyXmlApi
{
    [Get("/info.xml")]
    Task<EnvoyInfo> GetEnvoyInfoAsync(CancellationToken cancellationToken = default);
}