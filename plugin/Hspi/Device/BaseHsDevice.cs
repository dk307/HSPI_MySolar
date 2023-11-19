using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi.Utils;
using Serilog;
using Serilog.Events;
using static System.FormattableString;

#nullable enable

namespace Hspi.Device

{
    public class BaseHsDevice
    {
        public BaseHsDevice(IHsController hS, int deviceRefId, CancellationToken cancellationToken)
        {
            HS = hS;
            this.DeviceRefId = deviceRefId;
            this.cancellationToken = cancellationToken;
        }

        public int DeviceRefId { get; }

        public static PlugExtraData CreatePlugInExtraData(DeviceAndFeatureType type)
        {
            var plugExtraData = new PlugExtraData();
            //store as hex
            plugExtraData.AddNamed(TypeKey, ((int)type).ToString("X", CultureInfo.InvariantCulture));
            return plugExtraData;
        }

        public static string GetNameForLog(IHsController hsController, int refId)
        {
            try
            {
                return hsController.GetNameByRef(refId);
            }
            catch
            {
                return Invariant($"RefId:{refId}");
            }
        }

        public static DeviceAndFeatureType? GetType(IHsController hsController, int refId)
        {
            if (hsController.GetPropertyByRef(refId, EProperty.PlugExtraData) is not PlugExtraData plugInExtra)
            {
                throw new HsDeviceInvalidException("PlugExtraData is null");
            }

            if (!plugInExtra.ContainsNamed(TypeKey))
            {
                throw new HsDeviceInvalidException(Invariant($"{TypeKey} type not found in {refId}"));
            }

            var value = plugInExtra.GetNamed(TypeKey);
            if (value is not null &&
                int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var enumValue) &&
                Enum.IsDefined(typeof(DeviceAndFeatureType), enumValue))
            {
                return (DeviceAndFeatureType)enumValue;
            }

            return default;
        }

        protected static void AddStatusControls(NewFeatureData data, DeviceAndFeatureType featureType)
        {
            var imageName = EnumHelper.GetAttribute<ImagePathAttribute>(featureType)?.FileName ?? "default";
            var decimalPoints = EnumHelper.GetAttribute<DecimalPointsAttribute>(featureType)?.Count ?? 0;
            var imagePath = GetImagePath(imageName);

            var graphicCollection = new StatusGraphicCollection();
            ValueRange targetRange = new(int.MinValue, int.MaxValue)
            {
                DecimalPlaces = decimalPoints
            };
            graphicCollection.Add(new StatusGraphic(imagePath, targetRange));

            data.Feature[EProperty.StatusGraphics] = graphicCollection;

            var unitAttribute = EnumHelper.GetAttribute<UnitAttribute>(featureType);

            if (unitAttribute != null)
            {
                var suffix = unitAttribute.Unit;
                data.Feature.Add(EProperty.AdditionalStatusData, new List<string?>() { suffix });

                if (data.Feature.TryGetValue(EProperty.StatusGraphics, out var valueG) &&
                    valueG is StatusGraphicCollection graphics &&
                    graphics.Values != null)
                {
                    foreach (var statusGraphic in graphics.Values)
                    {
                        statusGraphic.HasAdditionalData = true;
                        statusGraphic.TargetRange.Suffix = " " + HsFeature.GetAdditionalDataToken(0);
                    }
                }
            }
        }

        protected static string GetImagePath(string iconFileName)
        {
            return Path.ChangeExtension(Path.Combine(PlugInData.PlugInId, "images", iconFileName), "png");
        }

        protected void UpdateDeviceValue(int refId, in double? data, in DateTimeOffset? lastChange = null)
        {
            if (Log.IsEnabled(LogEventLevel.Information))
            {
                var existingValue = Convert.ToDouble(HS.GetPropertyByRef(refId, EProperty.Value));

                Log.Write(existingValue != data ? LogEventLevel.Information : LogEventLevel.Debug,
                          "Updated value {value} for the {name}", data, GetNameForLog(HS, refId));
            }

            if (data.HasValue && HasValue(data.Value))
            {
                HS.UpdatePropertyByRef(refId, EProperty.InvalidValue, false);

                // only this call triggers events
                if (!HS.UpdateFeatureValueByRef(refId, data.Value))
                {
                    throw new InvalidOperationException($"Failed to update device {GetNameForLog(HS, refId)}");
                }
            }
            else
            {
                HS.UpdatePropertyByRef(refId, EProperty.InvalidValue, true);
            }
            if (lastChange.HasValue)
            {
                HS.UpdatePropertyByRef(refId, EProperty.LastChange, lastChange.Value.ToLocalTime().DateTime);
            }

            static bool HasValue(double value)
            {
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }
        }

        protected void UpdateDeviceValue(int refId, string? data, in DateTimeOffset? lastChange = null)
        {
            if (Log.IsEnabled(LogEventLevel.Information))
            {
                var existingValue = Convert.ToString(HS.GetPropertyByRef(refId, EProperty.StatusString));

                Log.Write(existingValue != data ? LogEventLevel.Information : LogEventLevel.Debug,
                          "Updated value {value} for the {name}", data, GetNameForLog(HS, refId));
            }

            if (!HS.UpdateFeatureValueStringByRef(refId, data ?? string.Empty))
            {
                throw new InvalidOperationException($"Failed to update device {GetNameForLog(HS, refId)}");
            }
            HS.UpdatePropertyByRef(refId, EProperty.InvalidValue, false);
            if (lastChange.HasValue)
            {
                HS.UpdatePropertyByRef(refId, EProperty.LastChange, lastChange.Value.ToLocalTime().DateTime);
            }
        }

        public const string TypeKey = "data";

        protected readonly CancellationToken cancellationToken;

        protected readonly IHsController HS;
    }
}