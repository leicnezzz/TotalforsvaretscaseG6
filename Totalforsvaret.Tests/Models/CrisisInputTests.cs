using Totalforsvaret.Models;
using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.ViewModels;

namespace Totalforsvaret.Tests.Models;

public class CrisisInputTests
{
    [Test]
    public void CreateRequiresCoordinates()
    {
        var input = Fill(new CreateCrisisInput());

        Assert.That(InvalidFields(input), Is.EquivalentTo(new[] { nameof(input.Latitude), nameof(input.Longitude) }));
    }

    [Test]
    public void EditAllowsMissingCoordinates()
    {
        Assert.That(InvalidFields(Fill(new CrisisInput())), Is.Empty);
    }

    [TestCase(double.NaN, 0d, nameof(CrisisInput.Latitude))]
    [TestCase(0d, double.PositiveInfinity, nameof(CrisisInput.Longitude))]
    [TestCase(90.1, 0d, nameof(CrisisInput.Latitude))]
    [TestCase(0d, -180.1, nameof(CrisisInput.Longitude))]
    public void CreateRejectsInvalidCoordinates(double latitude, double longitude, string field)
    {
        var input = Fill(new CreateCrisisInput { Latitude = latitude, Longitude = longitude });

        Assert.That(InvalidFields(input), Is.EqualTo(new[] { field }));
    }

    [TestCase(CrisisStatus.Active, "Aktiv")]
    [TestCase(CrisisStatus.UnderControl, "Under kontroll")]
    [TestCase(CrisisStatus.Closed, "Avsluttet")]
    [TestCase(CrisisStatus.Unspecified, "")]
    public void StatusPreservesExistingStoredValues(CrisisStatus status, string storedValue)
    {
        Assert.That(status.ToNorwegianText(), Is.EqualTo(storedValue));
        Assert.That(CrisisStatusText.FromStoredValue(storedValue), Is.EqualTo(status));
    }

    [TestCase(CrisisStatus.Unspecified)]
    [TestCase((CrisisStatus)99)]
    public void UnknownOrUnspecifiedStatusCannotBeSubmitted(CrisisStatus status)
    {
        var input = Fill(new CrisisInput());
        input.Status = status;

        Assert.That(InvalidFields(input), Is.EqualTo(new[] { nameof(input.Status) }));
    }

    private static CrisisInput Fill(CrisisInput input)
    {
        input.Name = " Hendelse ";
        input.Description = " Beskrivelse ";
        input.Type = " Flom ";
        input.Severity = "Høy";
        input.Status = CrisisStatus.Active;
        input.StartTime = new DateTime(2026, 1, 1);
        return input;
    }

    private static string[] InvalidFields(CrisisInput input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).ToArray();
    }
}
