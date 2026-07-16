using JeoAnoBa.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ----- Controllers & Serialization -----
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Our models reference each other in a loop (Game -> Categories -> Category.Game -> ...)
        // Without this, converting an object to JSON would loop forever and crash. This tells it
        // to just stop once it hits something it's already included.
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Makes the JSON responses readable (line breaks + indentation) instead of one long line.
        // Just for easier debugging, not required for anything to work.
        options.JsonSerializerOptions.WriteIndented = true;

        // By default ASP.NET Core turns "Id" into "id" (camelCase) in the JSON response.
        // The MAUI app expects exact matches like "Id", so if we let it do that,
        // MAUI would fail to read the data back correctly. This keeps property
        // names exactly as we wrote them in our C# classes (e.g. Id stays Id).
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Enables Swagger — an auto-generated webpage for viewing and testing our API endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----- Database Connection -----
// Grabs the database connection string (path to jeopardy.db) from appsettings.json,
// and tells EF Core to use SQLite with it whenever JeopardyDbContext is used
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<JeopardyDbContext>(options =>
    options.UseSqlite(connectionString));

// ----- CORS Policy -----
// CORS controls which outside apps/websites are allowed to send requests to this API.
// This policy allows requests from anywhere, using any method, with any headers
// simplest option so the MAUI app can talk to the API without being blocked.
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


// Runs once every time the API starts up: checks if the database is empty,
// and if so, imports the full Jeopardy question set into it.
// Safe to run on every startup — it skips itself if data already exists.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JeopardyDbContext>();
    await DataSeeder.SeedMasterLibraryAsync(db, app.Environment);
}

// ----- Middleware Pipeline -----
// Only turn on the Swagger test page while developing, not in a real deployed version
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Turn on the CORS policy defined above
app.UseCors("AllowAll");

// Checks whether a request is allowed to access what it's asking for
app.UseAuthorization();
// Connects incoming requests to the right controller/endpoint method
app.MapControllers();
// Starts the server listening for requests
app.Run();