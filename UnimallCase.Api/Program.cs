using Microsoft.OpenApi.Models;
using UnimallCase.Api.Middleware;
using UnimallCase.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Unimall Case API", Version = "v1" });
    
    // Add API Key authentication
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication using the 'X-API-Key' header",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },
        In = ParameterLocation.Header
    };

    var requirement = new OpenApiSecurityRequirement
    {
        { scheme, new List<string>() }
    };

    c.AddSecurityRequirement(requirement);
});

// Add Memory Caching
builder.Services.AddMemoryCache();

// Configure HTTP client
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<IProductCrawlerService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var logger = sp.GetRequiredService<ILogger<ProductCrawlerService>>();
    return new ProductCrawlerService(httpClient, logger);
});
builder.Services.AddSingleton<IProductImageCrawlerService, ProductImageCrawlerService>();
builder.Services.AddSingleton<IProductTransformService>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<ProductTransformService>>();
    var imageCrawler = sp.GetRequiredService<IProductImageCrawlerService>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new ProductTransformService(config["OpenAI:ApiKey"], logger, imageCrawler);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors();

// Add custom middleware
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
