using Destructurama;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using SerilogMasking;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddUserSecrets<Program>(optional: true, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var apiKey = builder.Configuration["Otlp:ApiKey"]!;
var seqBaseUrl = builder.Configuration["Seq:BaseUrl"]!;

builder.Host.UseSerilog((hostContext, services, loggerConfig) =>
{
    loggerConfig
        .MinimumLevel.Debug()
        .Enrich.FromLogContext()
        .Destructure.UsingAttributes()
        .WriteTo.Console()
        .WriteTo.Seq(seqBaseUrl,
            restrictedToMinimumLevel: LogEventLevel.Information,
            apiKey: apiKey
        );
});

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "SerilogMasking", serviceVersion: "1.0.0");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        var tracesUrl = builder.Configuration["Seq:TracesUrl"]!;
        tracerProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(seqBaseUrl+tracesUrl);
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

app.MapGet("/weatherforecast", async (ILogger<Program> logger) =>
{
    HttpClient httpClient = new();
    WeatherRequest weatherRequest = new(Days: 5, UserName: "testUser", Password: "testPassword");
    
    logger.LogInformation("Forecast Request: {@Request}", weatherRequest);

    WeatherForecastsResponse response = await WeatherService.GetWeatherForecastResponseAsync(httpClient, weatherRequest);
    
    logger.LogInformation("Forecast Response {@Response}", response);
    return response.WeatherForecasts;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/internal/weatherforecast", (WeatherRequest weatherRequest) =>
{
    string[] summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];
    
    List<WeatherForecast> forecasts = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                Date: DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC: Random.Shared.Next(-20, 55),
                Summary: summaries[Random.Shared.Next(summaries.Length)],
                Location: "Top Secret Facility"
            ))
        .ToList();

    return Results.Ok(forecasts);
})
.ExcludeFromDescription()
.WithName("GetInternalWeatherForecast")
.WithOpenApi();

app.Run();


