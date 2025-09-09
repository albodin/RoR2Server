#!/bin/bash
set -e

GAME_DIR="/game"
ROR2_DLL_PATH="${GAME_DIR}/Risk of Rain 2_Data/Managed/RoR2.dll"
BACKUP_PATH="${ROR2_DLL_PATH}.bak"
PATCHED_DLL="RoR2_Patched.dll"
CONFIG_DIR="${GAME_DIR}/Risk of Rain 2_Data/Config"

echo "Risk of Rain 2 Dedicated Server Starting..."

if [ ! -d "$GAME_DIR" ]; then
    echo "ERROR: Game directory not found at $GAME_DIR"
    echo "Please mount your Risk of Rain 2 game directory to /game"
    exit 1
fi

if [ ! -f "$ROR2_DLL_PATH" ]; then
    echo "ERROR: RoR2.dll not found at $ROR2_DLL_PATH"
    exit 1
fi

cp -f /tmp/steamworks_sdk/*64.dll "${GAME_DIR}/" 2>/dev/null || true

if [ ! -f "$BACKUP_PATH" ]; then
    echo "Backup not found. Patching RoR2.dll..."
    
    cp "$ROR2_DLL_PATH" "$BACKUP_PATH"
    echo "Created backup: $BACKUP_PATH"
    
    echo "Building patcher..."
    cd /app/RoR2Patcher
    dotnet build -c Release
    
    echo "Running patcher..."
    dotnet run -c Release -- --input "$ROR2_DLL_PATH" --output "/app/$PATCHED_DLL"
    
    cp "/app/$PATCHED_DLL" "$ROR2_DLL_PATH"
    echo "Applied patch to RoR2.dll"
else
    echo "Backup found. Skipping patching."
fi

echo "Processing configuration..."
mkdir -p "$CONFIG_DIR"
envsubst < /app/config.cfg > "$CONFIG_DIR/server.cfg"

winecfg

cd "$GAME_DIR"
echo "Starting Risk of Rain 2 Dedicated Server..."

export DISPLAY=:99
Xvfb :99 -screen 0 1024x768x24 &
sleep 5

SERVER_ARGS="-batchmode -nographics $EXTRA_ARGS"
wine "Risk of Rain 2.exe" $SERVER_ARGS &
WINE_PID=$!

LOG_PATH="/root/.wine/drive_c/users/root/AppData/LocalLow/Hopoo Games, LLC/Risk of Rain 2/Player.log"
echo "Waiting for log file at: $LOG_PATH"

while [ ! -f "$LOG_PATH" ]; do
    if ! kill -0 $WINE_PID 2>/dev/null; then
        echo "Wine process died before creating log file"
        exit 1
    fi
    sleep 2
done

echo "Log file found, tailing output..."
tail -f "$LOG_PATH"