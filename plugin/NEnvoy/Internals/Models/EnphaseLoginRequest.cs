using Refit;

#nullable enable

namespace NEnvoy.Internals.Models;

internal record EnphaseLoginRequest
(
    [property: AliasAs("user[email]")] string Username,
    [property: AliasAs("user[password]")] string Password
);