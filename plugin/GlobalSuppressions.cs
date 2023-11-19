// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "<Pending>", Scope = "type", Target = "~T:NEnvoy.Models.EIM")]
[assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "<Pending>", Scope = "member", Target = "~F:Hspi.NEnvoy.EnvoyConnectionInfo.DefaultEnphaseBaseUri")]
[assembly: SuppressMessage("Critical Vulnerability", "S4830:Server certificates should be verified during SSL/TLS connections", Justification = "<Pending>", Scope = "member", Target = "~M:Hspi.NEnvoy.EnvoyClient.GetUnsafeClient(System.Uri,Hspi.NEnvoy.EnvoySession)~System.Net.Http.HttpClient")]
[assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "<Pending>", Scope = "member", Target = "~F:Hspi.NEnvoy.EnvoyConnectionInfo.DefaultEntrezBaseUri")]
[assembly: SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "<Pending>", Scope = "type", Target = "~T:Hspi.HspiBase")]
[assembly: SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "<Pending>", Scope = "type", Target = "~T:Hspi.Device.DeviceAndFeatureType")]
