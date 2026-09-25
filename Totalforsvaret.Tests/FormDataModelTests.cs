using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Totalforsvaret.Models;

namespace Totalforsvaret.Tests;

public class FormDataModelTests
{
    [Test]
    public void Validering_NavnMangler_GirFeilForNavn()
    {
        var model = LagGyldigSkjema();
        model.Navn = string.Empty;
        var feil = new List<ValidationResult>();

        var gyldig = Validator.TryValidateObject(
            model, new ValidationContext(model), feil, validateAllProperties: true);

        Assert.That(gyldig, Is.False);
        Assert.That(feil.SelectMany(f => f.MemberNames),
            Does.Contain(nameof(FormDataModel.Navn)));
    }

    [Test]
    public void Validering_KartplasseringMangler_GirFeilForKoordinater()
    {
        var model = LagGyldigSkjema();
        model.Latitude = string.Empty;
        model.Longitude = string.Empty;
        var feil = new List<ValidationResult>();

        var gyldig = Validator.TryValidateObject(
            model, new ValidationContext(model), feil, validateAllProperties: true);

        Assert.That(gyldig, Is.False);
        var feltMedFeil = feil.SelectMany(f => f.MemberNames).ToList();
        Assert.That(feltMedFeil, Does.Contain(nameof(FormDataModel.Latitude)));
        Assert.That(feltMedFeil, Does.Contain(nameof(FormDataModel.Longitude)));
    }

    [Test]
    public void Validering_TilgjengelighetErNei_ErGyldig()
    {
        var model = LagGyldigSkjema();
        model.Available = false;
        var feil = new List<ValidationResult>();

        var gyldig = Validator.TryValidateObject(
            model, new ValidationContext(model), feil, validateAllProperties: true);

        Assert.That(gyldig, Is.True);
        Assert.That(feil, Is.Empty);
    }

    private static FormDataModel LagGyldigSkjema() => new()
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
