// Program.cs
using Npgsql;
using hcm_testing_api;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register a pooled data source (built-in pooling)
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Default")!);

var app = builder.Build();

// Ensure the 'documents' table exists
//EnsureDatabaseAsync(app.Services).GetAwaiter().GetResult();

// CRUD endpoints for Document
app.MapGet("/documents", async (NpgsqlDataSource dataSource) =>
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand(
        "select id, title, owner, createdat, updatedat from document order by id", conn);
    await using var reader = await cmd.ExecuteReaderAsync();

    var list = new List<Document>();
    while (await reader.ReadAsync())
    {
        list.Add(ReadDocument(reader));
    }
    return Results.Ok(list);
});

app.MapGet("/documents/{id:int}", async (int id, NpgsqlDataSource dataSource) =>
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand(
        "select id, title, owner, createdat, updatedat from document where id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);

    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        return Results.Ok(ReadDocument(reader));
    }
    return Results.NotFound();
});

app.MapPost("/documents", async (CreateDocumentRequest body, NpgsqlDataSource dataSource) =>
{
    if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Owner))
        return Results.BadRequest("Title and Owner are required.");

    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand(@"
        insert into document (title, owner)
        values (@title, @owner)
        returning id, title, owner, createdat, updatedat;", conn);
    cmd.Parameters.AddWithValue("title", body.Title);
    cmd.Parameters.AddWithValue("owner", body.Owner);

    await using var reader = await cmd.ExecuteReaderAsync();
    await reader.ReadAsync();
    var doc = ReadDocument(reader);
    return Results.Created($"/documents/{doc.Id}", doc);
});

app.MapPut("/documents/{id:int}", async (int id, UpdateDocumentRequest body, NpgsqlDataSource dataSource) =>
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand(@"
        update document
        set title = @title,
            owner = @owner,
            updatedat = now()
        where id = @id
        returning id, title, owner, createdat, updatedat;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("title", body.Title);
    cmd.Parameters.AddWithValue("owner", body.Owner);

    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        return Results.Ok(ReadDocument(reader));
    }
    return Results.NotFound();
});

app.MapDelete("/documents/{id:int}", async (int id, NpgsqlDataSource dataSource) =>
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand("delete from document where id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);

    var affected = await cmd.ExecuteNonQueryAsync();
    return affected > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

// Helpers and request DTOs
static async Task EnsureDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
    await using var conn = await dataSource.OpenConnectionAsync();

    var sql = @"
        create table if not exists document (
            id serial primary key,
            title varchar(255) not null,
            owner varchar(255) not null,
            createdat timestamp not null default now(),
            updatedat timestamp not null default now()
        );";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

static hcm_testing_api.Document ReadDocument(NpgsqlDataReader reader) =>
    new hcm_testing_api.Document
    {
        Id = reader.GetInt32(0),
        Title = reader.GetString(1),
        Owner = reader.GetString(2),
        CreatedAt = reader.GetFieldValue<DateTime>(3),
        UpdatedAt = reader.GetFieldValue<DateTime>(4)
    };

record CreateDocumentRequest(string Title, string Owner);
record UpdateDocumentRequest(string Title, string Owner);