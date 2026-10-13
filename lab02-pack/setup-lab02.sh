#!/bin/bash
# Запускати з Git Bash:  bash lab02-pack/setup-lab02.sh
# Робить усе для Лаб. 02: гілка, рішення, 4 проєкти, пакети, код, Aspire AppHost, README.
set -e
cd "$(dirname "$0")/.."            # корінь репозиторію
PACK=lab02-pack

echo "== 1. Гілка lab-02 =="
git checkout -b lab-02 2>/dev/null || git checkout lab-02

echo "== 2. Рішення Platform.slnx =="
[ -f Platform.slnx ] || dotnet new sln -n Platform --format slnx

echo "== 3. Проєкти сервісу Deals =="
mkdir -p Services/Deals
pushd Services/Deals >/dev/null
[ -d Deals.Api ]    || dotnet new webapi -n Deals.Api --use-controllers
[ -d Deals.Bll ]    || dotnet new classlib -n Deals.Bll
[ -d Deals.Dal ]    || dotnet new classlib -n Deals.Dal
[ -d Deals.Domain ] || dotnet new classlib -n Deals.Domain
rm -f Deals.Bll/Class1.cs Deals.Dal/Class1.cs Deals.Domain/Class1.cs
rm -f Deals.Api/Controllers/WeatherForecastController.cs Deals.Api/WeatherForecast.cs

echo "== 4. Посилання між проєктами та NuGet-пакети =="
dotnet add Deals.Api reference Deals.Bll
dotnet add Deals.Bll reference Deals.Dal Deals.Domain
dotnet add Deals.Dal reference Deals.Domain
dotnet add Deals.Dal package Microsoft.Data.SqlClient
dotnet add Deals.Dal package Dapper
dotnet add Deals.Bll package AutoMapper --version 14.0.0
dotnet add Deals.Bll package Microsoft.Extensions.Caching.Memory
dotnet add Deals.Api package AutoMapper --version 14.0.0
dotnet add Deals.Api package Swashbuckle.AspNetCore
popd >/dev/null

echo "== 5. Копіюємо код (перезаписує Program.cs та appsettings.json) =="
cp -rf $PACK/Services/. Services/

dotnet sln Platform.slnx add Services/Deals/Deals.Api Services/Deals/Deals.Bll Services/Deals/Deals.Dal Services/Deals/Deals.Domain

echo "== 6. Aspire AppHost =="
dotnet new install Aspire.ProjectTemplates || true
[ -d Platform.AppHost ] || dotnet new aspire-apphost -n Platform.AppHost
rm -f Platform.AppHost/Program.cs
cp -f $PACK/Platform.AppHost/AppHost.cs Platform.AppHost/AppHost.cs
dotnet add Platform.AppHost reference Services/Deals/Deals.Api
dotnet add Platform.AppHost package Aspire.Hosting.SqlServer
dotnet sln Platform.slnx add Platform.AppHost

echo "== 7. README =="
grep -q "Лабораторна 02" README.md || { echo "" >> README.md; cat $PACK/README_Lab02_section.md >> README.md; }

echo "== 8. Збірка =="
dotnet build Platform.slnx

echo
echo "ГОТОВО. Далі: dotnet run --project Services/Deals/Deals.Api  (і відкрити /swagger)"
echo "Потім:     rm -rf lab02-pack  &&  git add . && git commit && git push -u origin lab-02"
