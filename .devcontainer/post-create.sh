#!/usr/bin/env bash
set -e

echo "🔧 Restoring .NET tools..."
dotnet tool restore

echo "📦 Restoring .NET dependencies..."
dotnet restore Vakaros.Vkx.Api/Vakaros.Vkx.Api.csproj

echo "📦 Installing frontend dependencies..."
cd Vakaros.Vkx.Web
npm ci

echo "✅ Devcontainer setup complete"