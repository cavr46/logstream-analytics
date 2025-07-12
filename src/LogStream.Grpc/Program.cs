using LogStream.Application;
using LogStream.Infrastructure.Data;
using LogStream.Infrastructure.Repositories;
using LogStream.Infrastructure.Caching;
using LogStream.Infrastructure.Search;
using LogStream.Grpc.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Nest;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 1024 * 1024 * 10; // 10MB
    options.MaxSendMessageSize = 1024 * 1024 * 10; // 10MB
});

// Add Application layer
builder.Services.AddApplication();

// Add Infrastructure services
builder.Services.AddDbContext<LogStreamDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connectionString!);
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Add Elasticsearch
builder.Services.AddSingleton<IElasticClient>(provider =>
{
    var settings = new ConnectionSettings(new Uri(builder.Configuration.GetConnectionString("Elasticsearch")!))
        .DefaultIndex("logstream-logs");

    return new ElasticClient(settings);
});

// Register repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ISearchService, ElasticsearchService>();

// Add Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });

builder.Services.AddAuthorization();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContext<LogStreamDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddElasticsearch(builder.Configuration.GetConnectionString("Elasticsearch")!);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapGrpcService<LogIngestionService>();

app.MapHealthChecks("/health");

app.MapGet("/", () => "LogStream gRPC Service is running. Use a gRPC client to communicate with this service.");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogStreamDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();