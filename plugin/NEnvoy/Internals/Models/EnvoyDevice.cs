using System.Xml;
using System.Xml.Serialization;

namespace NEnvoy.Models;

#nullable enable

public record EnvoyDevice
{
    [XmlElement("sn")]
    public string? Serial { get; init; }
}