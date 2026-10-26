using System.ComponentModel.DataAnnotations;

namespace Totalforsvaret.Models
{
    public class Bruker // Superklasse for brukere offentlig og privat
    {
        public string? BrukerID { get; }
        private string? Passord { get; set; }

        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string? Telefonnummer { get; set; }
    }
}
