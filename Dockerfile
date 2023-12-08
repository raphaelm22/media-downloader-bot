FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy AS base
RUN echo "deb http://raspbian.raspberrypi.org/raspbian/ bullseye main contrib non-free rpi" >> /etc/apt/sources.list
RUN apt update --allow-insecure-repositories
RUN apt install -y --allow-unauthenticated chromium
RUN apt install -y --allow-unauthenticated ffmpeg
RUN rm -rf /var/lib/apt/lists/*
RUN apt clean

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
COPY src/*/*.csproj ./

RUN for file in $(ls *.csproj); do \
    mkdir -p src/${file%.*}/ && \
    mv $file src/${file%.*}/; \
done
RUN dotnet restore ./src/MediaDownloaderBot/MediaDownloaderBot.csproj --use-current-runtime

FROM build AS publish
COPY ./src/ ./src/
RUN dotnet publish ./src/MediaDownloaderBot/MediaDownloaderBot.csproj \
    -c Release \
    --no-restore \
    -p:PublishSingleFile=true \
    --self-contained false \
    -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["./mediaDownloaderBot"]
