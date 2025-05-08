using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using TorBoxNET;

namespace RdtClient.Service.Services.TorrentClients;

public class TorBoxClient(
    ILogger<TorBoxClient> logger,
    TorBoxUsenetClient usenetClient,
    TorBoxTorrentClient torrentClient
) : IMultiDownloadClient
{

    private ITorrentClient GetClient(Torrent? torrent = null, bool? forceUsenet = null, bool? forceTorrent = null)
    {
        if (forceUsenet == true && forceTorrent == true)
        {
            throw new ArgumentException("Cannot force both Usenet and Torrent clients at the same time.");
        }

        if (forceUsenet == true) return usenetClient;
        if (forceTorrent == true) return torrentClient;

        if (torrent != null)
        {
            Log($"Determining client for DebridContentKind: {torrent.DebridContentKind}", torrent);
            return torrent.DebridContentKind switch
            {
                1 => torrentClient,
                2 => usenetClient,
                _ => throw new ArgumentOutOfRangeException(nameof(torrent.DebridContentKind), torrent.DebridContentKind, "Invalid debrid content type specified. Expected 1 (Torrent) or 2 (Usenet).")
            };
        }

        return torrentClient; // Default fallback
    }

    private ITorrentClient GetClient(long? type)
    {
        return type switch
        {
            1 => torrentClient,
            2 => usenetClient,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid debrid content type specified. Expected 1 (Torrent) or 2 (Usenet).")
        };
    }

    public async Task<IList<TorrentClientTorrent>> GetTorrents()
    {
        var usenet = await usenetClient.GetTorrents();
        var torrents = await torrentClient.GetTorrents();
        return usenet.Concat(torrents).ToList();
    }

    public Task<TorrentClientUser> GetUser()
    {
        return torrentClient.GetUser();
    }

    public Task<int?> SelectFiles(Torrent torrent)
    {
        return GetClient(torrent).SelectFiles(torrent);
    }

    public Task<Torrent> UpdateData(Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
    {
        return GetClient(torrent).UpdateData(torrent, torrentClientTorrent);
    }

    public Task<IList<DownloadInfo>?> GetDownloadInfos(Torrent torrent)
    {
        return GetClient(torrent).GetDownloadInfos(torrent);
    }

    public Task<string> GetFileName(Download download)
    {
        Debug.Assert(download.FileName != null);
        return Task.FromResult(download.FileName);
    }

    // --- IMultiDownloadClient overloads with type routing ---

    public Task<string> AddMagnet(string link, long? type)
    {
        return GetClient(type).AddMagnet(link);
    }

    public Task<string> AddFile(byte[] bytes, long? type)
    {
        return GetClient(type).AddFile(bytes);
    }

    public Task<string> Unrestrict(string link, long? type)
    {
        return GetClient(type).Unrestrict(link);
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(string hash, long? type)
    {
        return GetClient(type).GetAvailableFiles(hash);
    }

    public Task Delete(string torrentId, long? type)
    {
        return GetClient(type).Delete(torrentId);
    }

    // --- ITorrentClient base methods (default to Torrent) ---

    public Task<string> AddMagnet(string link)
    {
        return AddMagnet(link, 1);
    }

    public Task<string> AddFile(byte[] bytes)
    {
        return AddFile(bytes, 1);
    }

    public Task<string> Unrestrict(string link)
    {
        return Unrestrict(link, 1);
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(string hash)
    {
        return GetAvailableFiles(hash, 1);
    }

    public Task Delete(string torrentId)
    {
        return Delete(torrentId, 1);
    }

    public static void MoveHashDirContents(string extractPath, Torrent torrent)
    {
        var hashDir = Path.Combine(extractPath, torrent.Hash);

        if (!Directory.Exists(hashDir)) return;

        var innerFolder = Directory.GetDirectories(hashDir)[0];
        var moveDir = extractPath.EndsWith(torrent.RdName!) ? extractPath : hashDir;

        foreach (var file in Directory.GetFiles(innerFolder))
        {
            var destFile = Path.Combine(moveDir, Path.GetFileName(file));
            File.Move(file, destFile);
        }

        foreach (var dir in Directory.GetDirectories(innerFolder))
        {
            var destDir = Path.Combine(moveDir, Path.GetFileName(dir));
            Directory.Move(dir, destDir);
        }

        if (!extractPath.Contains(torrent.RdName!))
        {
            Directory.Delete(innerFolder, true);
        }
        else
        {
            Directory.Delete(hashDir, true);
        }
    }

    private void Log(string message, Torrent? torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        logger.LogDebug(message);
    }
}
