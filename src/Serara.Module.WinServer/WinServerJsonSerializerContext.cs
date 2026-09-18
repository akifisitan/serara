using System.Text.Json.Serialization;

namespace Serara.Module.WinServer;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(List<WinApplicationMetadata>))]
public sealed partial class WinServerJsonSerializerContext : JsonSerializerContext;
