using System.Threading;
using System.Threading.Tasks;
using NEnvoy.Internals.Models;
using Refit;

#nullable enable

namespace NEnvoy.Internals;

internal interface IEntrezEnphase
{
    [Post("/tokens")]
    Task<string> RequestTokenAsync(EnphaseTokenRequest tokenRequest, CancellationToken cancellationToken = default);
}