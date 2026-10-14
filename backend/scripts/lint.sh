#!/usr/bin/env bash
# Verifica el formato sin modificar archivos. Pensado para ejecutarse antes de commitear.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

dotnet format --verify-no-changes
