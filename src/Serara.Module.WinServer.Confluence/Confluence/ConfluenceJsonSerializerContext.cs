using System.Text.Json.Serialization;

namespace Serara.Module.WinServer;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(SearchResponse))]
internal sealed partial class ConfluenceJsonSerializerContext : JsonSerializerContext;
