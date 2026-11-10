#!/usr/bin/env bash
# Лаб. 6: підключає NuGet-пакети, .proto-файли в .csproj і накладає готові файли (кеш, gRPC, Aggregator на gRPC, Redis в AppHost).
# Запуск з КОРЕНЯ репозиторію (Git Bash), AppHost має бути ЗУПИНЕНИЙ:   bash scripts/setup-lab06.sh /шлях/до/lab06
set -euo pipefail

OVERLAY="${1:?Вкажіть шлях до розпакованої теки lab06}"
[ -d "$OVERLAY/Platform.Aggregator" ] || { echo "У $OVERLAY немає Platform.Aggregator"; exit 1; }
[ -f Platform.slnx ] || { echo "Запускайте з кореня репозиторію (де Platform.slnx)"; exit 1; }
[ -d Platform.ServiceDefaults ] || { echo "Немає Platform.ServiceDefaults: спершу має бути виконана Лаб. 5 (гілка lab06 від lab05)"; exit 1; }

echo "== .NET SDK: $(dotnet --version)"

CATALOG_API=Services/Catalog/Catalog.Api/Catalog.Api.csproj
DEALS_API=Services/Deals/Deals.Api/Deals.Api.csproj
REVIEWS_API=Services/Reviews/Reviews.WebApi/Reviews.WebApi.csproj
AGG=Platform.Aggregator/Platform.Aggregator.csproj

echo "== 1. NuGet-пакети"
# Redis у AppHost (та сама версія, що й Aspire.Hosting.MongoDB з Лаб. 5)
dotnet add Platform.AppHost package Aspire.Hosting.Redis --version 13.6.1
# локальний кеш у шарах BLL / Application
dotnet add Services/Catalog/Catalog.Bll/Catalog.Bll.csproj package Microsoft.Extensions.Caching.Memory
dotnet add Services/Deals/Deals.Bll/Deals.Bll.csproj package Microsoft.Extensions.Caching.Memory
dotnet add Services/Reviews/Reviews.Application/Reviews.Application.csproj package Microsoft.Extensions.Caching.Memory
# у кожному мікросервісі: gRPC-сервер і IDistributedCache поверх Redis через Aspire
for p in "$CATALOG_API" "$DEALS_API" "$REVIEWS_API"; do
  dotnet add "$p" package Grpc.AspNetCore
  dotnet add "$p" package Aspire.StackExchange.Redis.DistributedCaching
done
# interceptor логування gRPC лежить у ServiceDefaults
dotnet add Platform.ServiceDefaults package Grpc.AspNetCore.Server
# Aggregator: gRPC-клієнти, генерація коду з .proto, кеш агрегатів
dotnet add "$AGG" package Grpc.Net.ClientFactory
dotnet add "$AGG" package Google.Protobuf
dotnet add "$AGG" package Grpc.Tools
dotnet add "$AGG" package Aspire.StackExchange.Redis.DistributedCaching

echo "== 2. Підключення .proto-файлів (Server у сервісах, Client в Aggregator)"
add_protobuf() {   # $1 = .csproj, $2 = рядок <Protobuf .../>
  if grep -q "$2" "$1"; then echo "   вже є в $1"; return; fi
  sed -i "s#</Project>#  <ItemGroup>\n    $2\n  </ItemGroup>\n\n</Project>#" "$1"
  echo "   додано в $1"
}
add_protobuf "$CATALOG_API" '<Protobuf Include="Protos/catalog.proto" GrpcServices="Server" />'
add_protobuf "$DEALS_API"   '<Protobuf Include="Protos/deals.proto" GrpcServices="Server" />'
add_protobuf "$REVIEWS_API" '<Protobuf Include="Protos/reviews.proto" GrpcServices="Server" />'
# Aggregator бере ТІ САМІ файли, але генерує лише клієнтську частину
add_protobuf "$AGG" '<Protobuf Include="../Services/Catalog/Catalog.Api/Protos/catalog.proto" GrpcServices="Client" />'
add_protobuf "$AGG" '<Protobuf Include="../Services/Deals/Deals.Api/Protos/deals.proto" GrpcServices="Client" />'
add_protobuf "$AGG" '<Protobuf Include="../Services/Reviews/Reviews.WebApi/Protos/reviews.proto" GrpcServices="Client" />'

echo "== 3. Aggregator: стару HTTP-службу замінює HttpApplicationAggregationService (її додає накладка)"
rm -f Platform.Aggregator/Services/ApplicationAggregationService.cs

echo "== 4. Накладання готових файлів"
cp -r "$OVERLAY"/Platform.ServiceDefaults/. Platform.ServiceDefaults/
cp -r "$OVERLAY"/Platform.Aggregator/.      Platform.Aggregator/
cp -r "$OVERLAY"/Platform.AppHost/.         Platform.AppHost/
cp -r "$OVERLAY"/Services/.                 Services/
mkdir -p scripts
cp "$OVERLAY"/scripts/benchmark-aggregator.sh scripts/

echo "== 5. Збірка (тут генеруються класи з .proto)"
dotnet build Platform.slnx
echo
echo "ГОТОВО. Далі:"
echo "  1) dotnet dev-certs https --trust          (один раз; потрібен для gRPC поверх HTTPS)"
echo "  2) dotnet run --project Platform.AppHost   (у Dashboard має з'явитися ресурс redis)"
echo "  3) bash scripts/benchmark-aggregator.sh <id заявки>   (порівняння HTTP і gRPC)"
