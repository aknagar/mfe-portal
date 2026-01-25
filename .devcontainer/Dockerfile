# Base image with common development tools
FROM mcr.microsoft.com/devcontainers/base:ubuntu-22.04

# Avoid warnings by switching to noninteractive
ENV DEBIAN_FRONTEND=noninteractive

# Install additional OS packages
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        curl \
        wget \
        git \
        gnupg2 \
        lsb-release \
        ca-certificates \
        apt-transport-https \
        software-properties-common \
        libc6 \
        libgcc1 \
        libgssapi-krb5-2 \
        libicu70 \
        libssl3 \
        libstdc++6 \
        zlib1g \
        jq \
        unzip \
    && apt-get autoremove -y && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# Install .NET Aspire workload (will be done after .NET is installed via features)
# This is handled in post-create.sh

# Create command history directory
RUN mkdir -p /commandhistory \
    && touch /commandhistory/.bash_history \
    && chown -R vscode:vscode /commandhistory

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    NUGET_XMLDOC_MODE=skip \
    ASPNETCORE_URLS=http://+:5000 \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

# Switch back to dialog for any ad-hoc use of apt-get
ENV DEBIAN_FRONTEND=dialog
