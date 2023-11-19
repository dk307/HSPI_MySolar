using System.Threading;
using System.Threading.Tasks;
using NEnvoy.Internals.Models;
using Refit;

namespace NEnvoy.Internals;

#nullable enable

internal interface IEnphase
{
    [Post("/login/login.json")]
    Task<EnphaseLoginResponse> LoginAsync([Body(BodySerializationMethod.UrlEncoded)] EnphaseLoginRequest login, CancellationToken cancellationToken = default);
}