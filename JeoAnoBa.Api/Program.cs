using JeoAnoBa.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ----- Controllers & Serialization -----
// Configured once with cycle ignore settings to avoid JSON exceptions in relational entities
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;

        // Keep PascalCase property names in responses. The MAUI client reads
        // responses with System.Text.Json's default (case-sensitive) options,
        // so without this, "id"/"name" (camelCase) would fail to bind to
        // Id/Name on the client.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----- Database Connection -----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<JeopardyDbContext>(options =>
    options.UseSqlite(connectionString));

// ----- CORS Policy -----
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JeopardyDbContext>();
    await DataSeeder.SeedMasterLibraryAsync(db, app.Environment);
}

// ----- Middleware Pipeline -----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();