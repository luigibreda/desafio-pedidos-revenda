using System.Net.Http.Headers;
using System.Text;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var configuration = builder.Configuration;

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Beverage Distributor API",
        Version = "v1",
        Description = "API para gerenciamento de pedidos de revenda de bebidas",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Suporte",
            Email = "suporte@beveragedistributor.com"
        }
    });
    
    // Enable annotations for better documentation
    c.EnableAnnotations();
    
    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database");

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

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Beverage Distributor API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Beverage Distributor API V1");
        c.RoutePrefix = string.Empty;
    });
}

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
