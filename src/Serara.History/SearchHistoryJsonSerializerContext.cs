using System.Text.Json.Serialization;

namespace Serara.History;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
)]
[JsonSerializable(typeof(Dictionary<string, SearchHistoryEntry>))]
internal sealed partial class SearchHistoryJsonSerializerContext : JsonSerializerContext;
