#!/usr/bin/env bash
set -e

echo "🧱 Bootstrapping workspace environment..."

# 1. Ensure deterministic folders exist
mkdir -p /workspace/.dotnet
mkdir -p /workspace/.nuget/packages
mkdir -p /workspace/.nuget/http-cache

# 2. Fix permissions (safety net)
chown -R vscode:vscode /workspace/.nuget || true
chown -R vscode:vscode /workspace/.dotnet || true

echo "⏳ Waiting for VS Code + filesystem stabilization..."
sleep 3

echo "🔧 Restoring .NET tools..."
dotnet tool restore

echo "📦 Restoring solution..."
dotnet restore Vakaros.Vkx.slnx

echo "✅ Bootstrap complete"