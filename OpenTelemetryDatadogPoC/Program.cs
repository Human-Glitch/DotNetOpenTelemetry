using System.Text.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "OpenTelemetryDatadogPoC", serviceVersion: "1.0.0");

var apiKey = builder.Configuration["Otlp:ApiKey"]!;

builder.Logging.AddOpenTelemetry(openTelemetryBuilder =>
{
    var url = builder.Configuration["Seq:LogsUrl"]!;
    openTelemetryBuilder.IncludeScopes = true;
    openTelemetryBuilder.AddOtlpExporter(otlpExporterOptions =>
    {
        otlpExporterOptions.Endpoint = new Uri(url);
        otlpExporterOptions.Headers = $"X-SEC-ApiKey={apiKey}";
        otlpExporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        var url = builder.Configuration["Seq:TracesUrl"]!;
        tracerProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(url);
                options.Headers = $"X-SEC-ApiKey={apiKey}";
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
    });
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

var app = builder.Build();

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
            summaries[Random.Shared.Next(summaries.Length)],
            "234"
        ))
        .ToArray();
    
    logger.LogInformation("Forecast Response {response}", JsonSerializer.Serialize(forecast));
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

public class WeatherForecast
{
    public WeatherForecast(DateOnly date, int temperatureC, string? summary, string apiKey)
    {
        Date = date;
        TemperatureC = temperatureC;
        Summary = summary;
        ApiKey = apiKey;
    }
    
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    
    public string? Summary { get; set; }
    
    public string ApiKey { get; set; }
    
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


