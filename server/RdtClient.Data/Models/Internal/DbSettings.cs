using RdtClient.Data.Enums;
using System.ComponentModel;

namespace RdtClient.Data.Models.Internal;

public class DbSettings
{
    [DisplayName("General")]
    [Description("")]
    public DbSettingsGeneral General { get; set; } = new();

    [DisplayName("Download Client")]
    [Description("")]
    public DbSettingsDownloadClient DownloadClient { get; set; } = new();

    [DisplayName("Provider")]
    [Description("")]
    public DbSettingsProvider Provider { get; set; } = new();

    [DisplayName("qBittorrent / *darr")]
    [Description("The following settings only apply when a torrent gets added through the qbittorrent API, usually Radarr or Sonarr.")]
    public DbSettingsIntegrations Integrations { get; set; } = new();

    [DisplayName("GUI Defaults")]
    [Description("Settings used when adding a torrent through the web interface.")]
    public DbSettingsGui Gui { get; set; } = new();

    [DisplayName("Watch")]
    [Description("The following settings only apply when a torrent gets through the watch folder.")]
    public DbSettingsWatch Watch { get; set; } = new();
}

public class DbSettingsGeneral
{
    [DisplayName("Log level")]
    [Description("Recommended level is Warning, set to Debug to get the most info.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Error;

    [DisplayName("Maximum parallel downloads")]
    [Description("Maximum amount of torrents that get downloaded to your host at the same time.")]
    public Int32 DownloadLimit { get; set; } = 100;

    [DisplayName("Maximum unpack processes")]
    [Description("Maximum amount of downloads that get unpacked on your host at the same time. Set to 0 to disable unpacking.")]
    public Int32 UnpackLimit { get; set; } = 0;

    [DisplayName("Categories")]
    [Description("Expose these categories through the QBittorrent API. Define multiple categories by separating them with a comma.")]
    public String? Categories { get; set; } = "radarr,sonarr,manual";

    [DisplayName("Run external program on torrent completion")]
    [Description("Path to the executable to run when the torrent and all downloads are finished. No arguments should be passed here.When running in Docker, this command will run on your docker instance!")]
    public String? RunOnTorrentCompleteFileName { get; set; } = null;

    [DisplayName("External program arguments")]
    [Description(@"When the executable above is executed, use these parameters.
Supports the following parameters:
%N: Torrent name
%L: Category
%F: Content path (same as root path for multifile torrent)
%R: Root path (first torrent subdirectory path)
%D: Save path
%C: Number of files
%Z: Torrent size (bytes)
%I: Info hash")]
    public String? RunOnTorrentCompleteArguments { get; set; } = null;

    [DisplayName("Authentication Type")]
    [Description("How to authenticate with the client. WARNING: when set to None anyone with access to the URL can use the client without any credentials.")]
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.UserNamePassword;

    [DisplayName("Copy added torrent files")]
    [Description("When a torrent file or magnet is added, create a copy in this directory.")]
    public String? CopyAddedTorrents { get; set; } = "/mnt/seed";

    [DisplayName("RClone custom command ")]
    [Description("Allows users to define a command, such as \"rc vfs/refresh recursive=true --rc-addr=172.17.0.1:5572\", to trigger an RClone refresh operation before initiating the file discovery process.")]
    public String? RcloneRefreshCommand { get; set; } = "";

    [DisplayName("Tracker enrichment list")]
    [Description("Optional. Specify the URL of a tracker list file to be appended to magnet links and torrent files.")]
    public String? MagnetTrackerEnrichment { get; set; } = "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_best.txt";

    [DisplayName("Disable update notifications")]
    [Description("Ignore update notifications. You will still be notified if the version you are running has a security vulnerability.")]
    public Boolean DisableUpdateNotifications { get; set; } = false;
}

public class DbSettingsDownloadClient
{
    [DisplayName("Download client")]
    [Description(@"Select which download client to use, see the
<a href=""https://github.com/mentalblank/rdt-client/"" target=""_blank"">README</a> for the various options.")]
    public DownloadClient Client { get; set; } = DownloadClient.Internal;

    [DisplayName("Download path")]
    [Description("Path in the docker container to download files to (i.e. /data/downloads), or a local path when using as a service.")]
    public String DownloadPath { get; set; } = "/mnt/symlinks";

    [DisplayName("Mapped path")]
    [Description("Path where files are downloaded to on your host (i.e. D:\\Downloads). This path is used for *arr to find your downloads.")]
    public String MappedPath { get; set; } = "/mnt/symlinks";

    [DisplayName("Download speed (in MB/s) (only used for the Internal Downloader)")]
    [Description("Maximum download speed in Megabytes per second. When set to 0 unlimited speed is used.")]
    public Int32 MaxSpeed { get; set; } = 0;

    [DisplayName("Parallel connections per download (only used for the Internal Downloader)")]
    [Description("Maximum amount of parallel threads that are used to download a single file to your host. If set to 0 no parallel downloading will be done.")]
    public Int32 ParallelCount { get; set; } = 100;

    [DisplayName("Chunk Count")]
    [Description("Split the downloaded file in this amount of chunks.")]
    public Int32 ChunkCount { get; set; } = 0;

    [DisplayName("Buffer Size")]
    [Description("Buffersize in bytes for the internal downloader, used to read data and write it to disk.")]
    public Int32 BufferSize { get; set; } = 4 * 1024 * 1024;

    [DisplayName("Connection Timeout (only used for the Internal Downloader)")]
    [Description("Timeout in milliseconds before the downloader times out.")]
    public Int32 Timeout { get; set; } = 5000;

    [DisplayName("Proxy Server (only used for the Bezzad Downloader)")]
    [Description("Address of a proxy server to download through (only used for the Bezzad Downloader).")]
    public String? ProxyServer { get; set; } = null;

    [DisplayName("Aria2c URL (only used for the Aria2c Downloader)")]
    [Description(@"This is the URL to your Aria2c instance. It must end in /jsonrpc. A common URL is
http://127.0.0.1:6800/jsonrpc.")]
    public String Aria2cUrl { get; set; } = "http://127.0.0.1:6800/jsonrpc";

    [DisplayName("Aria2c Secret (only used for the Aria2c Downloader)")]
    [Description("The secret of your Aria2c instance. Optional.")]
    public String Aria2cSecret { get; set; } = "mysecret123";

    [DisplayName("Aria2c Download Path")]
    [Description("The root path to download the file to on the Aria2c host, if empty use the Download path setting.")]
    public String? Aria2cDownloadPath { get; set; } = null;

    [DisplayName("Rclone mount path (only used for the Symlink Downloader)")]
    [Description("Path where Rclone is mounted. Required for Symlink Downloader. Suffix this path with a * to search subdirectories too.")]
    public String RcloneMountPath { get; set; } = "/mnt/remote/realdebrid/torrents/*";

    [DisplayName("Synology DownloadStation URL")]
    [Description("The URL to the Synology DownloadStation. A common URL is http://127.0.0.1:5000")]
    public String DownloadStationUrl { get; set; } = "http://127.0.0.1:5000";

    [DisplayName("Synology DownloadStation Username")]
    [Description("The username to use when connecting to the Synology DownloadStation.")]
    public String? DownloadStationUsername { get; set; } = null;
    [DisplayName("Synology DownloadStation Password")]
    [Description("The password to use when connecting to the Synology DownloadStation.")]
    public String? DownloadStationPassword { get; set; } = null;

    [DisplayName("Synology Download Station Download Path")]
    [Description("The root path to doawnload the file on the Synology DownloadStation host, if empty use the default DownloadStation path.")]
    public String? DownloadStationDownloadPath { get; set; } = null;

    [DisplayName("Log level")]
    [Description("Only set when trying to debug a download client, can generate a lot of logs.")]
    public DownloadClientLogLevel LogLevel { get; set; } = DownloadClientLogLevel.None;
}

public class DbSettingsProvider
{
    [DisplayName("Provider")]
    [Description(@"The following 4 providers are supported:
<a href=""https://real-debrid.com/"" target=""_blank"" rel=""noopener"">https://real-debrid.com</a>
<a href=""https://alldebrid.com/"" target=""_blank"" rel=""noopener"">https://alldebrid.com</a>
<a href=""https://www.premiumize.me/"" target=""_blank"" rel=""noopener"">https://www.premiumize.me/</a>
<a href=""https://debrid-link.com/"" target=""_blank"" rel=""noopener"">https://debrid-link.com/</a>
<a href=""https://torbox.app/"" target=""_blank"" rel=""noopener"">https://torbox.app/</a>
At this point only 1 provider can be used at the time.")]
    public Provider Provider { get; set; } = Provider.RealDebrid;

    [DisplayName("API Key")]
    [Description(@"You can find your API key here:
<a href=""https://real-debrid.com/apitoken"" target=""_blank"" rel=""noopener"">https://real-debrid.com/apitoken</a>
or
<a href=""https://alldebrid.com/apikeys/"" target=""_blank"" rel=""noopener"">https://alldebrid.com/apikeys/</a>
or
<a href=""https://www.premiumize.me/account/"" target=""_blank"" rel=""noopener"">https://www.premiumize.me/account/</a>
or
<a href=""https://torbox.app/settings/"" target=""_blank"" rel=""noopener"">https://torbox.app/settings/</a>
or
<a href=""https://debrid-link.com/webapp/apikey"" target=""_blank"" rel=""noopener"">https://debrid-link.com/webapp/apikey</a>")]
    public String ApiKey { get; set; } = "";

    [DisplayName("API Hostname (RD only)")]
    [Description("Use this instead of the normal hostname for Real Debrid API requests. Only used by Real Debrid. Leave blank to use default.")]
    public String? ApiHostname { get; set; }

    [DisplayName("Automatically import and process torrents added to provider")]
    [Description("When selected, import downloads that are not added through RDTClient but have been directly added to your debrid provider.")]
    public Boolean AutoImport { get; set; } = false;

    [DisplayName("Automatically delete downloads removed from provider")]
    [Description("When selected, cancel and delete downloads that have been removed from your debrid provider.")]
    public Boolean AutoDelete { get; set; } = false;

    [DisplayName("Connection Timeout")]
    [Description("Timeout in seconds to make a connection to the provider. Increase if you experience timeouts in the logs.")]
    public Int32 Timeout { get; set; } = 10;

    [DisplayName("Check Interval")]
    [Description("The interval to check the torrents info on the providers API. Minumum is 3 seconds. When there are no active downloads this limit is increased * 3.")]
    public Int32 CheckInterval { get; set; } = 10;

    [DisplayName("Max parallel downloads")]
    [Description("Limits the number of torrents that will be sent for downloading on the debrid provider at the same time. If set to 0, all downloads will be sent immediately without queuing.")]
    public Int32 MaxParallelDownloads { get; set; } = 0;

    [DisplayName("Prefer zipped downloads")]
    [Description("Torbox only. When selected, rdt-client will try to download the entire torrent as a .zip from TorBox and unpack it instead of downloading each file individually.")]
    public Boolean PreferZippedDownloads { get; set; } = false;

    [DisplayName("Auto Import Defaults")]
    public DbSettingsDefaultsWithCategory Default { get; set; } = new();
}

public class DbSettingsIntegrations
{
    public DbSettingsDefaultsWithCategory Default { get; set; } = new();
}

public class DbSettingsGui
{
    public DbSettingsDefaultsWithCategory Default { get; set; } = new();
}

public class DbSettingsWatch
{
    [DisplayName("Watch Path")]
    [Description("Watch this path for .torrent or .magnet files. When a file is found it will be automatically imported.")]
    public String? Path { get; set; } = null;

    [DisplayName("Error Path")]
    [Description("When an error occurs the torrent file is moved to this directory. When unset it will be moved to /error in the watchpath.")]
    public String? ErrorPath { get; set; } = null;

    [DisplayName("Processed Path")]
    [Description("When a torrent file is added succesfully it will be moved to this directory. When unset it will be moved to /processed in the watchpath.")]
    public String? ProcessedPath { get; set; } = null;

    [DisplayName("Check  Interval")]
    [Description("Time in seconds to check the folder for new files.")]
    public Int32 Interval { get; set; } = 60;

    [DisplayName("Import Defaults")]
    public DbSettingsDefaultsWithCategory Default { get; set; } = new();
}

public class DbSettingsDefaultsWithCategory : DbSettingsDefaults
{
    [DisplayName("Post Torrent Download Action")]
    [Description("When a torrent is finished downloading on your debrid provider, perform this action. Use this setting if you only want to add files to your debrid provider but not download them to the host.")]
    public TorrentHostDownloadAction HostDownloadAction { get; set; }

    [DisplayName("Category")]
    [Description("When a torrent is imported assign it this category.")]
    public String? Category { get; set; } = null;

    [DisplayName("Post Download Action")]
    [Description("When all files are downloaded from the provider to the host, perform this action. Does not apply when using the symlink downloader.")]
    public TorrentFinishedAction FinishedAction { get; set; } = TorrentFinishedAction.RemoveAllTorrents;
}

public class DbSettingsDefaults
{
    [DisplayName("Only download available files on debrid provider")]
    [Description("When selected, it will only download files in the torrent that have been download by your debrid provider. You can use this in combination with the Min File size setting above.")]
    public Boolean OnlyDownloadAvailableFiles { get; set; } = true;

    [DisplayName("[Symlink Only] Allow download of compressed files")]
    [Description("When selected and using the symlink downloader, it will allow the download of compressed files from your debrid provider.")]
    public Boolean DownloadCompressedSymlink { get; set; } = true;

    [DisplayName("Minimum file size to download")]
    [Description("Files that are smaller than this setting are skipped and not downloaded. When set to 0 all files are downloaded. When downloading from Radarr or Sonarr it's recommended to keep this setting at atleast a few MB to avoid the debrid provider having to re-download the torrent.")]
    public Int32 MinFileSize { get; set; } = 2;

    [DisplayName("Include files")]
    [Description("Select only the files that are matching this regular expression. Only use this setting OR the Exclude files setting, not both.")]
    public String? IncludeRegex { get; set; }

    [DisplayName("Exclude files")]
    [Description("Ignore files that are matching this regular expression. Only use this setting OR the Include files setting, not both.")]
    public String? ExcludeRegex { get; set; } = @"(?i)^.*\.(xml|ac3|sql|docx|arj|lzh|tar|gz|bat|bmp|cmd|cue|db|diz|dll|((dvd)?disc|par(t)?|py)\d*|exe|flac|gif|htm(l)?|ico(n)?|idx|info|ini|index|jp(e)?g|l(i)?nk|md5|m2ts|mp3|nfo|nzb|png|rar|readme|reg|sample|sfv|sql|srr|sub|srt|tag|tak|t(e)?xt|thumb|torrent|ts|url|vbs|webp|zip|zsh|padding_file|smallfile|_unpack|ade|adp|app|application|appref-ms|asp|aspx|asx|bas|bgi|cab|cer|chm|cmd|cnt|com|cpl|crt|csh|der|diagcab|fxp|gadget|grp|hlp|hpj|hta|htc|inf|ins|iso|isp|its|jar|jnlp|js|jse|ksh|lnk|mad|maf|mag|mam|maq|mar|mas|mat|mau|mav|maw|mcf|mda|mdb|mde|mdt|mdw|mdz|msc|msh|msh1|msh2|mshxml|msh1xml|msh2xml|msi|msp|mst|msu|ops|osd|pcd|pif|pl|plg|prf|prg|printerexport|ps1|ps1xml|ps2|ps2xml|psc1|psc2|psd1|psdm1|pst|py|pyc|pyo|pyw|pyz|pyzw|reg|scf|scr|sct|shb|shs|theme|tmp|vb|vbe|vbp|vbs|vhd|vhdx|vsmacros|vsw|webpnp|website|ws|wsc|wsf|wsh|xbap|xll|xnk|7z|bdjo|bdmv|bin|cci|clpi|crl|idx|m4a|mpls|msi|pdf|sig|tbl|xig|xrt|zipx)$";

    [DisplayName("Trim file extentions from folder name")]
    [Description("Removes defined file extentions from downloaded folder names")]
    public String? TrimRegex { get; set; } = @"(?i)\.(mkv|mp4|avi|m2ts|mov|wmv|asf|mpegts|ts|3gpp|flv|mpeg|wtv|webm|m4v|3gp|vob|ogv|rm|rmvb|divx|xvid|f4v|mts|mxf)$";

    [DisplayName("Automatic retry torrent")]
    [Description("When a single download has failed multiple times (see setting above) or when the torrent itself received an error it will retry the full torrent this many times before marking it failed.")]
    public Int32 TorrentRetryAttempts { get; set; } = 15;

    [DisplayName("Automatic retry downloads")]
    [Description("When a single download fails it will retry it this many times before marking it as failed.")]
    public Int32 DownloadRetryAttempts { get; set; } = 3;

    [DisplayName("Delete download when in error")]
    [Description("When a download has been in error for this many minutes, delete it from the provider and the client. 0 to disable.")]
    public Int32 DeleteOnError { get; set; } = 10;

    [DisplayName("Torrent maximum lifetime")]
    [Description("The maximum lifetime of a torrent in minutes. When this time has passed, mark the torrent as error. If the torrent is completed and has downloads, the lifetime setting will not apply. 0 to disable.")]
    public Int32 TorrentLifetime { get; set; } = 0;

    [DisplayName("Priority")]
    [Description("Set the priority of a torrent, 1 = highest, 0 = disabled.")]
    public Int32 Priority { get; set; } = 0;
}