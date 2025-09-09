# Risk of Rain 2 Dedicated Server Patcher

This project patches the Risk of Rain 2 game client to function as a dedicated server, since the official dedicated server is outdated.

**Note:** A pre-built Docker image is not provided as it would require distributing the game files. You must use your own copy of Risk of Rain 2.

## Requirements

- Docker and Docker Compose
- Risk of Rain 2 game files (Steam version)

## Setup

1. Copy your `Risk of Rain 2` folder to this project directory
   - On Windows: Usually found in `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2`
   - On Linux: Usually found in `~/.steam/steam/steamapps/common/Risk of Rain 2`

2. Build the Docker image:
   ```bash
   docker compose build
   ```

If you wish to run the server on a different machine save the image using (assuming you didn't change the image name in docker-compose.yml):
``` bash
docker save -o ror2serverImage.tar ror2server-ror2-server:latest
```

Then after transferring to the new machine you can load and use it:
```bash
docker load -i ror2serverImage.tar
``` 

## Configuration

Server settings can be configured through environment variables in `docker-compose.yml` or by creating a `.env` file:

- `MAX_PLAYERS` - Maximum number of players (default: 4)
- `STEAM_HEARTBEAT` - Enable Steam server browser listing (default: 1)
- `SERVER_HOSTNAME` - Server name (default: "Risk of Rain 2 Dedicated Server")
- `PORT` - Game port (default: 27015)
- `STEAM_QUERY_PORT` - Steam query port (default: 27016)
- `STEAM_SERVER_PORT` - Steam server port, 0 for random (default: 0)
- `SERVER_PASSWORD` - Server password, leave empty for no password (default: "")
- `SERVER_CUSTOM_TAGS` - Custom server tags (default: "")
- `GAMEMODE` - Game mode: ClassicRun, WeeklyRun, EclipseRun, InfiniteTowerRun (default: ClassicRun)
- `EXTRA_ARGS` - Additional command line arguments (default: "")

## Running the Server

Start the server:
```bash
docker compose up
```

Run in background:
```bash
docker compose up -d
```

Stop the server:
```bash
docker compose down
```

## Connecting to the Server

To connect to the server:
1. Open the in-game console by pressing ``Ctrl+Alt+` ``
2. Type: `connect "YOUR_IP:PORT"`
