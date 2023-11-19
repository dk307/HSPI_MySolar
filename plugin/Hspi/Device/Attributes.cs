using System;

#nullable enable

namespace Hspi.Device

{
    [AttributeUsage(AttributeTargets.All)]
    public class DecimalPointsAttribute : Attribute
    {
        public DecimalPointsAttribute(int count)
        {
            Count = count;
        }

        public int Count { get; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class ImagePathAttribute : Attribute
    {
        public ImagePathAttribute(string path)
        {
            FileName = path;
        }

        public string FileName { get; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class UnitAttribute : Attribute
    {
        public UnitAttribute(string unit)
        {
            Unit = unit;
        }

        public string Unit { get; }
    }
}