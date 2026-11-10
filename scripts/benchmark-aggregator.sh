#!/usr/bin/env bash
# Порівнює тривалість агрегувального запиту: HTTP (Лаб. 5) і gRPC (Лаб. 6), а також gRPC з кешем агрегата.
# Усі варіанти йдуть через Gateway, тому різниця - це лише транспорт між Aggregator і сервісами.
# Запуск: bash scripts/benchmark-aggregator.sh <id заявки> [кількість запитів, за замовчуванням 30]
set -euo pipefail

ID="${1:?Вкажіть id заявки (GUID)}"
N="${2:-30}"
BASE="${GATEWAY:-http://localhost:5000}"
URL="$BASE/api/aggregator/applications/$ID/full"

measure() {   # $1 = назва, $2 = URL
  echo -n "$1: "
  for _ in 1 2 3; do curl -s -o /dev/null "$2"; done      # прогрів: JIT, TLS, HTTP/2-з'єднання
  for _ in $(seq 1 "$N"); do curl -s -o /dev/null -w '%{time_total}\n' "$2"; done \
    | awk '{ printf "%.3f\n", $1 * 1000 }' | sort -n \
    | awk '{ a[NR] = $1; s += $1 }
           END { printf "середнє %.1f мс | мін %.1f | медіана %.1f | p95 %.1f  (n=%d)\n",
                 s / NR, a[1], a[int((NR + 1) / 2)], a[int(NR * 0.95) < 1 ? 1 : int(NR * 0.95)], NR }'
}

echo "Заявка $ID, $N запитів на варіант (nocache=true: кеш агрегата вимкнено)"
measure "HTTP  (Лаб. 5)          " "$URL?transport=http&nocache=true"
measure "gRPC  (Лаб. 6)          " "$URL?transport=grpc&nocache=true"
measure "gRPC + кеш агрегата     " "$URL"
