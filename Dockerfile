# Command line converter in a container, with FFmpeg for MP3/OGG/FLAC/... import and export.
#
#   docker run --rm -v "$PWD:/data" ghcr.io/falloutboy2099/acmtools to-audio sound.acm sound.ogg
#
# The image is built for amd64 and arm64 from any machine (the build is managed code, no native compilation).

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG VERSION=0.0.0
WORKDIR /src
COPY . .
RUN case "$TARGETARCH" in arm64) RID=linux-arm64 ;; *) RID=linux-x64 ;; esac \
    && dotnet publish AcmTools.Cli/AcmTools.Cli.csproj -c Release -r "$RID" --self-contained false \
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:Version="$VERSION" -o /out

FROM mcr.microsoft.com/dotnet/runtime:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /out/AcmTools.Cli /usr/local/bin/acmconverter
WORKDIR /data
ENTRYPOINT ["acmconverter"]