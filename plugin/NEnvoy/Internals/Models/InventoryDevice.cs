using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable enable

namespace NEnvoy.Models;

public sealed record InventoryItem
(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("devices")] IReadOnlyCollection<InventoryDevice> Devices
);

public sealed record InventoryDevice
(
    [property: JsonPropertyName("part_num")] string PartNumber,
    [property: JsonPropertyName("serial_num")] string SerialNumber,
    [property: JsonPropertyName("device_status")] IEnumerable<string> DeviceStatus,
    [property: JsonPropertyName("dev_type")] int DeviceType,
    [property: JsonPropertyName("producing")] bool Producing,
    [property: JsonPropertyName("communicating")] bool Communicating,
    [property: JsonPropertyName("provisioned")] bool Provisioned,
    [property: JsonPropertyName("operating")] bool Operating
);