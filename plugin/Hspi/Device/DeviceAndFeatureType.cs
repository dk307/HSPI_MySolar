#nullable enable

namespace Hspi.Device

{
    public enum DeviceAndFeatureType
    {
        Main = 1,
        InvertersDevice = 2,

        FeatureType = 0x100,

        [ImagePathAttribute("solarwatts")]
        [UnitAttribute("KWatts")]
        [DecimalPointsAttribute(3)]
        Production = FeatureType | 0x2,

        [ImagePathAttribute("consumptionwatts")]
        [UnitAttribute("KWatts")]
        [DecimalPointsAttribute(3)]
        Consumption = FeatureType | 0x3,

        [ImagePathAttribute("netconsumptionwatts")]
        [UnitAttribute("KWatts")]
        [DecimalPointsAttribute(3)]
        NetConsumption = FeatureType | 0x4,

        [ImagePathAttribute("totalinverters")]
        [DecimalPointsAttribute(0)]
        TotalMicroInverters = FeatureType | 0x5,

        [DecimalPointsAttribute(0)]
        [ImagePathAttribute("communicatinginverters")]
        MicroInvertersCommunicating = FeatureType | 0x6,

        [DecimalPointsAttribute(0)]
        [ImagePathAttribute("operatinginverters")]
        MicroInvertersOperating = FeatureType | 0x7,

        [DecimalPointsAttribute(3)]
        [ImagePathAttribute("kwh")]
        [UnitAttribute("KW Hours")]
        ProducedToday = FeatureType | 0x8,

        [DecimalPointsAttribute(3)]
        [ImagePathAttribute("kwh")]
        [UnitAttribute("KW Hours")]
        ConsumptionToday = FeatureType | 0x9,

        [DecimalPointsAttribute(3)]
        [ImagePathAttribute("kwh")]
        [UnitAttribute("KW Hours")]
        Produced7Days = FeatureType | 0xA,

        [DecimalPointsAttribute(3)]
        [ImagePathAttribute("kwh")]
        [UnitAttribute("KW Hours")]
        Consumption7Days = FeatureType | 0xB,

        [DecimalPointsAttribute(3)]
        [ImagePathAttribute("kwh")]
        [UnitAttribute("KW Hours")]
        ProducedLifetime = FeatureType | 0xC,

        [DecimalPointsAttribute(3)]
        [ImagePathAttribute("kwh")]
        [UnitAttribute("KW Hours")]
        ConsumptionLifetime = FeatureType | 0xD,

        [DecimalPointsAttribute(0)]
        [ImagePathAttribute("inverterwatts")]
        [UnitAttribute("Watts")]
        InverterCurrentProduction = FeatureType | 0xE,
    };
}