using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Totalforsvaret.Models;

namespace Totalforsvaret.Tests;

public class FormDataModelTests
{
    [Test]
    public void Validering_NavnMangler_GirFeilForNavn()
    {
        var model = LagGyldigSkjema(); // Lager et skjema med et tomt navn som tester validering. Inneholder gyldige verdier. 
		model.Navn = string.Empty; // Fjerner navnet for å teste validering.
        var feil = new List<ValidationResult>(); // Oppretter en liste som samle opp valideringsfeil.

        var gyldig = Validator.TryValidateObject(
            model, new ValidationContext(model), feil, validateAllProperties: true); // Validerer hele modellen og samler opp eventuelle valideringsfeil i listen "feil". Hvis modellen er gyldig, vil "gyldig" være true, eller false.
		Assert.That(gyldig, Is.False); // Kontrollerer at modellen ikke er gyldig, siden navnet mangler. Hvis modellen er gyldig, vil testen feile. 
        Assert.That(feil.SelectMany(f => f.MemberNames),
            Does.Contain(nameof(FormDataModel.Navn))); // Kontrollerer at valideringsfeilen gjelder navnet. 
    }

    [Test]
    public void Validering_KartplasseringMangler_GirFeilForKoordinater() 
    {
        var model = LagGyldigSkjema(); // Lager et skjema med gyldige verdier. 
        model.Latitude = string.Empty;
        model.Longitude = string.Empty; // Fjerner koordinatene for å teste validering. 
        var feil = new List<ValidationResult>(); // Oppretter en liste for å lagre valideringsfeil. 

        var gyldig = Validator.TryValidateObject(
            model, new ValidationContext(model), feil, validateAllProperties: true); // Validerer hele modellen og alle egenskapene.

        Assert.That(gyldig, Is.False); // Kontrollerer at modellen ikke er gyldig, siden koordinatene mangler. Hvis modellen er gyldig, vil testen feile. 
        var feltMedFeil = feil.SelectMany(f => f.MemberNames).ToList();
        Assert.That(feltMedFeil, Does.Contain(nameof(FormDataModel.Latitude))); // Kontrollerer at latitude er registrert som ugyldig.
        Assert.That(feltMedFeil, Does.Contain(nameof(FormDataModel.Longitude))); // Kontrollerer at longitude er registrert som ugyldig. 
    }

    [Test]
    public void Validering_TilgjengelighetErNei_ErGyldig()
    {
        var model = LagGyldigSkjema(); // Lager et skjema med gyldige verdier.
        model.Available = false; // Setter tilgjengelighet til false for å teste validering
        var feil = new List<ValidationResult>(); // Oppretter en liste for å lagre valideringsfeil.

        var gyldig = Validator.TryValidateObject(
            model, new ValidationContext(model), feil, validateAllProperties: true); // Validerer hele modellen og alle egenskapene. 

        Assert.That(gyldig, Is.True); // Kontrollerer at modellen er gyldig, siden tilgjengelighet er satt til false.
        Assert.That(feil, Is.Empty); // Kontrollerer at det ikke ble ooprettet noen valideringsfeil. 
    }

    private static FormDataModel LagGyldigSkjema() => new() // Oppretter standardverduer for skjemaet son gjør at modellen er gyldig. Dette brukes i testene for å teste validering. 
    {
        Navn = "Albert Einstein",
        RessursType = "ATV",
        CategoryId = 2,
        Available = true,
        TilgjengeligFra = new DateTime(2026, 10, 1, 12, 0, 0),
        Kontaktpunkt = "12345678",
        Latitude = "58.14670",
        Longitude = "7.99560"
    };
}
