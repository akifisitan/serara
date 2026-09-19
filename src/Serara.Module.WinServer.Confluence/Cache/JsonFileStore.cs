using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Serara.Module.WinServer;

public sealed class JsonFileStore
{
    public async Task<T?> ReadAsync<T>(
        string path,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken
    )
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(content, typeInfo);
    }

    public Task WriteAsync<T>(
        string path,
        T value,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken
    ) => File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, typeInfo), cancellationToken);
}
