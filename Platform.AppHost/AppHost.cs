// AppHost: лише оркестрація (без бізнес-логіки). Запускає контейнери БД, три мікросервіси,
// Aggregator і Gateway однією командою; рядки підключення й адреси сервісів передає Aspire.
var builder = DistributedApplication.CreateBuilder(args);

// ---- Інфраструктура: SQL Server і MongoDB у контейнерах з постійним сховищем ----
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

// ---- Мікросервіси: ім'я ресурсу = логічне ім'я для service discovery ----
var deals = builder.AddProject<Projects.Deals_Api>("deals-api")
    .WithReference(dealsDb).WaitFor(dealsDb);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb);

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb).WaitFor(reviewsDb);

// ---- Aggregator: знає про сервіси, які опитує ----
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(deals).WithReference(catalog).WithReference(reviews)
    .WaitFor(deals).WaitFor(catalog).WaitFor(reviews);

// ---- Gateway: єдина точка входу на фіксованому порту 5000 ----
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(deals).WithReference(catalog).WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();
