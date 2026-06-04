
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using projektBackend.Data;
using projektBackend.Services;


namespace projektBackend.RabbitMq
{
    public class RabbitMqWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public RabbitMqWorker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            System.Diagnostics.Debug.WriteLine("\n========== UWAGA: WORKER RABBITMQ URUCHOMIŁ SIĘ ==========\n");
            Console.WriteLine("\n========== UWAGA: WORKER RABBITMQ URUCHOMIŁ SIĘ ==========\n");

            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "pdf_email_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                Console.WriteLine($"\n[WORKER] >>> ODEBRANO WIADOMOŚĆ Z KOLEJKI! <<<");

                var body = ea.Body.ToArray();
                var idString = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[WORKER] Odczytany tekst z wiadomości to: '{idString}'");

                if (int.TryParse(idString, out int rezerwacjaId))
                {
                    Console.WriteLine($"[WORKER] Przekonwertowano na ID: {rezerwacjaId}. Szukam w bazie...");

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var rezerwacja = dbContext.Rezerwacje.Find(rezerwacjaId);

                        if (rezerwacja == null)
                        {
                            Console.WriteLine($"[WORKER - BŁĄD] Baza danych mówi, że NIE MA rezerwacji o ID {rezerwacjaId}!");
                        }
                        else if (rezerwacja.CzyWyslanoPotwierdzenie)
                        {
                            Console.WriteLine($"[WORKER - BŁĄD] Rezerwacja ID {rezerwacjaId} ma już flagę CzyWyslanoPotwierdzenie = true! Pomijam wysyłkę.");
                        }
                        else
                        {
                            Console.WriteLine($"[WORKER] Rezerwacja znaleziona, flaga jest na false. ZACZYNAM WYSYŁKĘ!");
                            try
                            {
                                var emailService = new EmailPdfService();
                                emailService.GenerujIWyslij(rezerwacja);

                                rezerwacja.CzyWyslanoPotwierdzenie = true;
                                dbContext.SaveChanges();

                                Console.WriteLine($"[SUKCES] Mail został wysłany bezbłędnie!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"\n================= AWARIA WYSYŁKI MAIL (BREVO) =================");
                                Console.WriteLine($"[GŁÓWNY BŁĄD]: {ex.Message}");
                                if (ex.InnerException != null)
                                {
                                    Console.WriteLine($"[SZCZEGÓŁY]: {ex.InnerException.Message}");
                                }
                                Console.WriteLine($"===============================================================\n");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[WORKER - BŁĄD] Tekst '{idString}' to nie jest poprawna liczba (ID)!");
                }
            };

            channel.BasicConsume(queue: "pdf_email_queue", autoAck: true, consumer: consumer);

            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}