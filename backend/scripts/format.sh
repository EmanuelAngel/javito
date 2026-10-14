#!/usr/bin/env bash
# Aplica el formato de .NET sobre la solución.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

dotnet format
