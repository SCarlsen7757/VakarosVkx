FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0

# Install Node.js 24 LTS
ARG NODE_VERSION=24
RUN curl -fsSL https://deb.nodesource.com/setup_${NODE_VERSION}.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*
