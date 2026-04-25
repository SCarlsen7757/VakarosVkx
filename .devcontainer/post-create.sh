#!/usr/bin/env bash
set -e

echo "🧱 Bootstrapping workspace environment..."

# Mark /workspace as safe for git (required when volume is owned by root)
git config --global --add safe.directory /workspace

echo "🔧 Restoring .NET tools..."
dotnet tool restore

echo "📦 Restoring solution..."
dotnet restore Vakaros.Vkx.slnx

echo "✅ Bootstrap complete"