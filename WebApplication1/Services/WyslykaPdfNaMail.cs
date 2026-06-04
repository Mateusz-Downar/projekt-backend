using projektBackend.Models; 
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Net.Mail;

namespace projektBackend.Services
{
    public class EmailPdfService
    {
        public void GenerujIWyslij(Rezerwacja rezerwacja)
        {
            // 1. Generowanie PDF 
            QuestPDF.Settings.License = LicenseType.Community;
            byte[] plikPdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Header().Text("Potwierdzenie Rezerwacji").FontSize(20).SemiBold();
                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().Text($"Stół nr: {rezerwacja.NumerStolu}");
                        column.Item().Text($"Imię: {rezerwacja.Imie}");
                        column.Item().Text($"Data: {rezerwacja.DataIGodzina}");
                    });
                });
            }).GeneratePdf();

            //BREVO KONFIGURACJA ITP ITD
            var smtpClient = new SmtpClient("smtp-relay.brevo.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("domena zapisales na fb", "klucz zapisales na fb"),
                EnableSsl = true,
            };

            // 3. Tworzenie maila
            var mailMessage = new MailMessage
            {
                From = new MailAddress("matidownar@gmail.com", "System Rezerwacji"),
                Subject = "Twoje potwierdzenie rezerwacji",
                Body = "Dziękujemy za rezerwację! W załączniku przesyłamy potwierdzenie.",
            };

            // Jeśli klient nie podał maila, możemy to zignorować lub wysłać na jakiś domyślny
            var adresDocelowy = string.IsNullOrEmpty(rezerwacja.Email) ? "test@test.pl" : rezerwacja.Email;
            mailMessage.To.Add(adresDocelowy);

            using (var stream = new MemoryStream(plikPdf))
            {
                mailMessage.Attachments.Add(new Attachment(stream, "Bilet.pdf", "application/pdf"));
                smtpClient.Send(mailMessage);
            }
        }
    }
}