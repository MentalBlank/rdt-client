# RDT-Client
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0) [![Docker](https://img.shields.io/badge/docker-available-blue.svg)](README-DOCKER.md) [![License](https://img.shields.io/github/license/mentalblank/rdt-client.svg)](LICENSE) [![Releases](https://img.shields.io/github/v/release/mentalblank/rdt-client.svg)](https://github.com/mentalblank/rdt-client/releases)

RDT-Client is a web interface to manage your torrents across Real-Debrid, AllDebrid, Premiumize, TorBox, and Debrid-Link.

## Features
- Add new torrents via magnet links or files.
- Download all files from a supported Debrid provider automatically.
- Auto-unpack files after download.
- Fake qBittorrent API to integrate easily with Sonarr, Radarr, CouchPotato, etc.
- Run as a background service on Windows or Linux.
- Built with Angular 15 and .NET 9.

> **Note: You must have a premium account with Real-Debrid, AllDebrid, Premiumize, TorBox, or Debrid-Link!**

### Debrid Provider Registration:
- [Real-Debrid.](https://real-debrid.com/)
- [AllDebrid.](https://alldebrid.com/)
- [Premiumize.](https://www.premiumize.me/)
- [TorBox.](https://torbox.app/)
- [Debrid-Link.](https://debrid-link.fr/)

### Docker Setup
We strongly recommend using Docker for the easiest deployment.

See the full [Docker Setup Guide](README-DOCKER.md).

### Windows Service Setup

1. Install the [ASP.NET Core Runtime 9.0.0](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Download the latest [GitHub release archive](https://github.com/mentalblank/rdt-client/releases) and extract it.
3. Edit `appsettings.json`:
   - Update `LogLevel` `Path` to a local directory.
   - Update `Database` `Path` to a local directory.
   - **Tip:** Windows paths must use escaped slashes, e.g.,  
     `D:\\RdtClient\\db\\rdtclient.db`
4. Start the service:
   - To run manually:  
     ```bash
     RdtClient.Web.exe
     ```
   - To install as a Windows Service:  
     ```bash
     service-install.bat
     ```
   This installs the client to run automatically in the background when the system starts.

### Linux Service Setup

1. Use the [Official .NET Installation Guide](https://docs.microsoft.com/en-us/dotnet/core/install/linux) to Install .NET SDK 9.0

   Example for **Ubuntu 20.04**:
   ```bash
   wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   rm packages-microsoft-prod.deb
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-9.0```

2. Download the latest [GitHub release archive](https://github.com/mentalblank/rdt-client/releases):  
   ```bash
   wget <zip_url>
   ```   
4. Extract to the path of your choice (~/rtdc in this example):  
   ```bash
   unzip RealDebridClient.zip -d ~/rdtc
   cd ~/rdtc
   ```
4. Configure ```appsettings.json```
   - Update the Database Path to a valid local directory.
   - Ensure the directory exists
   - **Tip:** You can remove the /data/db/ part if you want to simplify the path.
6. Test rdt client runs ok:  
   ```bash
   dotnet RdtClient.Web.dll
   ```
7. Navigate to ```http://<youripaddress>:6500``` and check it all works
8. Create a service (systemd in this example):  
   ```bash
   sudo nano /etc/systemd/system/rdtc.service
   ```  
9. Paste and edit the following to include your directory path:
   ```bash
   [Unit]
   Description=RdtClient Service

   [Service]

   WorkingDirectory=/home/<username>/rdtc
   ExecStart=/usr/bin/dotnet RdtClient.Web.dll
   SyslogIdentifier=RdtClient
   User=<username>

   [Install]
   WantedBy=multi-user.target
   ```
10. Save the file.
11. Enable and start the service:   
    ```bash
    sudo systemctl daemon-reload
    sudo systemctl enable rdtc
    sudo systemctl start rdtc
    ```  

### Proxmox LXC Setup
If you are using Proxmox, you can easily run RDT-Client inside a Linux container (LXC).

[Check out the Proxmox guide by tteck](https://tteck.github.io/Proxmox/)

---

## Setup

### First Login

1. Open your browser and go to:  
   [http://127.0.0.1:6500](http://127.0.0.1:6500) (or replace with your server's IP/hostname).

2. Enter your login credentials.  
   > **Note:** The **first** credentials you enter will be saved for future logins.

3. Click on **`Settings`** at the top, and enter your provider's API key:

   - **Real-Debrid:** [Get your API Key](https://real-debrid.com/apitoken)
   - **AllDebrid:** [Get your API Key](https://alldebrid.com/apikeys)
   - **Premiumize:** [Get your API Key](https://www.premiumize.me/account)
   - **TorBox:** [Get your API Key](https://torbox.app/settings)
   - **Debrid-Link:** [Get your API Key](https://debrid-link.com/webapp/apikey)

4. **Set your Download Path:**
   - If using **Docker**, this must match the path you mapped in your Docker container (default: `/data/downloads`).
   - On **Windows**, set this to a path on your host system.

5. **Set your Mapped Path:**
   - This is the local host destination path corresponding to your Docker volume mapping.
   - On **Windows**, this is usually the **same** as your Download Path.

6. **Save** your settings.

---

## Download Clients

Currently, there are **five** available download clients:

### Internal Downloader

This experimental [Internal Downloader](https://github.com/mentalblank/Downloader.NET) can download files using multiple sections in parallel.

**Options:**
- **Download Speed (in MB/s):**  
  Maximum speed per download across all parallel downloads and chunks.
- **Parallel Connections per Download:**  
  Number of sections downloaded in parallel for each file.
- **Connection Timeout:**  
  Timeout in milliseconds for a chunk download.  
  > Each chunk will retry up to **5 times** before failing completely.

### Bezzad Downloader

This [Bezzad Downloader](https://github.com/bezzad/Downloader) downloads files in parallel with multiple chunks.

**Options:**
- **Download Speed (in MB/s):**  
  Maximum speed per download across all parallel downloads and chunks.
- **Parallel Connections per Download:**  
  Number of parallel downloads per file.  
  > Recommended maximum: **8**.
- **Parallel Chunks per Download:**  
  Number of chunks each download is split into.  
  > Recommended maximum: **8**.
- **Connection Timeout:**  
  Timeout in milliseconds before a download chunk times out.  
  > Each chunk will retry up to **5 times** before failing completely.

### Aria2c Downloader

This method uses an external [Aria2c](https://aria2.github.io/) download client.  
> You must install Aria2c manually on your host; it is not included in the Docker image.

**Options:**
- **URL:**  
  Full URL to your Aria2c service. Must end with `/jsonrpc`.  
  Example: `http://192.168.10.2:6800/jsonrpc`
- **Secret:**  
  An optional secret token to connect to Aria2c.

> If Aria2c is selected, none of the `Internal Downloader` settings are used. You must configure Aria2c manually.

### Symlink Downloader

The Symlink downloader requires an **rclone** mount point integrated into your filesystem.

> Make sure the exact path to the mounted files matches across all applications, including rdt-client, otherwise, symlinks will not resolve correctly.
> If the mount path cannot be found, downloads will not start.

**Required Configuration:**
- **Post Download Action:**  
  DO NOT SELECT REMOVE FROM PROVIDER.
- **Rclone Mount Path:**  
  Example: `/PATH_TO_YOUR_RCLONE_MOUNT/torrents/`

**Suggested Configuration:**
- **Automatic Retry Downloads:**  
  Set retries to **greater than 3**.

### Synology Download Station

The **Synology Download Station** downloader uses an external **Download Station server**.  
> You must install Synology Download Station manually on your host; it is not included in the Docker image

**Options:**
- **URL:**  
  Full URL to your Synology Download Station.  
  Example: `http://127.0.0.1:5000`
- **Username:**  
  Username used to authenticate with the Synology Download Station.
- **Password:**  
  Password used to authenticate with the Synology Download Station.
- **Download Path:**  
  The root path where files will be saved on the Synology host.  
  > If left empty, the default path configured in Download Station will be used.

---

## Connecting Sonarr/Radarr

RdtClient emulates the **qBittorrent web protocol** and allows applications to use those APIs. This way, you can use **Sonarr** and **Radarr** to download directly from debrid providers.

### Steps:

1. Login to **Sonarr** or **Radarr** and click `Settings`.
2. Go to the `Download Client` tab and click the plus to add a new client.
3. Select `qBittorrent` from the list.
4. In the `Host` field, enter the IP or hostname of **RDT-Client**.
5. In the `Port` field, enter `6500`.
6. Enter your **Username/Password** that you set up earlier in the appropriate fields.
7. Set the **category** to `sonarr` for Sonarr or `radarr` for Radarr.
8. Leave the other settings as they are.
9. Click `Test`, and if all is well, hit `Save`.
10. **Sonarr** will now treat **RDT-Client** as a regular torrent client.

### Important Notes:
- When downloading, the **category** setting from Sonarr/Radarr will be appended to your download path. For example, if your **Remote Path** is set to `C:\Downloads` and your **Sonarr** Download Client's **category** is set to `sonarr`, files will be downloaded to `C:\Downloads\sonarr`.
- **Progress and ETA:** The progress and ETA shown in Sonarr's **Activity** tab will not be accurate, but once the download finishes, the torrent will be marked as completed and can be processed.
  
---

## Troubleshooting

- **Forgot your logins?**  
  Simply delete the `rdtclient.db` file and restart the service.

- **Log file for issues:**  
  A log file is written to your persistent path as `rdtclient.log`.  
  If you run into issues, change the log level in your Docker script to `Debug` for more detailed logs.

---

## Running within a Folder

By default, the application runs in the root of your hosted address (e.g., `https://rdt.myserver.com/`).  
However, if you want to run it as a relative folder (e.g., `https://myserver.com/rdt`), you will need to change the `BasePath` setting in the `appsettings.json` file. 

### Docker Environments:
For Docker environments, you can set the `BASE_PATH` environment variable.

---

## Build Instructions

### Prerequisites

- NodeJS
- NPM
- Angular CLI
- .NET 9
- Visual Studio 2022
- (optional) Resharper

### Steps to Build the Project:

1. Open the client folder project in VS Code and run `npm install`.
2. To debug, run `ng serve`, to build, run `ng build -c production`.
3. Open the Visual Studio 2019 project `RdtClient.sln` and `Publish` the `RdtClient.Web` to the given `PublishFolder` target.
4. When debugging, make sure to run `RdtClient.Web.dll` and not `IISExpress`.
5. The result is found in `Publish`.

### Build Docker Container

1. In the root of the project, run:  
   `docker build --tag rdtclient .`
   
2. To create the Docker container, run:  
   `docker run --publish 6500:6500 --detach --name rdtclientdev rdtclient:latest`

3. To stop the container, run:  
   `docker stop rdtclient`

4. To remove the container, run:  
   `docker rm rdtclient`

5. Alternatively, use `docker-build.bat` for convenience.

---

## Misc Install Notes

### Rootless Podman, Linux Host, and CIFS Connections

RDT Client's read and write permission tests might fail if the CIFS connection is not set up properly, despite permissions appearing correct. In the Web GUI, it will report **"Access Denied"**, and in the log file, you will see exceptions like this ([dotnet issue](https://github.com/dotnet/runtime/issues/42790)): 
```bash
System.IO.IOException: Permission denied
```

The **nobrl** option must be specified in your CIFS connection. [Refer to the man page](https://linux.die.net/man/8/mount.cifs) for details.  
Example:
```bash
Options=_netdev,credentials=/etc/samba/credentials/600file,rw,uid=SUBUID,gid=SBUGID,nobrl,file_mode=0770,dir_mode=0770,noperm
```

