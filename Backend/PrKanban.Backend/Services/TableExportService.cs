using ClosedXML.Excel;

namespace PrKanban.Services;

public sealed class TableExportService : ITableExportService
{
    public byte[] CreateWorkbook(string[] headers, string[][] rows, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Tasks");
        var widths = headers.Select(header => header.Length).ToArray();
        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).Value = headers[column];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        for (var row = 0; row < rows.Length; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var column = 0; column < headers.Length; column++)
            {
                sheet.Cell(row + 2, column + 1).Value = rows[row][column];
                widths[column] = Math.Max(widths[column], rows[row][column].Length);
            }
        }
        for (var column = 0; column < headers.Length; column++)
            sheet.Column(column + 1).Width = Math.Clamp(widths[column] + 2, 12, 50);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
