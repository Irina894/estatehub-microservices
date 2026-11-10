// AppHost: лише оркестрація (без бізнес-логіки). Запускає контейнери БД і Redis, три мікросервіси,
// Aggregator і Gateway однією командою; рядки підключення й адреси сервісів передає Aspire.
var builder = DistributedApplication.CreateBuilder(args);

// ---- Інфраструктура: SQL Server, MongoDB і Redis у контейнерах з постійним сховищем ----
// Пароль SQL Server - секрет AppHost:
//   dotnet user-secrets set "Parameters:sql-password" "<ваш складний пароль>" --project Platform.AppHost
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
                 .WithDataVolume();
var dealsDb = sql.AddDatabase("DealsDb", "EstateHub_Transactions");   // Лаб. 2 (ADO.NET + Dapper)
var catalogDb = sql.AddDatabase("CatalogDb", "EstateHub_Catalog");    // Лаб. 3 (EF Core)

var mongo = builder.AddMongoDB("mongo")
                   .WithDataVolume();
var reviewsDb = mongo.AddDatabase("ReviewsDb");                       // Лаб. 4 (MongoDB)

// Лаб. 6: Redis - розподілений кеш (L2) для всіх сервісів. WithDataVolume зберігає кеш між перезапусками.
// Ім'я ресурсу "redis" = ім'я в builder.AddRedisDistributedCache("redis") у сервісах;
// рядок підключення Aspire передає як ConnectionStrings:redis.
var redis = builder.AddRedis("redis")
                   .WithDataVolume();

// ---- Мікросервіси: ім'я ресурсу = логічне ім'я для service discovery ----
var deals = builder.AddProject<Projects.Deals_Api>("deals-api")
    .WithReference(dealsDb).WaitFor(dealsDb)
    .WithReference(redis).WaitFor(redis);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithReference(redis).WaitFor(redis);

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb).WaitFor(reviewsDb)
    .WithReference(redis).WaitFor(redis);

// ---- Aggregator: gRPC-клієнт сервісів (https://<ім'я>) + кеш агрегатів у Redis ----
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(deals).WithReference(catalog).WithReference(reviews)
    .WaitFor(deals).WaitFor(catalog).WaitFor(reviews)
    .WithReference(redis).WaitFor(redis);

// ---- Gateway: єдина точка входу на фіксованому порту 5000 (REST назовні, gRPC лише всередині платформи) ----
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(deals).WithReference(catalog).WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();
