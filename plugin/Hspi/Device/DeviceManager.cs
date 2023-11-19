using System;
using System.IO;
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

        private async Task UpdateDeviceData()
        {
            string tokenPath = Path.Combine(PlugInData.HomeSeerDirectory, "Data", PlugInData.PlugInId, "token.txt");

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
                await client.GetInventoryAsync(CancellationToken)
                            .ContinueWith((dataTask) => mainDevice.Update(dataTask.Result)).ConfigureAwait(false);
                await client.GetV1InvertersAsync(CancellationToken)
                            .ContinueWith((dataTask) => invertersDevice.Update(dataTask.Result)).ConfigureAwait(false);

                await Task.Delay(envoySettings.RefreshInterval, CancellationToken).ConfigureAwait(false);
            }
        }

        private (MainDevice, InvertersDevice) SetupDevices(IHsController hs)
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
                        mainDevice2 = new MainDevice(hs, refId, CancellationToken);
                    }
                    else if (deviceType == DeviceAndFeatureType.InvertersDevice)
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
                mainDevice2 = new MainDevice(hs, refId, CancellationToken);
            }

            if (invertersDevice2 == null)
            {
                int refId = InvertersDevice.CreateDevice(hs);
                invertersDevice2 = new InvertersDevice(hs, refId, CancellationToken);
            }

            return (mainDevice2, invertersDevice2);
        }

        public void Dispose()
        {
            cancellationTokenSource?.Dispose();
        }

        public static async Task<EnvoyClient> GetClientAsync(EnvoyConnectionInfo envoyConnectionInfo, string tokenfile, CancellationToken cancellationToken = default)
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

        private readonly IEnvoySettings envoySettings;
        private readonly MainDevice mainDevice;
        private readonly InvertersDevice invertersDevice;
        private readonly CancellationTokenSource cancellationTokenSource;
        private CancellationToken CancellationToken => cancellationTokenSource.Token;
    }
}