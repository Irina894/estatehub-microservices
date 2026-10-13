var builder = DistributedApplication.CreateBuilder(args);

// SQL Server у контейнері (потрібен Docker Desktop) + база угод
var sql = builder.AddSqlServer("sql")
                 .WithDataVolume();
var dealsDb = sql.AddDatabase("DealsDb", "EstateHub_Transactions");

// API сервісу заявок та угод; рядок підключення потрапляє як ConnectionStrings:DealsDb
builder.AddProject<Projects.Deals_Api>("deals-api")
       .WithReference(dealsDb)
       .WaitFor(dealsDb);

builder.Build().Run();
