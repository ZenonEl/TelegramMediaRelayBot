FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Only NuGet (Microsoft/Azure) may be unreachable from the host's tunnel exit;
# route just the restore/publish steps through the proxy. apt/pypi work direct,
# so they are deliberately left un-proxied (apt can't speak SOCKS anyway).
ARG BUILD_PROXY=
WORKDIR /src
COPY *.csproj ./
RUN http_proxy=$BUILD_PROXY https_proxy=$BUILD_PROXY dotnet restore TelegramMediaRelayBot.csproj
COPY . .
# Publish the app only (not the test project) as a framework-dependent .dll to
# match the runtime image and the `dotnet ...dll` entrypoint.
RUN http_proxy=$BUILD_PROXY https_proxy=$BUILD_PROXY \
    dotnet publish TelegramMediaRelayBot.csproj -c Release -o /app \
    --self-contained false -p:PublishSingleFile=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 python3-pip ffmpeg && \
    pip3 install --break-system-packages yt-dlp gallery-dl pysocks && \
    apt-get clean && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENTRYPOINT ["dotnet", "TelegramMediaRelayBot.dll"]
