// Program.cs
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Register a pooled data source (built-in pooling)
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Default")!);

var app = builder.Build();

app.MapGet("/dbtime", async (NpgsqlDataSource dataSource) =>
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand("select now()", conn);
    var now = (DateTime)await cmd.ExecuteScalarAsync();
    return Results.Ok(now);
});

app.Run();