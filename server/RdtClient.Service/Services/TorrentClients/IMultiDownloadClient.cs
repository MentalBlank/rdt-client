using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.TorrentClient;

namespace RdtClient.Service.Services.TorrentClients;

/// <summary>
/// Extended client interface for providers supporting multiple download types (e.g., Usenet + Torrent).
/// </summary>
public interface IMultiDownloadClient : ITorrentClient
{
    /// <summary>
    /// Adds a magnet link to the appropriate queue based on the type.
    /// </summary>
    /// <param name="link">The magnet link</param>
    /// <param name="DebridContentKind">1 = Torrent, 2 = Usenet</param>
    Task<string> AddMagnet(string link, long? DebridContentKind);

    /// <summary>
    /// Adds a file (e.g., NZB or .torrent) to the debrid queue based on the type.
    /// </summary>
    /// <param name="bytes">The file contents</param>
    /// <param name="DebridContentKind">1 = Torrent, 2 = Usenet</param>
    Task<string> AddFile(byte[] bytes, long? DebridContentKind);

    /// <summary>
    /// Converts a debrid provider link to a direct download link.
    /// </summary>
    /// <param name="link">The debrid link</param>
    /// <param name="DebridContentKind">1 = Torrent, 2 = Usenet</param>
    Task<string> Unrestrict(string link, long? DebridContentKind);

    /// <summary>
    /// Gets the list of available files from the provider by hash and type.
    /// </summary>
    Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(string hash, long? DebridContentKind);

    /// <summary>
    /// Deletes a remote torrent/NZB from the debrid provider.
    /// </summary>
    Task Delete(string torrentId, long? DebridContentKind);
}
