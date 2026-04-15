#!/bin/bash
# scripts/build-blazor.sh
# Publica o Blazor WASM e copia os artefatos para o Next.js public/_blazor/

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$SCRIPT_DIR/.."
BLAZOR_DIR="$ROOT/frontend/apps/blazor-modules"
NEXTJS_PUBLIC="$ROOT/frontend/apps/next-shell/public/_blazor"

echo "🔷 Building Blazor WASM..."
dotnet publish "$BLAZOR_DIR" -c Release -o "$BLAZOR_DIR/publish" --nologo

echo "📁 Copying artifacts to Next.js public/_blazor/..."
rm -rf "$NEXTJS_PUBLIC"
mkdir -p "$NEXTJS_PUBLIC"
cp -r "$BLAZOR_DIR/publish/wwwroot/." "$NEXTJS_PUBLIC/"

echo "✅ Blazor artifacts ready at: $NEXTJS_PUBLIC"
echo "   → _framework/: $(ls "$NEXTJS_PUBLIC/_framework/" | wc -l | tr -d ' ') files"
