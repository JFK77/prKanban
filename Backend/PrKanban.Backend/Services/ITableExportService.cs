namespace PrKanban.Services;

public interface ITableExportService
{
    byte[] CreateWorkbook(string[] headers, string[][] rows, CancellationToken cancellationToken);
}
