using projektBackend.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;

public class PdfGenerator
{
    public byte[] GenerujPotwierdzenie(Rezerwacja rezerwacja)
    {
        
        QuestPDF.Settings.License = LicenseType.Community;

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Header().Text("Potwierdzenie rezerwacji").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Rezerwacja dla: {rezerwacja.Imie}");
                    column.Item().Text($"Stół nr: {rezerwacja.NumerStolu}");
                    column.Item().Text($"Data: {rezerwacja.DataIGodzina}");
                    column.Item().Text($"Czas trwania: {rezerwacja.CzasTrwaniaWGodzinach}h");
                });
                page.Footer().Text(x => { x.CurrentPageNumber(); });
            });
        }).GeneratePdf();
    }
}