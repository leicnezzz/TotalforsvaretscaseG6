using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Models
{
    public class PublicActor : Bruker
    {
        public string? Avdeling { get; set; }
    }
}
