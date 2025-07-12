using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using LogStream.Application;
using LogStream.Infrastructure.Data;
using LogStream.Infrastructure.Repositories;
using LogStream.Infrastructure.Caching;
using LogStream.Infrastructure.Search;
using StackExchange.Redis;
using Nest;
using Serilog;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
            .CreateLogger();

        services.AddSingleton<ILogger>(Log.Logger);

        // Add Application layer
        services.AddApplication();

        // Add Infrastructure services
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<LogStreamDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Add Redis
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisConnectionString = context.Configuration.GetConnectionString("Redis");
            return ConnectionMultiplexer.Connect(redisConnectionString!);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = context.Configuration.GetConnectionString("Redis");
        });

        // Add Elasticsearch
        services.AddSingleton<IElasticClient>(provider =>
        {
            var elasticConnectionString = context.Configuration.GetConnectionString("Elasticsearch");
            var settings = new ConnectionSettings(new Uri(elasticConnectionString!))
                .DefaultIndex("logstream-logs");

            return new ElasticClient(settings);
        });

        // Register repositories and services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ISearchService, ElasticsearchService>();

        // Add custom services
        services.AddScoped<ILogProcessingService, LogProcessingService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IArchivalService, ArchivalService>();
    })
    .Build();

host.Run();