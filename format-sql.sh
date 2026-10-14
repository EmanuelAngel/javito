#!/usr/bin/env bash

# Script para formatear archivos SQL usando sql-formatter vía pnpm dlx (o npx).
# Uso:
#   ./format-sql.sh                    -> Formatea todos los archivos .sql del proyecto
#   ./format-sql.sh database.sql       -> Formatea un archivo específico
#   echo "SELECT * FROM t" | ./format-sql.sh -> Formatea desde stdin y muestra en stdout

set -e

if command -v pnpm >/dev/null 2>&1; then
  RUN_SQLFORMATTER="pnpm --package=sql-formatter dlx sql-formatter --config .sql-formatter.json"
else
  RUN_SQLFORMATTER="npx --yes sql-formatter --config .sql-formatter.json"
fi

if [ -n "$1" ]; then
  if [ -f "$1" ]; then
    echo "✨ Formateando '$1'..."
    $RUN_SQLFORMATTER --fix "$1"
    echo "✅ Archivo formateado: $1"
  else
    echo "❌ Error: El archivo '$1' no existe."
    exit 1
  fi
elif [ ! -t 0 ]; then
  # Entrada por stdin / pipe
  $RUN_SQLFORMATTER
else
  # Por defecto busca y formatea todos los .sql en el repositorio
  echo "🔍 Buscando archivos .sql para formatear..."
  find . -name "*.sql" -not -path "*/.*" -not -path "*/bin/*" -not -path "*/obj/*" | while read -r sql_file; do
    echo "✨ Formateando $sql_file..."
    $RUN_SQLFORMATTER --fix "$sql_file"
  done
  echo "✅ Todos los archivos .sql fueron formateados."
fi
