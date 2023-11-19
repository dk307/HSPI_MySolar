using NEnvoy.Internals.Converters;
using System;
using System.Text.Json.Serialization;

namespace NEnvoy.Models;

public record V1Inverter(
    [property: JsonPropertyName("serialNumber")] string Serial,
    [property: JsonPropertyName("lastReportDate")][property: JsonConverter(typeof(IntTimestampDateTimeOffsetJsonConverter))] DateTimeOffset LastReportDate,
    [property: JsonPropertyName("lastReportWatts")] double LastReportedWatts
);