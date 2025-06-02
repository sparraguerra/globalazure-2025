var builder = WebApplication.CreateBuilder(args);

var serviceName = Assembly.GetExecutingAssembly().GetName().Name!.ToLowerInvariant();

// Configure OpenTelemetry Tracing
builder.Logging
    .AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    })
    .AddConsole();

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
var otel = builder.Services.AddOpenTelemetry()
    .ConfigureResource(c => c.AddService(serviceName))
    .WithTracing(tracing => 
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddSource(serviceName)
            .AddConsoleExporter();
    })
    .WithMetrics(metrics => 
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });
 
// Export OpenTelemetry data via OTLP, using env vars for the configuration
if (otlpEndpoint is not null)
{
    otel.UseOtlpExporter();
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("weatherforecast");
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    logger.LogInformation("Request to /weatherforecast processed");
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

await app.RunAsync();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
