using System.Text.Json.Serialization;

namespace Serara.Infra.Confluence;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(SearchResponse))]
public sealed partial class ConfluenceJsonSerializerContext : JsonSerializerContext;
