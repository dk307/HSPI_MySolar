using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Humanizer;
using NEnvoy.Models;
using Serilog;

#nullable enable

namespace Hspi.Device

{
    public sealed class MainDevice : BaseHsDevice
    {
        public MainDevice(IHsController hs,
                          int deviceRefId,
                          CancellationToken cancellationToken)
            : base(hs, deviceRefId, cancellationToken)
        {
            var children = (HashSet<int>)hs.GetPropertyByRef(deviceRefId, EProperty.AssociatedDevices);

            Dictionary<DeviceAndFeatureType, int> features = new();

            foreach (var childRefId in children)
            {
                var childrenFeatureType = GetType(hs, childRefId);
                if (childrenFeatureType != null)
                {
                    features.Add(childrenFeatureType.Value, childRefId);
                }
            }

            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.Production);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.Consumption);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.NetConsumption);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.TotalMicroInverters);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.MicroInvertersOperating);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.MicroInvertersCommunicating);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.ProducedToday);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.ConsumptionToday);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.Produced7Days);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.Consumption7Days);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.ProducedLifetime);
            CheckFeatureTypeExists(hs, features, deviceRefId, DeviceAndFeatureType.ConsumptionLifetime);

            deviceFeatures = features.ToImmutableDictionary();

            static void CheckFeatureTypeExists(IHsController hs, IDictionary<DeviceAndFeatureType, int> features,
                                               int deviceRefId,
                                               DeviceAndFeatureType featureType)
            {
                if (!features.ContainsKey(featureType))
                {
                    var featureId = CreateFeature(hs, deviceRefId, featureType);
                    features[featureType] = featureId;
                }
            }
        }

        public static int CreateDevice(IHsController hsController)
        {
            var newDeviceData = DeviceFactory.CreateDevice(PlugInData.PlugInId)
                                             .WithName("Envoy")
                                             .WithLocation("Solar")
                                             .WithLocation2("Solar")
                                             .WithExtraData(CreatePlugInExtraData(DeviceAndFeatureType.Main))
                                             .PrepareForHs();

            var newDeviceRefId = hsController.CreateDevice(newDeviceData);
            Log.Information("Created Main device:{id}", newDeviceRefId);

            return newDeviceRefId;
        }

        public void Update(ProductionData data)
        {
            var totalConsumption = data.Consumption.OfType<EIM>().FirstOrDefault(x => x.MeasurementType == "total-consumption");
            var netConsumption = data.Consumption.OfType<EIM>().FirstOrDefault(x => x.MeasurementType == "net-consumption");
            var production = data.Production.OfType<EIM>().FirstOrDefault(x => x.MeasurementType == "production");

            UpdateWattsNowValue(totalConsumption, DeviceAndFeatureType.Consumption);
            UpdateWattsNowValue(netConsumption, DeviceAndFeatureType.NetConsumption);
            UpdateWattsNowValue(production, DeviceAndFeatureType.Production);

            UpdateTodayValue(totalConsumption, DeviceAndFeatureType.ConsumptionToday);
            UpdateTodayValue(production, DeviceAndFeatureType.ProducedToday);

            Update7DaysValue(totalConsumption, DeviceAndFeatureType.Consumption7Days);
            Update7DaysValue(production, DeviceAndFeatureType.Produced7Days);

            UpdateLifetimeValue(totalConsumption, DeviceAndFeatureType.ConsumptionLifetime);
            UpdateLifetimeValue(production, DeviceAndFeatureType.ProducedLifetime);

            void UpdateWattsNowValue(EIM value, DeviceAndFeatureType type)
            {
                if (value != null)
                {
                    int refId = deviceFeatures[type];
                    UpdateDeviceValue(refId, value.WattsNow / 1000, value.ReadingTime);
                }
                else
                {
                    int refId = deviceFeatures[DeviceAndFeatureType.Consumption];
                    UpdateDeviceValue(refId, null);
                }
            }

            void UpdateTodayValue(EIM value, DeviceAndFeatureType type)
            {
                if (value != null)
                {
                    int refId = deviceFeatures[type];
                    UpdateDeviceValue(refId, value.WattHoursToday / 1000, value.ReadingTime);
                }
            }

            void Update7DaysValue(EIM value, DeviceAndFeatureType type)
            {
                if (value != null)
                {
                    int refId = deviceFeatures[type];
                    UpdateDeviceValue(refId, value.WattHoursLastSevenDays / 1000, value.ReadingTime);
                }
            }

            void UpdateLifetimeValue(EIM value, DeviceAndFeatureType type)
            {
                if (value != null)
                {
                    int refId = deviceFeatures[type];
                    UpdateDeviceValue(refId, value.WattHoursLifeTime / 1000, value.ReadingTime);
                }
            }
        }

        public void Update(IEnumerable<InventoryItem> inventory)
        {
            int? total = null;
            int? communicating = null;
            int? operating = null;

            var inverters = inventory.FirstOrDefault(x => x.Type == "PCU")?.Devices;

            if (inverters != null)
            {
                total = inverters.Count;
                communicating = inverters.Count(x => x.Communicating);
                operating = inverters.Count(x => x.Operating);
            }

            UpdateValue(total, DeviceAndFeatureType.TotalMicroInverters);
            UpdateValue(communicating, DeviceAndFeatureType.MicroInvertersCommunicating);
            UpdateValue(operating, DeviceAndFeatureType.MicroInvertersOperating);

            void UpdateValue(int? count, DeviceAndFeatureType type)
            {
                if (count != null)
                {
                    int refId = deviceFeatures[type];
                    UpdateDeviceValue(refId, count);
                }
                else
                {
                    int refId = deviceFeatures[DeviceAndFeatureType.Consumption];
                    UpdateDeviceValue(refId, null);
                }
            }
        }

        private static int CreateFeature(IHsController hsController, int newDevice, DeviceAndFeatureType featureType)
        {
            var newFeatureData = FeatureFactory.CreateFeature(PlugInData.PlugInId)
                                               .WithName(featureType.ToString().Humanize())
                                               .WithLocation("Solar")
                                               .WithLocation2("Solar")
                                               .WithMiscFlags(EMiscFlag.StatusOnly)
                                               .WithMiscFlags(EMiscFlag.SetDoesNotChangeLastChange)
                                               .WithExtraData(CreatePlugInExtraData(featureType))
                                               .PrepareForHsDevice(newDevice);

            AddStatusControls(newFeatureData, featureType);

            return hsController.CreateFeatureForDevice(newFeatureData);
        }

        private readonly ImmutableDictionary<DeviceAndFeatureType, int> deviceFeatures;
    }
}