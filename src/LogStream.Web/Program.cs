using LogStream.Application;
using LogStream.Infrastructure.Data;
using LogStream.Infrastructure.Repositories;
using LogStream.Infrastructure.Caching;
using LogStream.Infrastructure.Search;
using LogStream.Web.Services;
using LogStream.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
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

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add SignalR
builder.Services.AddSignalR();

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

// Add web-specific services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILogStreamApiService, LogStreamApiService>();
builder.Services.AddScoped<IRealTimeService, RealTimeService>();

// Add Authentication
builder.Services.AddAuthentication("OpenIdConnect")
    .AddOpenIdConnect("OpenIdConnect", options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.ClientId = builder.Configuration["Authentication:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });

builder.Services.AddAuthorization();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContext<LogStreamDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddElasticsearch(builder.Configuration.GetConnectionString("Elasticsearch")!);

// Add CORS for API calls
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Map SignalR hubs
app.MapHub<LogStreamHub>("/hubs/logstream");

// Map health checks
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogStreamDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();