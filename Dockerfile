FROM steamcmd/steamcmd:latest

RUN apt-get update && apt-get install -y \
    wget \
    apt-transport-https \
    software-properties-common \
    gettext-base \
    wine64 \
    xvfb \
    && wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-8.0 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY RoR2Patcher/ ./RoR2Patcher/
COPY ["Risk of Rain 2/", "/game/"]
COPY config.cfg /app/config.cfg

COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

ENV MAX_PLAYERS=4
ENV STEAM_HEARTBEAT=1
ENV HOSTNAME="Risk of Rain 2 Dedicated Server"
ENV PORT=27015
ENV STEAM_QUERY_PORT=27016
ENV STEAM_SERVER_PORT=0
ENV SERVER_PASSWORD=""
ENV SERVER_CUSTOM_TAGS=""
ENV GAMEMODE="ClassicRun"
ENV EXTRA_ARGS=""

EXPOSE ${PORT}/udp ${STEAM_QUERY_PORT}/udp

ENTRYPOINT ["/app/entrypoint.sh"]