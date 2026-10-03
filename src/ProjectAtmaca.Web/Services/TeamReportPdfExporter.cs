using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProjectAtmaca.Web.Services;

public static class TeamReportPdfExporter
{
    public const string ContentType = "application/pdf";

    private const string HeaderColor = "#EDF0FF";
    private const string MutedColor = "#6B7890";

    public static byte[] Build(
        string seasonName,
        string teamName,
        IReadOnlyList<TrainingReportRow> trainings,
        IReadOnlyList<AthleteReportRow> athletes,
        IReadOnlyList<FixtureReportRow> fixtures,
        IReadOnlyList<MatchAthleteReportRow> matchAthletes)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text("Takım Raporu").FontSize(16).Bold();
                    column.Item().Text($"Sezon: {seasonName}   ·   Takım: {teamName}   ·   Rapor tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontColor(MutedColor);
                    column.Item().PaddingBottom(8);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(14);

                    if (trainings.Count > 0)
                    {
                        column.Item().Element(container => Section(
                            container,
                            "Antrenman devamlılığı",
                            ["Tarih", "Antrenman", "Uygun sporcu", "Katıldı", "Katılmadı", "Durum bekliyor", "BTA", "Katılım oranı"],
                            [3, 6, 2, 2, 2, 2, 1.5f, 2.5f],
                            trainings.Select(row => new[]
                            {
                                row.Date.ToString("dd.MM.yyyy"), row.Title, row.Eligible.ToString(),
                                row.Present.ToString(), row.Absent.ToString(), row.NotRecorded.ToString(),
                                row.Bta.ToString(), Rate(row.Present, row.Absent)
                            }),
                            "Katılım oranı yalnızca “Katıldı” ve normal “Katılmadı” kayıtlarından hesaplanır. BTA ayrı gösterilir ve orana dahil edilmez."));
                    }

                    if (athletes.Count > 0)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(container => Section(
                            container,
                            "Sporcu bazlı antrenman katılımı",
                            ["Sporcu", "Kart no", "Kapsamdaki antrenman", "Katıldı", "Katılmadı", "Durum bekliyor", "BTA", "Katılım oranı"],
                            [5, 3, 3, 2, 2, 2, 1.5f, 2.5f],
                            athletes.Select(row => new[]
                            {
                                row.Name, row.CardNumber ?? "", row.Total.ToString(), row.Present.ToString(),
                                row.Absent.ToString(), row.NotRecorded.ToString(), row.Bta.ToString(),
                                Rate(row.Present, row.Absent)
                            }),
                            "Katılım oranı = Katıldı / (Katıldı + Katılmadı). BTA kayıtları ve durumu girilmemiş antrenmanlar orana dahil edilmez; BTA sporcunun diğer takımdaki katılımı hakkında bilgi vermez."));
                    }

                    if (fixtures.Count > 0)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(container => Section(
                            container,
                            "Müsabaka sonuçları",
                            ["Tarih", "Tür", "Rakip", "Skor", "Sonuç"],
                            [3, 2, 6, 2, 3],
                            fixtures.Select(row => new[]
                            {
                                row.Date.ToString("dd.MM.yyyy"), row.Type, row.Opponent,
                                $"{row.OurScore} – {row.OpponentScore}", row.Outcome
                            }),
                            "Yalnızca “Tamamlandı” durumundaki müsabakalar sayılır."));
                    }

                                                if (matchAthletes.Count > 0)
                                                {
                                                    column.Item().PageBreak();
                                                    column.Item().Element(container => Section(
                                                        container,
                                                        "Sporcu müsabaka istatistikleri",
                                                        ["Sporcu", "Kart no", "Kadroda", "İlk 11", "Süre alan", "Dakika", "Oynama oranı", "Gol", "Asist", "Sarı", "Kırmızı"],
                                                        [5, 3, 2, 2, 2, 2, 2.5f, 1.5f, 1.5f, 1.5f, 1.8f],
                                                        matchAthletes.Select(row => new[]
                                                        {
                                                            row.Name, row.CardNumber ?? "", row.InSquad.ToString(), row.Starts.ToString(),
                                                            row.Appearances.ToString(), row.Minutes.ToString(),
                                                            row.PossibleMinutes == 0 ? "—" : $"{100d * row.Minutes / row.PossibleMinutes:0.#}%",
                                                            row.Goals.ToString(), row.Assists.ToString(),
                                                            row.YellowCards.ToString(), row.RedCards.ToString()
                                                        }),
                                                        "Oynama oranı = oynanan dakika / takımın tamamlanan müsabakalarının toplam süresi. Dakikalar kayıtlı müsabaka süresi, değişiklik ve kırmızı kart dakikalarından hesaplanır."));
                                                }
                                            });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Sayfa ").FontColor(MutedColor);
                    text.CurrentPageNumber().FontColor(MutedColor);
                    text.Span(" / ").FontColor(MutedColor);
                    text.TotalPages().FontColor(MutedColor);
                });
            });
        }).GeneratePdf();
    }

    public static string BuildFileName(string seasonName, string teamName) =>
        TeamReportExcelExporter.BuildFileName(seasonName, teamName)
            .Replace(".xlsx", ".pdf", StringComparison.Ordinal);

    private static string Rate(int present, int absent) =>
        present + absent == 0 ? "—" : $"{100d * present / (present + absent):0.#}%";

    private static void Section(
        IContainer container,
        string title,
        string[] headers,
        float[] widths,
        IEnumerable<string[]> rows,
        string footnote)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text(title).FontSize(12).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var width in widths)
                        columns.RelativeColumn(width);
                });

                table.Header(header =>
                {
                    foreach (var text in headers)
                        header.Cell().Background(HeaderColor).Padding(5).Text(text).Bold();
                });

                foreach (var row in rows)
                {
                    foreach (var value in row)
                        table.Cell().BorderBottom(0.5f).BorderColor("#E3E7EF").Padding(5).Text(value);
                }
            });
            column.Item().Text(footnote).FontSize(8).FontColor(MutedColor);
        });
    }
}
