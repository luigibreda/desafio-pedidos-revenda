using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using BeverageDistributor.API.Middlewares;
using BeverageDistributor.Application.Mappings;
using BeverageDistributor.Application.Validators;
using BeverageDistributor.Application;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Application.Services;
using BeverageDistributor.Domain.Interfaces;
using BeverageDistributor.Infrastructure;
using BeverageDistributor.Infrastructure.Persistence;
using BeverageDistributor.Infrastructure.Repositories;
using BeverageDistributor.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Prometheus;
using RabbitMQ.Client;

// Configuração do Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "BeverageDistributor.API")
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Properties:j}{NewLine}{Exception}{NewLine}",
        theme: AnsiConsoleTheme.Code)
    .WriteTo.Seq(Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341")
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Configurar o Serilog como provedor de logging
    builder.Host.UseSerilog();

    // Add services to the container.
    var configuration = builder.Configuration;

// Configuração básica do OpenTelemetry
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "BeverageDistributor.API",
                serviceVersion: "1.0.0",
                serviceInstanceId: Environment.MachineName);

// Cria um ActivitySource para rastreamento personalizado
var activitySource = new ActivitySource("BeverageDistributor.API");

// Configuração simplificada do OpenTelemetry - usando apenas APIs estáveis
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("BeverageDistributor.API")
    .AddSource("BeverageDistributor.Application")
    .AddSource("BeverageDistributor.Infrastructure")
    .SetResourceBuilder(resourceBuilder)
    .AddAspNetCoreInstrumentation()
    .AddConsoleExporter()
    .Build();

// Configuração simplificada de métricas
var meter = new Meter("BeverageDistributor.API");
var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter("BeverageDistributor.API")
    .AddMeter("BeverageDistributor.Application")
    .AddMeter("BeverageDistributor.Infrastructure")
    .AddConsoleExporter()
    .Build();
var ordersProcessed = meter.CreateCounter<long>("orders_processed", "Number of orders processed");
var orderProcessingTime = meter.CreateHistogram<double>("order_processing_time_seconds", "Time to process an order");
var activeOrders = meter.CreateUpDownCounter<int>("active_orders", "Number of active orders");

// Register the meter and metrics as singletons
builder.Services.AddSingleton(meter);
builder.Services.AddSingleton(ordersProcessed);
builder.Services.AddSingleton(orderProcessingTime);
builder.Services.AddSingleton(activeOrders);

// Register the metrics service
builder.Services.AddScoped<IMetricsService, MetricsService>();

// Add Infrastructure Layer
builder.Services.AddInfrastructure(configuration);

// Add Application Layer
builder.Services.AddApplication();

// Add AutoMapper with explicit profile registration
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
    cfg.AddProfile<OrderProfile>();
}, AppDomain.CurrentDomain.GetAssemblies());

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

// Register validators from the Application layer
builder.Services.AddValidatorsFromAssemblyContaining<CreateDistributorDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();

// Register Order services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Register Integration services
builder.Services.Configure<ExternalApiSettings>(configuration.GetSection("ExternalApi"));
builder.Services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
builder.Services.Configure<OrderProcessingSettings>(configuration.GetSection("OrderProcessing"));

// Configure external order service based on configuration
var useMockService = configuration.GetValue<bool>("ExternalApi:UseMockService", true);

if (useMockService)
{
    // Register mock service
    builder.Services.AddSingleton<IExternalOrderService, MockExternalOrderService>();
    
    // Add a no-op HTTP client to satisfy any dependencies
    builder.Services.AddHttpClient("NoOpHttpClient");
    
    builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddConsole();
    });
}
else
{
    // Configure HTTP Client with basic retry policy for the real service
    builder.Services.AddHttpClient<IExternalOrderService, ExternalOrderService>((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<ExternalApiSettings>>().Value;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }
    });
}

// Register RabbitMQ producer
builder.Services.AddSingleton<IMessageProducer, RabbitMqProducer>();

// Register Order Orchestrator
builder.Services.AddScoped<IOrderOrchestratorService, OrderOrchestratorService>();

// Register Order Processing Service as a hosted service
builder.Services.AddHostedService<OrderProcessingService>();

// Register Cache Service
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database");

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Beverage Distributor API",
        Version = "v1",
        Description = "API para gerenciamento de pedidos de revenda de bebidas",
        Contact = new OpenApiContact
        {
            Name = "Suporte",
            Email = "suporte@beveragedistributor.com"
        }
    });
    
    // Enable annotations for better documentation
    c.EnableAnnotations();
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Beverage Distributor API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the root
    });
}

// Add health check endpoint
app.MapHealthChecks("/health");

// Simple metrics endpoint
app.MapGet("/metrics", async context =>
{
    await context.Response.WriteAsync("Metrics are currently only available through the console exporter in this version.");
});

// Adicionar o middleware de logging de requisições
app.UseRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.IsNpgsql())
        {
            await context.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}



app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    // Garante que todos os logs sejam enviados antes de encerrar
    Log.CloseAndFlush();
}
