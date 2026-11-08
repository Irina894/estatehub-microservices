#!/usr/bin/env bash
# Розгортає db/p1 (схема, процедури, seed) у контейнер SQL Server, який запускає Aspire.
# sqlcmd виконується ВСЕРЕДИНІ контейнера, тому порт і мережа Windows не потрібні.
# Запуск з кореня репозиторію: SQL_PASSWORD='<пароль>' bash scripts/deploy-deals-db.sh
set -euo pipefail
: "${SQL_PASSWORD:?Задайте SQL_PASSWORD (той самий, що Parameters:sql-password)}"
export PATH="$PATH:/c/Program Files/Docker/Docker/resources/bin"
export MSYS_NO_PATHCONV=1   # Git Bash не має перетворювати шляхи /tmp/... на Windows-шляхи

CONTAINER=$(docker ps --filter "name=sql-" --format "{{.Names}}" | head -n 1)
[ -n "$CONTAINER" ] || { echo "Контейнер SQL Server не знайдено. Запустіть AppHost."; exit 1; }

SQLCMD=$(docker exec "$CONTAINER" sh -c 'ls /opt/mssql-tools18/bin/sqlcmd 2>/dev/null || ls /opt/mssql-tools/bin/sqlcmd 2>/dev/null' | head -n 1)
[ -n "$SQLCMD" ] || { echo "У контейнері немає sqlcmd"; exit 1; }
echo "Контейнер: $CONTAINER, sqlcmd: $SQLCMD"

docker exec "$CONTAINER" mkdir -p /tmp/p1
docker cp db/p1/. "$CONTAINER:/tmp/p1/"

for f in schema procedures seed; do
  echo "== db/p1/$f.sql"
  docker exec "$CONTAINER" "$SQLCMD" -S localhost -U sa -P "$SQL_PASSWORD" -C -b -f 65001 -i "/tmp/p1/$f.sql"
done
echo "Deals БД готова."
