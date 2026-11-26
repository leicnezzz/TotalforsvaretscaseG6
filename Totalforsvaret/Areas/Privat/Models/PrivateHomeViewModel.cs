namespace Totalforsvaret.Areas.Privat.Models;

/// <summary>
/// Carries the signed-in actor's name and photo availability for the private dashboard.
/// </summary>
public class PrivateHomeViewModel
{
	public bool HasPhoto { get; init; }
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