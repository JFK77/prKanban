namespace PrKanban.Services;

public interface ITableExportService
{
    byte[] CreateWorkbook(string[] headers, string[][] rows, CancellationToken cancellationToken);

    /// <summary>Creates a billing report workbook with metadata and summary blocks above the entry table.</summary>
    byte[] CreateBillingWorkbook(string title, string[][] metadata, string[][] summary, string[] headers, string[][] rows, CancellationToken cancellationToken);
}
