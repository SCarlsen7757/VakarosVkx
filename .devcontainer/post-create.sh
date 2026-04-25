#!/usr/bin/env bash
set -e

echo "🧱 Bootstrapping workspace environment..."

# Mark /workspace as safe for git (required when volume is owned by root)
git config --global --add safe.directory /workspace

# Remove build artifacts that may be owned by the Windows host user.
# If left in place, the vscode user gets MSB3374 (cannot set timestamps) during build.
echo "🧹 Cleaning build artifacts..."
find /workspace -name "obj" -type d -not -path "*/node_modules/*" -exec rm -rf {} + 2>/dev/null || true
find /workspace -name "bin" -type d -not -path "*/node_modules/*" -exec rm -rf {} + 2>/dev/null || true

echo "🌐 Installing web dependencies..."
cd /workspace/Vakaros.Vkx.Web && npm install

echo "🔧 Restoring .NET tools..."
dotnet tool restore

echo "📦 Restoring solution..."
dotnet restore Vakaros.Vkx.slnx

echo "✅ Bootstrap complete"