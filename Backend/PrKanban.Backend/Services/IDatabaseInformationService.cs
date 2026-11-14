namespace PrKanban.Services;

/// <summary>Provides non-sensitive information about the configured database.</summary>
public interface IDatabaseInformationService
{
    DatabaseInformationResponse GetInformation();
}

/// <summary>Database provider and location, without connection credentials.</summary>
public sealed record DatabaseInformationResponse(string Provider, string Location);
