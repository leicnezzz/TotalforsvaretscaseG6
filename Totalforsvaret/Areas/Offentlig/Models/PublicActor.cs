using Totalforsvaret.Models;

namespace Totalforsvaret.Areas.Offentlig.Models
{
    /// <summary>Represents an account that can register crises and needs and coordinate resources.</summary>
    public class PublicActor : Bruker
    {
        public string? Avdeling { get; set; }
    }
}
