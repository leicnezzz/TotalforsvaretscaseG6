namespace Totalforsvaret.Areas.Privat.Models;

public class PrivateHomeViewModel
{
    public string Navn { get; init; } = "";

    public string Initialer
    {
        get
        {
            var deler = Navn.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (deler.Length == 0)
                return "?";

            if (deler.Length == 1)
                return deler[0][0].ToString().ToUpperInvariant();

            return $"{deler[0][0]}{deler[^1][0]}"
                .ToUpperInvariant();
        }
    }
}