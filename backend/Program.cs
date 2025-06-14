using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<Kernel>(Kernel.CreateBuilder()
                                            .Build());

builder.Services.AddHttpClient();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors();
// Add CORS policy to allow all
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

// API Endpoints
app.MapGet("/health", () => Results.Ok("System is healthy"));

app.MapGet("/version", () =>
{
    var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown";
    return Results.Ok(version);
});

app.MapGet("/kernel", (Kernel kernel) => Results.Ok(kernel.GetType().Name));

app.MapGet("/ndvi", async (HttpClient client) =>
{
    // Set the search endpoint
    string searchUrl = "https://planetarycomputer.microsoft.com/api/stac/v1/search";

    // Define the search query (JSON payload)
    var searchQuery = new
    {
        collections = new[] { "sentinel-2-l2a" },
        datetime = "2023-01-01T00:00:00Z/2023-12-31T23:59:59Z",
        bbox = new double[] { -122.5, 37.5, -122.0, 38.0 } // Example: San Francisco area
    };

    try
    {
        // Serialize the query to JSON
        string jsonQuery = JsonSerializer.Serialize(searchQuery);
        var content = new StringContent(jsonQuery, System.Text.Encoding.UTF8, "application/json");

        // Make POST request to the search endpoint
        HttpResponseMessage response = await client.PostAsync(searchUrl, content);
        response.EnsureSuccessStatusCode();

        // Read and deserialize the JSON response
        string jsonResponse = await response.Content.ReadAsStringAsync();

        var searchResult = JsonSerializer.Deserialize<dynamic>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});

app.Run();


