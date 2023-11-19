using System;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace NEnvoy.Models;

[XmlRoot("envoy_info")]
public record EnvoyInfo
{
    [XmlElement("device")]
    public EnvoyDevice Device { get; init; }
}