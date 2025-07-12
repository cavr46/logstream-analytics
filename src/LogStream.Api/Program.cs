using LogStream.Application;
using LogStream.Infrastructure.Data;
using LogStream.Infrastructure.Repositories;
using LogStream.Infrastructure.Caching;
using LogStream.Infrastructure.Search;
using LogStream.Api.Extensions;
using LogStream.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Nest;
using Serilog;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add FastEndpoints
builder.Services.AddFastEndpoints();

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
        .DefaultIndex("logstream-logs")
        .DefaultMappingFor<LogStream.Infrastructure.Search.ElasticsearchLogDocument>(m => m
            .IndexName("logstream-logs")
            .IdProperty(p => p.Id));

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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add Prometheus metrics
builder.Services.AddSingleton(Metrics.DefaultRegistry);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add custom middleware
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Add Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

app.UseFastEndpoints();

// Map health checks
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogStreamDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();