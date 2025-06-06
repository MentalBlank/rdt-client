﻿using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonoTorrent;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Web.Controllers;

[Authorize(Policy = "AuthSetting")]
[Route("Api/Torrents")]
public class TorrentsController(ILogger<TorrentsController> logger, Torrents torrents, TorrentRunner torrentRunner) : Controller
{
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IList<Torrent>>> GetAll()
    {
        var results = await torrents.Get();

        // Prevent infinite recursion when serializing
        foreach (var file in results.SelectMany(torrent => torrent.Downloads))
        {
            file.Torrent = null;
        }

        return Ok(results);
    }

    [HttpGet]
    [Route("Get/{torrentId:guid}")]
    public async Task<ActionResult<Torrent>> GetById(Guid torrentId)
    {
        var torrent = await torrents.GetById(torrentId);

        if (torrent?.Downloads != null)
        {
            foreach (var file in torrent.Downloads)
            {
                file.Torrent = null;
            }
        }

        return Ok(torrent);
    }

    /// <summary>
    ///     Used for debugging only. Force a tick.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("Tick")]
    public async Task<ActionResult> Tick()
    {
        await torrentRunner.Tick();

        return Ok();
    }

    [HttpPost]
    [Route("UploadFile")]
    public async Task<ActionResult> UploadFile([FromForm] IFormFile? file,
                                               [ModelBinder(BinderType = typeof(JsonModelBinder))]
                                               TorrentControllerUploadFileRequest? formData)
    {
        if (file == null || file.Length <= 0)
        {
            return BadRequest("Invalid file");
        }

        if (formData?.Torrent == null)
        {
            return BadRequest("Invalid Torrent");
        }

        logger.LogDebug($"Add file");

        var fileStream = file.OpenReadStream();

        await using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        switch (Path.GetExtension(file.FileName).ToLowerInvariant())
        {
            case ".nzb":
                formData.Torrent.ContentKind = 1;
                formData.Torrent.RdName = Path.GetFileNameWithoutExtension(file.FileName);

                break;
            case ".magnet":
            case ".torrent":
                formData.Torrent.ContentKind = 0;

                break;
            default:
                return BadRequest("Invalid file type");
        }

        await torrents.AddFileToDebridQueue(bytes, formData.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("UploadMagnet")]
    public async Task<ActionResult> UploadMagnet([FromBody] TorrentControllerUploadMagnetRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.MagnetLink))
        {
            return BadRequest("Invalid magnet link");
        }

        if (request.Torrent == null)
        {
            return BadRequest("Invalid Torrent");
        }

        logger.LogDebug($"Add magnet");

        if (request.MagnetLink.StartsWith("magnet:?"))
        {
            request.Torrent.ContentKind = 0;
        }
        else if (request.MagnetLink.StartsWith("https://") && request.MagnetLink.EndsWith(".nzb", StringComparison.OrdinalIgnoreCase))
        {
            request.Torrent.ContentKind = 1;
            var fileName = Path.GetFileNameWithoutExtension(new Uri(request.MagnetLink).AbsolutePath);
            request.Torrent.RdName = fileName;
        }
        else
        {
            return BadRequest("Invalid link format");
        }

