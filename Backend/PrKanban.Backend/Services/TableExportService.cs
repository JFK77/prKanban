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

    public byte[] CreateBillingWorkbook(string title, string[][] metadata, string[][] summary, string[] headers, string[][] rows, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");
        var line = 1;

        sheet.Cell(line, 1).Value = title;
        sheet.Cell(line, 1).Style.Font.Bold = true;
        sheet.Cell(line, 1).Style.Font.FontSize = 14;
        line += 2;

        foreach (var pair in metadata)
        {
            sheet.Cell(line, 1).Value = pair.ElementAtOrDefault(0) ?? "";
            sheet.Cell(line, 1).Style.Font.Bold = true;
            sheet.Cell(line, 2).Value = pair.ElementAtOrDefault(1) ?? "";
            line++;
        }
        line++;

        foreach (var pair in summary)
        {
            sheet.Cell(line, 1).Value = pair.ElementAtOrDefault(0) ?? "";
            sheet.Cell(line, 1).Style.Font.Bold = true;
            sheet.Cell(line, 2).Value = pair.ElementAtOrDefault(1) ?? "";
            line++;
        }
        line++;

        var headerRow = line;
        var widths = headers.Select(header => header.Length).ToArray();
        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(headerRow, column + 1).Value = headers[column];
        sheet.Row(headerRow).Style.Font.Bold = true;
        sheet.Row(headerRow).Style.Fill.BackgroundColor = XLColor.FromArgb(226, 232, 240);
        for (var row = 0; row < rows.Length; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var column = 0; column < headers.Length; column++)
            {
                var value = rows[row].ElementAtOrDefault(column) ?? "";
                sheet.Cell(headerRow + 1 + row, column + 1).Value = value;
                widths[column] = Math.Max(widths[column], value.Length);
            }
        }
        for (var column = 0; column < headers.Length; column++)
            sheet.Column(column + 1).Width = Math.Clamp(widths[column] + 2, 12, 60);
        sheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
