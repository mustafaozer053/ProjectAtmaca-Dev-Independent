using ClosedXML.Excel;

namespace ProjectAtmaca.Web.Services;

public sealed record TrainingReportRow(
    DateOnly Date,
    string Title,
    int Eligible,
    int Present,
    int Absent,
    int NotRecorded,
    int Bta);

public sealed record AthleteReportRow(
    string Name,
    string? CardNumber,
    int Total,
    int Present,
    int Absent,
    int NotRecorded,
    int Bta);

public sealed record FixtureReportRow(
    DateOnly Date,
    string Type,
    string Opponent,
    int OurScore,
    int OpponentScore,
    string Outcome);

public static class TeamReportExcelExporter
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Build(
        string seasonName,
        string teamName,
        IReadOnlyList<TrainingReportRow> trainings,
        IReadOnlyList<AthleteReportRow> athletes,
        IReadOnlyList<FixtureReportRow> fixtures)
    {
        using var workbook = new XLWorkbook();

        var trainingSheet = workbook.Worksheets.Add("Antrenman devamlılığı");
        WriteHeader(trainingSheet, seasonName, teamName);
        WriteTable(
            trainingSheet,
            ["Tarih", "Antrenman", "Uygun sporcu", "Katıldı", "Katılmadı", "Durum bekliyor", "BTA", "Katılım oranı"],
            trainings.Select(row => new object?[]
            {
                row.Date.ToDateTime(TimeOnly.MinValue), row.Title, row.Eligible,
                row.Present, row.Absent, row.NotRecorded, row.Bta,
                Rate(row.Present, row.Absent)
            }));

        var athleteSheet = workbook.Worksheets.Add("Sporcu katılımı");
        WriteHeader(athleteSheet, seasonName, teamName);
        WriteTable(
            athleteSheet,
            ["Sporcu", "Kart no", "Kapsamdaki antrenman", "Katıldı", "Katılmadı", "Durum bekliyor", "BTA", "Katılım oranı"],
            athletes.Select(row => new object?[]
            {
                row.Name, row.CardNumber, row.Total, row.Present, row.Absent,
                row.NotRecorded, row.Bta, Rate(row.Present, row.Absent)
            }));
        athleteSheet.Cell(athleteSheet.LastRowUsed()!.RowNumber() + 2, 1).Value =
            "Katılım oranı = Katıldı / (Katıldı + Katılmadı). BTA ve durumu girilmemiş antrenmanlar orana dahil edilmez.";

        var fixtureSheet = workbook.Worksheets.Add("Müsabaka sonuçları");
        WriteHeader(fixtureSheet, seasonName, teamName);
        WriteTable(
            fixtureSheet,
            ["Tarih", "Tür", "Rakip", "Bizim skor", "Rakip skor", "Sonuç"],
            fixtures.Select(row => new object?[]
            {
                row.Date.ToDateTime(TimeOnly.MinValue), row.Type, row.Opponent,
                row.OurScore, row.OpponentScore, row.Outcome
            }));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string BuildFileName(string seasonName, string teamName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var raw = $"Takim-Raporu_{seasonName}_{teamName}";
        var cleaned = new string(raw
            .Select(character => invalid.Contains(character) || character == ' ' ? '-' : character)
            .ToArray());
        return $"{cleaned}_{DateTime.Now:yyyyMMdd}.xlsx";
    }

    private static object Rate(int present, int absent) =>
        present + absent == 0 ? "—" : Math.Round(100d * present / (present + absent), 1) / 100d;

    private static void WriteHeader(IXLWorksheet sheet, string seasonName, string teamName)
    {
        sheet.Cell(1, 1).Value = $"Sezon: {seasonName}";
        sheet.Cell(2, 1).Value = $"Takım: {teamName}";
        sheet.Cell(3, 1).Value = $"Rapor tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}";
        sheet.Range(1, 1, 2, 1).Style.Font.Bold = true;
    }

    private static void WriteTable(
        IXLWorksheet sheet,
        string[] headers,
        IEnumerable<object?[]> rows)
    {
        const int headerRow = 5;
        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(headerRow, column + 1).Value = headers[column];

        var headerRange = sheet.Range(headerRow, 1, headerRow, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDF0FF");

        var rowNumber = headerRow + 1;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Length; column++)
            {
                var cell = sheet.Cell(rowNumber, column + 1);
                switch (row[column])
                {
                    case DateTime date:
                        cell.Value = date;
                        cell.Style.DateFormat.Format = "dd.MM.yyyy";
                        break;
                    case int number:
                        cell.Value = number;
                        break;
                    case double rate:
                        cell.Value = rate;
                        cell.Style.NumberFormat.Format = "0.0%";
                        break;
                    case null:
                        break;
                    default:
                        cell.Value = row[column]!.ToString();
                        break;
                }
            }

            rowNumber++;
        }

        sheet.Columns().AdjustToContents();
    }
}
