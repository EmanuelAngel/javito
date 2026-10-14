#!/usr/bin/env bash
# Compila la solución y ejecuta las pruebas del backend.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

dotnet build --nologo
dotnet test --nologo
