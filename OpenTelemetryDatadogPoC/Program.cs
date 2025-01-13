using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetryDatadogPoC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Define the resource builder with service information
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "OpenTelemetryDatadogPoC", serviceVersion: "1.0.0");

var apiKey = builder.Configuration["Otlp:ApiKey"]!;

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        var url = builder.Configuration["Seq:TracesUrl"]!;
        tracerProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddProcessor(new SensitiveDataProcessor()) // Custom processor for scrubbing
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(url);
                options.Headers = $"X-SEC-ApiKey={apiKey}";
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    })
    // .WithMetrics(meterProviderBuilder =>
    // {
    //     var url = builder.Configuration["Seq:Url"]!;
    //     meterProviderBuilder
    //         .SetResourceBuilder(resourceBuilder)
    //         .AddAspNetCoreInstrumentation()
    //         .AddHttpClientInstrumentation()
    //         .AddOtlpExporter(options =>
    //         {
    //             options.Endpoint = new Uri(url);
    //             options.Headers = $"X-SEC-ApiKey={apiKey}";
    //             options.Protocol = OtlpExportProtocol.HttpProtobuf;
    //         });
    // })
    .WithLogging(loggingProviderBuilder =>
    {
        var url = builder.Configuration["Seq:LogsUrl"]!;
        loggingProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            //.AddProcessor(new SensitiveDataProcessor())
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(url);
                options.Headers = $"X-SEC-ApiKey={apiKey}";
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    logger.LogInformation("Forecast Request: Getting 5 random weather forecasts");
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    
    logger.LogInformation("Forecast Response {0}", JsonSerializer.Serialize(forecast));
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
