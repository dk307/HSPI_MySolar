using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NEnvoy.Internals;
using NEnvoy.Internals.Models;
using NEnvoy.Models;
using Refit;
using Serilog.Debugging;

#nullable enable

namespace Hspi.NEnvoy
{
    public class EnvoyClient

    {
        public EnvoyClient(IEnvoyXmlApi envoyXmlClient, IEnvoyJsonApi envoyJsonClient, EnvoySession session)
        {
            this.envoyXmlClient = envoyXmlClient;
            this.envoyJsonClient = envoyJsonClient;
            this.session = session;
        }

        public string GetToken() => (session ?? throw new NoActiveSessionException()).Token;

        public static async Task<EnvoyClient> FromLoginAsync(EnvoyConnectionInfo connectionInfo,
                                                             CancellationToken cancellationToken = default)
        {
            var baseAddress = CreateBaseUri(connectionInfo.EnvoyHost);
            var envoyxmlclient = GetEnvoyXmlClient(baseAddress);
            var envoyInfo = await GetEnvoyInfoAsync(envoyxmlclient, cancellationToken).ConfigureAwait(false);

            var enphaseclient = RestService.For<IEnphase>(connectionInfo.EnphaseBaseUri);

            EnphaseLoginRequest login = new(connectionInfo.Username, connectionInfo.Password);
            var loginresult = await enphaseclient.LoginAsync(login, cancellationToken).ConfigureAwait(false);
            if (string.Equals(loginresult.Message, "success", StringComparison.OrdinalIgnoreCase))
            {
                var entrezclient = RestService.For<IEntrezEnphase>(connectionInfo.EnphaseEntrezBaseUri);
                var token = await entrezclient.RequestTokenAsync(new EnphaseTokenRequest(loginresult.SessionId, envoyInfo.Device.Serial, connectionInfo.Username), cancellationToken).ConfigureAwait(false);

                var session2 = EnvoySession.Create(baseAddress, connectionInfo.SessionTimeout, token);
                var envoyjsonclient = GetEnvoyJsonClient(baseAddress, session2);

                return new EnvoyClient(envoyxmlclient, envoyjsonclient, session2);
            }

            throw new LoginFailedException(loginresult.Message);
        }

        public Task<ProductionData> GetProductionAsync(CancellationToken cancellationToken = default)
                => envoyJsonClient.GetProductionAsync(cancellationToken);

        public Task<IEnumerable<InventoryItem>> GetInventoryAsync(CancellationToken cancellationToken = default)
            => envoyJsonClient.GetInventoryAsync(cancellationToken);

        public Task<IEnumerable<V1Inverter>> GetV1InvertersAsync(CancellationToken cancellationToken = default)
            => envoyJsonClient.GetV1InvertersAsync(cancellationToken);

        public static EnvoyClient FromToken(string token, EnvoyConnectionInfo connectionInfo)
        {
            var baseuri = CreateBaseUri(connectionInfo.EnvoyHost);
            var session2 = EnvoySession.Create(baseuri, connectionInfo.SessionTimeout, token);
            var envoyjsonclient = GetEnvoyJsonClient(baseuri, session2);
            return new(GetEnvoyXmlClient(baseuri), envoyjsonclient, session2);
        }

        private static Uri CreateBaseUri(string host) => new($"https://{host}");

        private static Task<EnvoyInfo> GetEnvoyInfoAsync(IEnvoyXmlApi envoyClient, CancellationToken cancellationToken = default)
                => envoyClient.GetEnvoyInfoAsync(cancellationToken);

        private static IEnvoyJsonApi GetEnvoyJsonClient(Uri baseAddress, EnvoySession session)
            => RestService.For<IEnvoyJsonApi>(GetUnsafeClient(baseAddress, session));

        private static IEnvoyXmlApi GetEnvoyXmlClient(Uri baseAddress)
                                                            => RestService.For<IEnvoyXmlApi>(GetUnsafeClient(baseAddress), new RefitSettings
                                                            {
                                                                ContentSerializer = new XmlContentSerializer()
                                                            });

        private static HttpClient GetUnsafeClient(Uri baseAddress, EnvoySession? session = null)
        {
            var handler = new EnvoyHttpClientHandler(session)
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslErrors) => true,
            };

            if (session?.CookieContainer != null)
            {
                handler.CookieContainer = session.CookieContainer;
                handler.UseCookies = true;
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = baseAddress
            };

            return client;
        }

        private sealed class EnvoyHttpClientHandler : HttpClientHandler
        {
            public EnvoyHttpClientHandler(EnvoySession? session)
                => _session = session;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_session != null)
                {
                    if (_session.Expired)
                    {
                        var authrequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_session.BaseAddress, "/auth/check_jwt"));
                        authrequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
                        (await base.SendAsync(authrequest, cancellationToken).ConfigureAwait(false)).EnsureSuccessStatusCode();
                    }

                    _session.UpdateLastRequest();
                }

                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            private readonly EnvoySession? _session;
        }

        private readonly IEnvoyJsonApi envoyJsonClient;
        private readonly IEnvoyXmlApi envoyXmlClient;
        private readonly EnvoySession session;
    }
}