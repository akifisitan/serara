namespace Serara.Module.WinServer;

public interface IWinApplicationMetadataLoader
{
    Task<IReadOnlyList<WinApplicationMetadata>> Load(CancellationToken cancellationToken);
}
