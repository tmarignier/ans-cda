#!/usr/bin/env bash
# Prépare l'environnement Claude Code (web) : SDK .NET 10 + restauration NuGet de la solution.
set -euo pipefail

if [[ "${CLAUDE_CODE_REMOTE:-}" != "true" ]]; then
  exit 0
fi

if ! dotnet --list-sdks 2>/dev/null | grep -q "^10\."; then
  # dot.net/builds.dotnet.microsoft.com peuvent être bloqués par le proxy : on passe par apt (Ubuntu).
  apt-get install -y -qq dotnet-sdk-10.0 >/dev/null 2>&1 \
    || { apt-get update -qq >/dev/null 2>&1 && apt-get install -y -qq dotnet-sdk-10.0 >/dev/null 2>&1; }
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet restore "$CLAUDE_PROJECT_DIR/dotnet/CdaCrImg.sln" >/dev/null
