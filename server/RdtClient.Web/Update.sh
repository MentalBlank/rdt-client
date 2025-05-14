#!/bin/bash

# Define paths and URLs
INSTALL_DIR="/opt/rdtc"
INSTALLED_DIR="$INSTALL_DIR/.installed"
INSTALLED_FILE="$INSTALLED_DIR/version.txt"
BACKUP_DIR="$INSTALL_DIR/.backup"
GITHUB_API_URL="https://api.github.com/repos/mentalblank/rdt-client/releases/latest"
DOWNLOAD_URL=$(curl -s "$GITHUB_API_URL" | jq -r '.assets[0].browser_download_url')

# Function to check if a newer version is available
check_for_update() {
    local current_version
    if [ -f "$INSTALLED_FILE" ]; then
        current_version=$(cat "$INSTALLED_FILE")
    else
        mkdir -p $INSTALLED_DIR
        current_version="0.0.0"
    fi

    latest_version=$(curl -s "$GITHUB_API_URL" | jq -r '.name' | tr -d v)

    if [[ "$1" == "-force" ]]; then
        update
    fi

    if [[ "$current_version" == "$latest_version" ]]; then
        echo "The installed version ($current_version) is up to date."
    else
        echo "A newer version ($latest_version) is available."
        if [[ "$1" == "-update" ]]; then
            update
        elif [[ "$1" == "-check" ]]; then
            read -p "Do you want to update? (Y/n): " choice
            case "$choice" in
            [Yy]*)
                update
                ;;
            *)
                echo "Update canceled."
                ;;
            esac
        fi
    fi
}

# Function to update the software
update() {
    echo "Updating RDTClient..."

    # Download the latest zip file
    echo "Downloading Lastest RDTClient"
    curl -sLO "$DOWNLOAD_URL"
    ZIP_FILE=$(basename "$DOWNLOAD_URL")

    # Stop the rdtc service
    echo "Stopping RDTClient"
    sudo systemctl stop rdtc

    echo "Backing up RDTClient"
    cp $INSTALL_DIR/appsettings.json $BACKUP_DIR
    cp -r $INSTALL_DIR/db $BACKUP_DIR

    # Unzip the downloaded file
    echo "Unzipping $ZIP_FILE"
    unzip -q -o "$ZIP_FILE" -d "$INSTALL_DIR"

    # Store the new version in the installed file
    echo "Updating Latest Version file"
    echo "$latest_version" >"$INSTALLED_FILE"

    echo "Restoring Backup"
    cp $BACKUP_DIR/appsettings.json $INSTALL_DIR
    cp -r $BACKUP_DIR/db $INSTALL_DIR

    # Start the rdtc service
    echo "Starting RDTClient"
    sudo systemctl start rdtc

    echo "Update complete."
}

# Main script
case "$1" in
"-update")
    check_for_update "-update"
    ;;
"-force")
    update
    ;;
"-check")
    check_for_update "-check"
    ;;
"")
    check_for_update "-update"
    ;;
*)
    echo "Usage: $0 {-update|-force|-check}"
    exit 1
    ;;
esac

exit 0
