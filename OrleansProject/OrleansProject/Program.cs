using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Orleans.Configuration;
using Orleans.Dashboard;
using OrleansProject;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Your worker (kept from your snippet)
builder.Services.AddHostedService<Worker>();
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Redis settings (env vars are nice for k8s/docker, with defaults for local dev)
var redisConnectionString =
    builder.Configuration.GetConnectionString("Redis") ??
    builder.Configuration["REDIS_CONNECTION_STRING"] ??
    "localhost:6379";

// Optional: Orleans basics
var siloPort = builder.Configuration.GetValue("ORLEANS_SILO_PORT", 11111);
var gatewayPort = builder.Configuration.GetValue("ORLEANS_GATEWAY_PORT", 30000);
var clusterId = builder.Configuration["ORLEANS_CLUSTER_ID"] ?? "dev";
var serviceId = builder.Configuration["ORLEANS_SERVICE_ID"] ?? "OrleansProject";

// Orleans Silo + Redis clustering + Redis grain storage (state)
builder.UseOrleans(silo =>
{
    // Cluster identity
    silo.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = clusterId;
        options.ServiceId = serviceId;
    });

    // --- Redis for membership (cluster) ---
    // Requires Microsoft.Orleans.Clustering.Redis
    silo.UseRedisClustering(options =>
    {
        options.ConfigurationOptions = new ConfigurationOptions
        {
            EndPoints = { redisConnectionString }
        };
        // options.DatabaseNumber = 0; // set if you want a specific DB
    });

    // --- Redis for grain state ---
    // Requires Microsoft.Orleans.Persistence.Redis
    // Use this as the default storage provider (unless you register others)
    silo.AddRedisGrainStorageAsDefault(options =>
    {
        options.ConfigurationOptions = new ConfigurationOptions
        {
            EndPoints = { redisConnectionString }
        };        
        // options.DatabaseNumber = 1; // optionally separate from membership
    });

    // If you prefer named storage (e.g. [StorageProvider(ProviderName="redis")]):
    // silo.AddRedisGrainStorage("redis", options => { options.ConnectionString = redisConnectionString; });

    // Optional but helpful
    silo.ConfigureLogging(logging => logging.AddConsole());
    silo.AddDashboard();
    silo.Configure<GrainCollectionOptions>(o =>
    {
        // o.CollectionAge = TimeSpan.FromMinutes(15);
    });
});
var host = builder.Build();
host.MapOrleansDashboard("/dashboard");
await host.RunAsync();
