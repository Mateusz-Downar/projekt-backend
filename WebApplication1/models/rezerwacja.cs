using System.ComponentModel.DataAnnotations;

namespace projektBackend.Models 
{
    public class Rezerwacja
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numer stołu jest wymagany.")]
        [Range(1, 4, ErrorMessage = "W naszym klubie mamy tylko stoły o numerach od 1 do 4.")]
        public int NumerStolu { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane.")]
        [MaxLength(50)]
        public string Imie { get; set; }

        [Required(ErrorMessage = "Numer telefonu jest wymagany.")]
        [Phone(ErrorMessage = "Podaj poprawny numer telefonu.")]
        public string Telefon { get; set; }

        [EmailAddress(ErrorMessage = "Podaj poprawny format adresu e-mail.")]
        public string? Email { get; set; } 

        [Required(ErrorMessage = "Data i godzina są wymagane.")]
        public DateTime DataIGodzina { get; set; }

        [Required(ErrorMessage = "Czas trwania jest wymagany.")]
        [Range(1, 10, ErrorMessage = "Możesz zarezerwować stół na maksymalnie 10 godzin.")]
        public int CzasTrwaniaWGodzinach { get; set; }

        public DateTime DataUtworzenia { get; set; } = DateTime.Now;

        public bool CzyWyslanoPotwierdzenie { get; set; } = false;
    }
}