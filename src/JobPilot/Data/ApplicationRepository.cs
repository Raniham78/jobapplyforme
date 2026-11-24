using JobPilot.Models;
using Microsoft.Data.Sqlite;

namespace JobPilot.Data;

public sealed class ApplicationRepository
{
    private readonly string _connectionString;

    public ApplicationRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullDatabasePath = Path.GetFullPath(databasePath);
        var databaseDirectory = Path.GetDirectoryName(fullDatabasePath);

        if (!string.IsNullOrWhiteSpace(databaseDirectory))
            Directory.CreateDirectory(databaseDirectory);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = fullDatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        _connectionString = builder.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS applications (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                company      TEXT NOT NULL,
                role         TEXT NOT NULL,
                url          TEXT NOT NULL,
                status       TEXT NOT NULL,
                created_utc  TEXT NOT NULL,
                updated_utc  TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_applications_created_utc
                ON applications(created_utc DESC);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> AddDraftAsync(string company, string role, string url,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var now = DateTime.UtcNow.ToString("O");
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO applications(company, role, url, status, created_utc, updated_utc)
            VALUES ($company, $role, $url, 'Draft', $created, $updated);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$company", company);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$url", url);
        command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    public async Task MarkSubmittedAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE applications
            SET status = 'Submitted', updated_utc = $updated
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobApplication>> GetRecentAsync(int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var items = new List<JobApplication>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, company, role, url, status, created_utc
            FROM applications
            ORDER BY id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 100));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var created = DateTime.Parse(reader.GetString(5), null,
                System.Globalization.DateTimeStyles.RoundtripKind);
            items.Add(new JobApplication(
                reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), created));
        }

        return items;
    }
}
