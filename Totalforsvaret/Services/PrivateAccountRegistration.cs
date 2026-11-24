using Totalforsvaret.Areas.Privat.Models;
using Totalforsvaret.Models;

namespace Totalforsvaret.Services;

internal static class PrivateAccountRegistration
{
    public static PrivateActor CreateUser(RegisterInput input)
    {
        var email = input.Email.Trim();
        return new PrivateActor
        {
            UserName = email,
            Email = email,
            Navn = $"{input.FirstName.Trim()} {input.LastName.Trim()}"
        };
    }
}