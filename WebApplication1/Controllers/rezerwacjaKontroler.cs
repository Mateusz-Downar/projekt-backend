using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projektBackend.Data;
using projektBackend.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace projektBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RezerwacjeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RezerwacjeController(AppDbContext context)
        {
            _context = context;
        }

        // wszystkie
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rezerwacja>>> GetRezerwacje()
        {
            return Ok(await _context.Rezerwacje.ToListAsync());
        }

        // jedna po id
        [HttpGet("{id}")]
        public async Task<ActionResult<Rezerwacja>> GetRezerwacja(int id)
        {
            var rezerwacja = await _context.Rezerwacje.FindAsync(id);

            if (rezerwacja == null)
            {
                return NotFound(new { Kod = 404, Komunikat = "Nie znaleziono takiej rezerwacji." });
            }

            return Ok(rezerwacja);
        }

        // dodaje nową rezerwację
        [HttpPost]
        public async Task<ActionResult<Rezerwacja>> PostRezerwacja(Rezerwacja rezerwacja)
        {
            try 
            {
                // sprawdzenie daty
                if (rezerwacja.DataIGodzina < DateTime.Now)
                {
                    return BadRequest(new { Kod = 400, Komunikat = "Błąd daty" });
                }

                // czy zajety stol
                DateTime nowyPoczatek = rezerwacja.DataIGodzina;
                DateTime nowyKoniec = rezerwacja.DataIGodzina.AddHours(rezerwacja.CzasTrwaniaWGodzinach);

                bool czyZajety = await _context.Rezerwacje.AnyAsync(r =>
                    r.NumerStolu == rezerwacja.NumerStolu &&
                    nowyPoczatek < r.DataIGodzina.AddHours(r.CzasTrwaniaWGodzinach) &&
                    nowyKoniec > r.DataIGodzina
                );

                if (czyZajety)
                {
                    return Conflict(new { Kod = 409, Komunikat = $"Stół nr {rezerwacja.NumerStolu} jest już zajęty." });
                }

                // bazowo jest false zeby wyslalo
                rezerwacja.CzyWyslanoPotwierdzenie = false;

                // zapis w bazie
                _context.Rezerwacje.Add(rezerwacja);
                await _context.SaveChangesAsync();
                Console.WriteLine($"\n[KONTROLER] ---> WYSYŁAM ID {rezerwacja.Id} DO RABBITMQ! <---\n");

                // 4. RABBITMQ
                try
                {
                    var factory = new ConnectionFactory() { HostName = "localhost" };
                    using (var connection = factory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare(queue: "pdf_email_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                        var messageId = rezerwacja.Id.ToString();
                        var body = Encoding.UTF8.GetBytes(messageId);
                        channel.BasicPublish(exchange: "", routingKey: "pdf_email_queue", basicProperties: null, body: body);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd kolejki: {ex.Message}");
                }

                return CreatedAtAction(nameof(GetRezerwacja), new { id = rezerwacja.Id }, rezerwacja);
            }
            catch (Exception ex) 
            {
               
                var pelnyBlad = ex.Message;
                if (ex.InnerException != null)
                {
                    pelnyBlad += " | PRAWDZIWY POWÓD Z BAZY: " + ex.InnerException.Message;
                }

                Console.WriteLine($"!!! AWARIA: {pelnyBlad}");

                return StatusCode(500, new { Kod = 500, Komunikat = "Błąd serwera", Szczegoly = pelnyBlad });
            }
        }


        // Aktualizacja rezerwacji 
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRezerwacja(int id, Rezerwacja rezerwacja)
        {
            if (id != rezerwacja.Id)
            {
                return BadRequest(new { Kod = 400, Komunikat = "Błąd ID" });
            }

            // data
            if (rezerwacja.DataIGodzina < DateTime.Now)
            {
                return BadRequest(new
                {
                    Kod = 400,
                    Komunikat = "Błąd daty"
                });
            }


            // Sprawdza czy stol nie jest zajety nie licząc samej aktualizowanej rezerwacji
            DateTime nowyPoczatek = rezerwacja.DataIGodzina;
            DateTime nowyKoniec = rezerwacja.DataIGodzina.AddHours(rezerwacja.CzasTrwaniaWGodzinach);

            bool czyZajety = await _context.Rezerwacje.AnyAsync(r =>
                r.NumerStolu == rezerwacja.NumerStolu &&
                r.Id != id && // oprocz rezrwacji ktora edutyjemy
                nowyPoczatek < r.DataIGodzina.AddHours(r.CzasTrwaniaWGodzinach) &&
                nowyKoniec > r.DataIGodzina
            );
            if (czyZajety)
            {
                return Conflict(new
                {
                    Kod = 409,
                    Komunikat = $"Stół nr {rezerwacja.NumerStolu} jest już zajęty w terminie {rezerwacja.DataIGodzina}."
                });
            }

            // 3. Aktualizacja w bazie
            _context.Entry(rezerwacja).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("--- ZAPISANO REZERWACJĘ W BAZIE ---");
                
                Console.WriteLine("--- PRÓBUJĘ WYSŁAĆ DO RABBITMQ ---");
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RezerwacjaExists(id))
                {
                    return NotFound(new { Kod = 404, Komunikat = $"Rezerwacja o ID {id} nie istnieje." });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Kod = 500, Komunikat = "Błąd serwera podczas aktualizacji.", Szczegoly = ex.Message });
            }
        }


        private bool RezerwacjaExists(int id)
        {
            return _context.Rezerwacje.Any(e => e.Id == id);
        }

        // usuwanie  po id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRezerwacja(int id)
        {
            var rezerwacja = await _context.Rezerwacje.FindAsync(id);
            if (rezerwacja == null) return NotFound();

            _context.Rezerwacje.Remove(rezerwacja);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // pobieranie pdf
        [HttpGet("{id}/pobierz-pdf")]
        public IActionResult PobierzPdf(int id)
        {
            var rezerwacja = _context.Rezerwacje.Find(id);
            if (rezerwacja == null) return NotFound();

            var generator = new PdfGenerator();
            byte[] danePdf = generator.GenerujPotwierdzenie(rezerwacja);

            return File(danePdf, "application/pdf", $"Rezerwacja_{id}.pdf");
        }
    }
}