# Unity dedicated game server container
# Expects a pre-built Linux x86_64 server in ./ServerBuild/
# Build the Unity project first:
#   unity -batchmode -quit -projectPath . -executeMethod ServerBuildScript.BuildLinuxServer
# Then build the image:
#   docker build -t spacefighter-server .

FROM ubuntu:22.04

# Install Unity Linux server runtime dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgcc-s1 \
    libstdc++6 \
    libssl3 \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the Unity server build output
COPY ./ServerBuild/ .

RUN chmod +x ./SpaceFighter

# UDP for Unity Netcode game traffic (matches GameBootstrap.AutoConnectPort)
EXPOSE 7979/udp
# TCP fallback
EXPOSE 7979/tcp

ENTRYPOINT ["./SpaceFighter", "-batchmode", "-nographics", "-logFile", "-"]
