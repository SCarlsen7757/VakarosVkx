FROM mcr.microsoft.com/devcontainers/dotnet:10.0

ARG NODE_VERSION=24

# Install pinned Node.js version via nvm (nvm is pre-installed in the base image)
RUN su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"
