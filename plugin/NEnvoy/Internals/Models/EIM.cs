using System;
using System.Text.Json.Serialization;

namespace NEnvoy.Models;

public sealed record EIM
(
    int ActiveCount,
    DateTimeOffset ReadingTime,
    [property: JsonPropertyName("measurementType")] string MeasurementType,
    [property: JsonPropertyName("wNow")] double WattsNow,
    [property: JsonPropertyName("whLifetime")] double WattHoursLifeTime,
    [property: JsonPropertyName("whToday")] double WattHoursToday,
    [property: JsonPropertyName("whLastSevenDays")] double WattHoursLastSevenDays
) : ProductionRecord(ActiveCount, ReadingTime);