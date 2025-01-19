using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Destructurama.Attributed;

namespace SerilogMasking;

public static class WeatherService
{
    public static async Task<WeatherForecastsResponse>GetWeatherForecastResponseAsync(HttpClient httpClient, WeatherRequest weatherRequest)
    {
        HttpRequestMessage httpRequestMessage = new()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"http://localhost:5102/internal/weatherforecast"),
            Content = new StringContent(JsonSerializer.Serialize(weatherRequest), Encoding.UTF8, "application/json")
        };
        
        HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        string responseString = await httpResponseMessage.Content.ReadAsStringAsync();

        if (httpResponseMessage.IsSuccessStatusCode is false)
        {
            throw new Exception($"Weather forecast failed with status code {(int)httpResponseMessage.StatusCode}, response: {responseString}");
        }
        
        var weatherForecasts = JsonSerializer.Deserialize<List<WeatherForecast>>(responseString)!;

        return new WeatherForecastsResponse(weatherForecasts);
    }    
}

public record WeatherRequest(int Days, string UserName, string Password)
{
    [LogMasked(Text = "REDACTED")] 
    public string UserName { get; set; } = UserName;

    [LogMasked(Text = "REDACTED")] 
    public string Password { get; set; } = Password;
};

public record WeatherForecast(DateOnly Date, int TemperatureC, string Summary, string Location)
{
    [JsonPropertyName("date")] 
    public DateOnly Date { get; set; } = Date;
    
    [JsonPropertyName("temperatureC")]
    public int TemperatureC { get; set; } = TemperatureC;
    
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = Summary;
    
    [LogMasked(Text = "REDACTED")] 
    [JsonPropertyName("location")]
    public string Location { get; set; } = Location;
    
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record WeatherForecastsResponse(List<WeatherForecast> WeatherForecasts)
{
    public List<WeatherForecast> WeatherForecasts { get; set; } = WeatherForecasts;
};