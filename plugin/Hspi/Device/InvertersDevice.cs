using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Humanizer;
using NEnvoy.Models;
using Serilog;
using static System.FormattableString;

#nullable enable

namespace Hspi.Device

{
    public sealed class InvertersDevice : BaseHsDevice
    {
        public InvertersDevice(IHsController hs,
                               int deviceRefId,
                               CancellationToken cancellationToken)
            : base(hs, deviceRefId, cancellationToken)
        {
            var children = (HashSet<int>)hs.GetPropertyByRef(deviceRefId, EProperty.AssociatedDevices);

            Dictionary<string, int> features = new();

            foreach (var childRefId in children)
            {
                var childrenFeatureType = GetType(hs, childRefId);
                if (childrenFeatureType == DeviceAndFeatureType.InverterCurrentProduction)
                {
                    var serial = GetSerialNumber(hs, childRefId);
                    features.Add(serial, childRefId);
                }
            }

            deviceFeatures = features.ToImmutableDictionary();
        }

        public const string SerialKey = "serial-number";

        public static string GetSerialNumber(IHsController hsController, int refId)
        {
            if (hsController.GetPropertyByRef(refId, EProperty.PlugExtraData) is not PlugExtraData plugInExtra)
            {
                throw new HsDeviceInvalidException("PlugExtraData is null");
            }

            if (!plugInExtra.ContainsNamed(SerialKey))
            {
                throw new HsDeviceInvalidException(Invariant($"{TypeKey} type not found in {refId}"));
            }

            return plugInExtra.GetNamed(SerialKey);
        }

        public static int CreateDevice(IHsController hsController)
        {
            var newDeviceData = DeviceFactory.CreateDevice(PlugInData.PlugInId)
                                             .WithName("Inverters")
                                             .WithLocation("Solar")
                                             .WithLocation2("Solar")
                                             .WithExtraData(CreatePlugInExtraData(DeviceAndFeatureType.InvertersDevice))
                                             .PrepareForHs();

            var newDeviceRefId = hsController.CreateDevice(newDeviceData);
            Log.Information("Created Inverters device:{id}", newDeviceRefId);

            return newDeviceRefId;
        }

        private static int CreateFeature(IHsController hsController,
                                         int newDevice,
                                         string serialNumber)
        {
            string name = DeviceAndFeatureType.InverterCurrentProduction.ToString().Humanize() + " - " + serialNumber;
            PlugExtraData extraData = CreatePlugInExtraData(DeviceAndFeatureType.InverterCurrentProduction);
            extraData.AddNamed(SerialKey, serialNumber);
            var newFeatureData = FeatureFactory.CreateFeature(PlugInData.PlugInId)
                                               .WithName(name)
                                               .WithLocation("Solar")
                                               .WithLocation2("Solar")
                                               .WithMiscFlags(EMiscFlag.StatusOnly)
                                               .WithMiscFlags(EMiscFlag.SetDoesNotChangeLastChange)
                                               .WithExtraData(extraData)
                                               .PrepareForHsDevice(newDevice);

            AddStatusControls(newFeatureData, DeviceAndFeatureType.InverterCurrentProduction);

            return hsController.CreateFeatureForDevice(newFeatureData);
        }

        public void Update(IEnumerable<V1Inverter> result)
        {
            foreach (var v1 in result)
            {
                if (deviceFeatures.TryGetValue(v1.Serial, out var childRefId))
                {
                    UpdateDeviceValue(childRefId, v1.LastReportedWatts, v1.LastReportDate);
                }
                else
                {
                    int newchildRefId = CreateFeature(this.HS, DeviceRefId, v1.Serial);
                    if (ImmutableInterlocked.TryAdd(ref deviceFeatures, v1.Serial, newchildRefId))
                    {
                        UpdateDeviceValue(newchildRefId, v1.LastReportedWatts, v1.LastReportDate);
                    }
                    else
                    {
                        HS.DeleteFeature(newchildRefId);
                    }
                }
            }
        }

        private ImmutableDictionary<string, int> deviceFeatures;
    }
}