        await torrents.AddMagnetToDebridQueue(request.MagnetLink, request.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("CheckFiles")]
    public async Task<ActionResult> CheckFiles([FromForm] IFormFile? file)
    {
        if (file == null || file.Length <= 0)
        {
            return BadRequest("Invalid torrent file");
        }

        var fileStream = file.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        switch (Path.GetExtension(file.FileName).ToLowerInvariant())
        {
            case ".nzb":
                return Ok(await torrents.GetAvailableFiles(DownloadHelper.ComputeMd5Hash(bytes), 1));
            case ".magnet":
            case ".torrent":
            {
                var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);

                return Ok(await torrents.GetAvailableFiles(torrent.InfoHashes.V1OrV2.ToHex(), 0));
            }
            default:
                return BadRequest("Invalid file type");
        }
    }

    [HttpPost]
    [Route("CheckFilesMagnet")]
    public async Task<ActionResult> CheckFilesMagnet([FromBody] TorrentControllerCheckFilesRequest? request)
    {
        if (request == null || String.IsNullOrWhiteSpace(request.MagnetLink))
        {
            return BadRequest("Link cannot be null or empty");
        }

        var contentKind = DownloadHelper.DetectContentKind(request.MagnetLink);

        switch (contentKind)
        {
            case 0: // Magnet link
                try
                {
                    if (request.MagnetLink.StartsWith("magnet:?"))
                    {
                        var magnet = MagnetLink.Parse(request.MagnetLink);
                        var result = await torrents.GetAvailableFiles(magnet.InfoHashes.V1OrV2.ToHex());

                        return Ok(result);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"Invalid magnet link: {ex.Message}");
                }

                break;

            case 1: // Direct NZB file URL
                try
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(request.MagnetLink);

                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest($"Failed to download NZB file: {response.StatusCode}");
                    }

                    await using var memoryStream = new MemoryStream();
                    await response.Content.CopyToAsync(memoryStream);

                    var bytes = memoryStream.ToArray();
                    var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);
                    var result = await torrents.GetAvailableFiles(torrent.InfoHashes.V1OrV2.ToHex());

                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error processing NZB file: {ex.Message}");
                }

            default:
                return BadRequest("Unsupported link format");
        }

        return BadRequest("Invalid link request");
    }

    [HttpPost]
    [Route("Delete/{torrentId:guid}")]
    public async Task<ActionResult> Delete(Guid torrentId, [FromBody] TorrentControllerDeleteRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        logger.LogDebug("Delete {torrentId}", torrentId);

        await torrents.Delete(torrentId, request.DeleteData, request.DeleteRdTorrent, request.DeleteLocalFiles);

        return Ok();
    }

    [HttpPost]
    [Route("Retry/{torrentId:guid}")]
    public async Task<ActionResult> Retry(Guid torrentId)
    {
        logger.LogDebug("Retry {torrentId}", torrentId);

        await torrents.UpdateRetry(torrentId, DateTimeOffset.UtcNow, 0);
        await torrents.RetryTorrent(torrentId, 0);

        return Ok();
    }

    [HttpPost]
    [Route("RetryDownload/{downloadId:guid}")]
    public async Task<ActionResult> RetryDownload(Guid downloadId)
    {
        logger.LogDebug("Retry download {downloadId}", downloadId);

        await torrents.RetryDownload(downloadId);

        return Ok();
    }

    [HttpPut]
    [Route("Update")]
    public async Task<ActionResult> Update([FromBody] Torrent? torrent)
    {
        if (torrent == null)
        {
            return BadRequest();
        }

        await torrents.Update(torrent);

        return Ok();
    }

    [HttpPost]
    [Route("VerifyRegex")]
    public async Task<ActionResult> VerifyRegex([FromForm] IFormFile? file, [FromBody] TorrentControllerVerifyRegexRequest? request)
    {
        if (request == null)
        {
            return Ok();
        }

        var includeError = "";
        var excludeError = "";

        IList<TorrentClientAvailableFile> availableFiles;

        if (!String.IsNullOrWhiteSpace(request.MagnetLink))
        {
            var magnet = MagnetLink.Parse(request.MagnetLink);

            availableFiles = await torrents.GetAvailableFiles(magnet.InfoHashes.V1OrV2.ToHex());
        }
        else if (file != null)
        {
            var fileStream = file.OpenReadStream();

            await using var memoryStream = new MemoryStream();

            await fileStream.CopyToAsync(memoryStream);

            var bytes = memoryStream.ToArray();

            var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);

            availableFiles = await torrents.GetAvailableFiles(torrent.InfoHashes.V1OrV2.ToHex());
        }
        else
        {
            return BadRequest();
        }

        var selectedFiles = new List<TorrentClientAvailableFile>();

        if (!String.IsNullOrWhiteSpace(request.IncludeRegex))
        {
            foreach (var availableFile in availableFiles)
            {
                try
                {
                    if (Regex.IsMatch(availableFile.Filename, request.IncludeRegex))
                    {
                        selectedFiles.Add(availableFile);
                    }
                }
                catch (Exception ex)
                {
                    includeError = ex.Message;
                }
            }
        }
        else if (!String.IsNullOrWhiteSpace(request.ExcludeRegex))
        {
            foreach (var availableFile in availableFiles)
            {
                try
                {
                    if (!Regex.IsMatch(availableFile.Filename, request.ExcludeRegex))
                    {
                        selectedFiles.Add(availableFile);
                    }
                }
                catch (Exception ex)
                {
                    excludeError = ex.Message;
                }
            }
        }
        else
        {
            selectedFiles = [.. availableFiles];
        }

        return Ok(new
        {
            includeError,
            excludeError,
            selectedFiles
        });
    }
}

public class TorrentControllerUploadFileRequest
{
    public Torrent? Torrent { get; set; }
}

public class TorrentControllerUploadMagnetRequest
{
    public String? MagnetLink { get; set; }
    public Torrent? Torrent { get; set; }
}

public class TorrentControllerDeleteRequest
{
    public Boolean DeleteData { get; set; }
    public Boolean DeleteRdTorrent { get; set; }
    public Boolean DeleteLocalFiles { get; set; }
}

public class TorrentControllerCheckFilesRequest
{
    public String? MagnetLink { get; set; }
}

public class TorrentControllerVerifyRegexRequest
{
    public String? IncludeRegex { get; set; }
    public String? ExcludeRegex { get; set; }
    public String? MagnetLink { get; set; }
}