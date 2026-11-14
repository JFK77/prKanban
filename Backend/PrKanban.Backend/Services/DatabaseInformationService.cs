using Microsoft.Data.Sqlite;

namespace PrKanban.Services;

public sealed class DatabaseInformationService(IConfiguration configuration) : IDatabaseInformationService
{
    public DatabaseInformationResponse GetInformation()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The default database connection is not configured.");
        var connection = new SqliteConnectionStringBuilder(connectionString);
        var source = connection.DataSource;
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException("The database location is not configured.");

        string location;
        if (source.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            var queryStart = source.IndexOfAny(['?', '#']);
            location = queryStart < 0 ? source : source[..queryStart];
        }
        else
        {
            location = source == ":memory:" ? source : Path.GetFullPath(source);
        }
        return new DatabaseInformationResponse("SQLite", location);
    }
}
