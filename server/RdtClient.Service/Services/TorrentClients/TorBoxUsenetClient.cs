using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TorBoxNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services.TorrentClients;

public class TorBoxUsenetClient(ILogger<TorBoxUsenetClient> logger, IHttpClientFactory httpClientFactory, IDownloadableFileFilter fileFilter) : ITorrentClient
{
    private TimeSpan? _offset;
    private TorBoxNetClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new("TorBox API Key not set in the settings");
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"rdt-client {Assembly.GetEntryAssembly()?.GetName().Version}");
            httpClient.Timeout = TimeSpan.FromSeconds(Settings.Get.Provider.Timeout);

            var torBoxNetClient = new TorBoxNetClient(null, httpClient, 5);
            torBoxNetClient.UseApiAuthentication(apiKey);

            if (_offset == null)
            {
                var serverTime = DateTimeOffset.UtcNow;
                _offset = serverTime.Offset;
            }

            return torBoxNetClient;
        }
        catch (AggregateException ae)
        {
            foreach (var inner in ae.InnerExceptions)
            {
                logger.LogError(inner, $"The connection to TorBox has failed: {inner.Message}");
            }

            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, $"The connection to TorBox has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"The connection to TorBox has timed out: {ex.Message}");

            throw;
        }
    }

  private TorrentClientTorrent Map(UsenetInfoResult torrent)
  {
      return new()
      {
          Id = torrent.Hash,
          Filename = torrent.Name,
          OriginalFilename = torrent.Name,
          Hash = torrent.Hash,
          Bytes = torrent.Size,
          OriginalBytes = torrent.Size,
          Host = torrent.DownloadPresent.ToString(),
          Split = 0,
          Progress = (Int64)(torrent.Progress * 100.0),
          Status = torrent.DownloadState,
          Added = ChangeTimeZone(torrent.CreatedAt)!.Value,
            Files = (torrent.Files ?? []).Select(m => new TorrentClientFile
            {
                Path = String.Join("/", m.Name.Split('/').Skip(1)),
                Bytes = m.Size,
                Id = m.Id,
                Selected = true
            }).ToList(),
          Links = [],
          Ended = ChangeTimeZone(torrent.UpdatedAt),
          Speed = torrent.DownloadSpeed,
          Seeders = 0
      };
  }

  public async Task<IList<TorrentClientTorrent>> GetTorrents()
  {
      var usenetDownloads = new List<UsenetInfoResult>();

      var currentTorrents = await GetClient().Usenet.GetCurrentAsync(true);
      if (currentTorrents != null)
      {
          usenetDownloads.AddRange(currentTorrents);
      }

      var queuedTorrents = await GetClient().Usenet.GetQueuedAsync(true);
      if (queuedTorrents != null)
      {
          usenetDownloads.AddRange(queuedTorrents);
      }

      return usenetDownloads.Select(Map).ToList();
  }

    public async Task<TorrentClientUser> GetUser()
    {
        var user = await GetClient().User.GetAsync(false);

        return new()
        {
            Username = user.Data!.Email,
            Expiration = user.Data!.Plan != 0 ? user.Data!.PremiumExpiresAt!.Value : null
        };
    }

    public async Task<String> AddMagnet(String nzbUrl)
    {
        var result = await GetClient().Usenet.AddLinkAsync(nzbUrl);

        if (result.Error != null)
        {
            throw new($"AddLinkAsync returned an error: {result.Error}");
        }

        return result.Data!.Hash!;
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        logger.LogDebug("Adding file to TorBox Usenet: {Length} bytes", bytes.Length);

        var user = await GetClient().User.GetAsync(true);

        var result = await GetClient().Usenet.AddFileAsync(bytes);
        if (result.Error == "ACTIVE_LIMIT")
        {
            return DownloadHelper.ComputeMd5Hash(bytes);
        }

        return result.Data!.Hash!;
    }

    public async Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(string hash)
    {
        var response = await GetClient().Usenet.GetAvailabilityAsync(hash, listFiles: true);

        logger.LogDebug("Usenet availability response for hash {Hash}:\n{Response}",
                        hash, JsonConvert.SerializeObject(response, Formatting.Indented));

        if (response?.Data != null && response.Data.Count > 0)
        {
            logger.LogDebug("Availability data exists for hash {Hash}", hash);
        }
        else
        {
            logger.LogWarning("No availability data returned for hash {Hash}", hash);
        }

        return [];
    }

    public Task<Int32?> SelectFiles(Torrent torrent)
    {
        return Task.FromResult<Int32?>(torrent.Files.Count);
    }

    public async Task Delete(String torrentId)
    {
        await GetClient().Usenet.ControlAsync(torrentId, "delete");
    }

    public async Task<String> Unrestrict(String link)
    {
        var segments = link.Split('/');

        var zipped = segments[5] == "zip";
        var fileId = zipped ? "0" : segments[5];

        var result = await GetClient().Usenet.RequestDownloadAsync(Convert.ToInt32(segments[4]), Convert.ToInt32(fileId), zipped);

        if (result.Error != null)
        {
            throw new("Unrestrict returned an invalid download");
        }

        return result.Data!;
    }

    public async Task<Torrent> UpdateData(Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (torrent.RdId == null)
            {
                return torrent;
            }

            var rdTorrent = torrentClientTorrent ?? await GetInfo(torrent.Hash) ?? throw new($"Resource not found");

            torrent.RdName = !String.IsNullOrWhiteSpace(rdTorrent.OriginalFilename)
                ? rdTorrent.OriginalFilename
                : rdTorrent.Filename;

            if (rdTorrent.Bytes > 0)
            {
                torrent.RdSize = rdTorrent.Bytes;
            }
            else if (rdTorrent.OriginalBytes > 0)
            {
                torrent.RdSize = rdTorrent.OriginalBytes;
            }

            if (rdTorrent.Files != null)
            {
                torrent.RdFiles = JsonConvert.SerializeObject(rdTorrent.Files);
            }

            torrent.ClientKind = Provider.TorBox;
            torrent.RdHost = rdTorrent.Host;
            torrent.RdSplit = rdTorrent.Split;
            torrent.RdProgress = rdTorrent.Progress;
            torrent.RdAdded = rdTorrent.Added;
            torrent.RdEnded = rdTorrent.Ended;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = 0;
            torrent.RdStatusRaw = rdTorrent.Status;

            if (rdTorrent.Host == "True")
            {
                torrent.RdStatus = TorrentStatus.Finished;
            }
            else
            {
                torrent.RdStatus = rdTorrent.Status switch
                {
                    //Todo: edit this section
                    "queued" => TorrentStatus.Processing,
                    "metaDL" => TorrentStatus.Processing,
                    "checking" => TorrentStatus.Processing,
                    "checkingResumeData" => TorrentStatus.Processing,
                    "paused" => TorrentStatus.Downloading,
                    "stalledDL" => TorrentStatus.Downloading,
                    "downloading" => TorrentStatus.Downloading,
                    "completed" => TorrentStatus.Downloading,
                    "uploading" => TorrentStatus.Downloading,
                    "uploading (no peers)" => TorrentStatus.Downloading,
                    "stalled" => TorrentStatus.Downloading,
                    "stalled (no seeds)" => TorrentStatus.Downloading,
                    "processing" => TorrentStatus.Downloading,
                    "cached" => TorrentStatus.Finished,
                    _ => TorrentStatus.Error
                };
            }

        }
        catch (Exception ex)
        {
            if (ex.Message == "Resource not found")
            {
                torrent.RdStatusRaw = "deleted";
            }
            else
            {
                throw;
            }
        }

        return torrent;
    }

    public async Task<IList<DownloadInfo>?> GetDownloadInfos(Torrent torrent)
    {
        var torrentId = await GetClient().Usenet.GetHashInfoAsync(torrent.Hash, skipCache: true);
        var downloadableFiles = torrent.Files.Where(file => fileFilter.IsDownloadable(torrent, file.Path, file.Bytes)).ToList();
        if (downloadableFiles.Count == torrent.Files.Count && torrent.DownloadClient != Data.Enums.DownloadClient.Symlink && Settings.Get.Provider.PreferZippedDownloads)
        {
            logger.LogDebug("Downloading files from TorBox as a zip.");
            return
            [
                new()
                {
                    RestrictedLink = $"https://torbox.app/fakedl/{torrentId?.Id}/zip",
                    FileName = $"{torrent.RdName}.zip"
                }
            ];
        }

        logger.LogDebug("Downloading files from TorBox individually.");

        return downloadableFiles.Select(file => new DownloadInfo
                                {
                                    RestrictedLink = $"https://torbox.app/fakedl/{torrentId?.Id}/{file.Id}",
                                    FileName = Path.GetFileName(file.Path)
                                })
                                .ToList();
    }

    public Task<String> GetFileName(Download download)
    {
        Debug.Assert(download.FileName != null);
        
        return Task.FromResult(download.FileName);
    }

    private DateTimeOffset? ChangeTimeZone(DateTimeOffset? dateTimeOffset)
    {
        if (_offset == null)
        {
            return dateTimeOffset;
        }

        return dateTimeOffset?.Subtract(_offset.Value).ToOffset(_offset.Value);
    }

    private async Task<TorrentClientTorrent> GetInfo(String torrentHash)
    {
        var result = await GetClient().Usenet.GetHashInfoAsync(torrentHash, skipCache: true);

        return Map(result!);
    }

    public static void MoveHashDirContents(String extractPath, Torrent _torrent)
  {
        var hashDir = Path.Combine(extractPath, _torrent.Hash);
  
      if (!Directory.Exists(hashDir)) return;
  
      var subdirs = Directory.GetDirectories(hashDir);
      if (subdirs.Length == 0) return;
  
      var innerFolder = subdirs[0];
  
      foreach (var file in Directory.GetFiles(innerFolder))
      {
          var destFile = Path.Combine(extractPath, Path.GetFileName(file));
          if (File.Exists(destFile)) File.Delete(destFile);
          File.Move(file, destFile);
      }
  
      foreach (var dir in Directory.GetDirectories(innerFolder))
      {
          var destDir = Path.Combine(extractPath, Path.GetFileName(dir));
          if (Directory.Exists(destDir)) Directory.Delete(destDir, true);
          Directory.Move(dir, destDir);
      }
  
      try
      {
          Directory.Delete(hashDir, true);
      }
      catch (IOException ex)
      {
          Console.Error.WriteLine($"[Extract CleanUp] Failed to delete: {ex.Message}");
      }
  }

    private void Log(String message, Torrent? torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        logger.LogDebug(message);
    }
}
