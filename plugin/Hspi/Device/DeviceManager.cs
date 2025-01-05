using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HomeSeer.PluginSdk;
using Hspi.NEnvoy;
using Hspi.Utils;
using Serilog;

#nullable enable

namespace Hspi.Device
{
    internal sealed class DeviceManager : IDisposable
    {
        public DeviceManager(IHsController hs,
                             IEnvoySettings envoySettings,
                             CancellationToken cancellationToken)
        {
            this.envoySettings = envoySettings;
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            (this.mainDevice, this.invertersDevice) = SetupDevices(hs);

            Utils.TaskHelper.StartAsyncWithErrorChecking("Envoy update", UpdateDeviceData, cancellationToken, TimeSpan.FromSeconds(15));
        }

        public Exception? LastError { get; private set; }

        private CancellationToken CancellationToken => cancellationTokenSource.Token;

        public void Dispose()
        {
            cancellationTokenSource?.Dispose();
        }

        private static async Task<EnvoyClient> GetClientAsync(EnvoyConnectionInfo envoyConnectionInfo, string tokenfile, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(tokenfile))
            {
                Log.Information("Creating new token from login");
                var client = await EnvoyClient.FromLoginAsync(envoyConnectionInfo, cancellationToken).ConfigureAwait(false);

                string parentDir = Path.GetDirectoryName(tokenfile);
                if (!Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                File.WriteAllText(tokenfile, client.GetToken());
                return client;
            }

            string token = File.ReadAllText(tokenfile);
            return EnvoyClient.FromToken(token, envoyConnectionInfo);
        }

        private (MainDevice, InvertersDevice?) SetupDevices(IHsController hs)
        {
            MainDevice? mainDevice2 = null;
            InvertersDevice? invertersDevice2 = null;
            var refDeviceIds = hs.GetRefsByInterface(PlugInData.PlugInId, true);
            foreach (var refId in refDeviceIds)
            {
                CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var deviceType = BaseHsDevice.GetType(hs, refId);
                    if (deviceType == DeviceAndFeatureType.Main)
                    {
                        mainDevice2 = new MainDevice(hs, refId,
                                                     this.envoySettings.SevenDaysKwhEnabled,
                                                     this.envoySettings.LifetimeKwhEnabled,
                                                     this.envoySettings.InverterStatusEnabled,
                                                     CancellationToken);
                    }
                    else if ((envoySettings.InvertersWattsEnabled) && (deviceType == DeviceAndFeatureType.InvertersDevice))
                    {
                        invertersDevice2 = new InvertersDevice(hs, refId, CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("{id} has invalid plugin data. Load failed with {error}. Please recreate it.", refId, ex.GetFullMessage());
                }
            }

            if (mainDevice2 == null)
            {
                int refId = MainDevice.CreateDevice(hs);
                mainDevice2 = new MainDevice(hs, refId,
                                             this.envoySettings.SevenDaysKwhEnabled,
                                             this.envoySettings.LifetimeKwhEnabled,
                                             envoySettings.InverterStatusEnabled,
                                             CancellationToken);
            }

            if ((envoySettings.InvertersWattsEnabled) && (invertersDevice2 == null))
            {
                int refId = InvertersDevice.CreateDevice(hs);
                invertersDevice2 = new InvertersDevice(hs, refId, CancellationToken);
            }

            return (mainDevice2, invertersDevice2);
        }

        private async Task UpdateDeviceData()
        {
            string tokenPath = Path.Combine(PlugInData.HomeSeerDirectory, "Data", PlugInData.PlugInId, "token.txt");
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    EnvoyConnectionInfo envoyConnectionInfo = new()
                    {
                        Username = envoySettings.UserName ?? string.Empty,
                        Password = envoySettings.Password ?? string.Empty,
                        EnvoyHost = envoySettings.EnvoyHost ?? string.Empty,
                    };

                    var client = await GetClientAsync(envoyConnectionInfo, tokenPath, CancellationToken).ConfigureAwait(false);

                    while (!CancellationToken.IsCancellationRequested)
                    {
                        await client.GetProductionAsync(CancellationToken)
                                    .ContinueWith((dataTask) => mainDevice.Update(dataTask.Result)).ConfigureAwait(false);

                        if (envoySettings.InverterStatusEnabled)
                        {
                            await client.GetInventoryAsync(CancellationToken)
                                        .ContinueWith((dataTask) => mainDevice.Update(dataTask.Result)).ConfigureAwait(false);
                        }

                        if (invertersDevice != null)
                        {
                            await client.GetV1InvertersAsync(CancellationToken)
                                        .ContinueWith((dataTask) => invertersDevice.Update(dataTask.Result)).ConfigureAwait(false);
                        }

                        this.LastError = null;
                        await Task.Delay(envoySettings.RefreshInterval, CancellationToken).ConfigureAwait(false);
                    }
                }
                catch (System.AggregateException ex)
                {
                    if (ex.InnerException is HttpRequestException httpException &&
                        httpException.Message.Contains("401") &&
                        System.IO.File.Exists(tokenPath))
                    {
                        Log.Information("Deleting token file.");
                        System.IO.File.Delete(tokenPath);
                        continue;
                    }
                    this.LastError = ex;
                    throw;
                }
                catch (Exception ex)
                {
                    this.LastError = ex;
                    throw;
                }
            }
        }

        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly IEnvoySettings envoySettings;
        private readonly InvertersDevice? invertersDevice;
        private readonly MainDevice mainDevice;
    }
}