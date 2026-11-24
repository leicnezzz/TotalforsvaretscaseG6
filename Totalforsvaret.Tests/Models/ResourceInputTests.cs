using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Totalforsvaret.Models;

namespace Totalforsvaret.Tests;

public class ResourceInputTests
{
    private static ResourceInput ValidVehicle() => new()
    {
        Object = "Varebil",
        CategoryId = 1,
        Available = true,
        Kontaktpunkt = "+47 900 12 345",
        Address = "Rådhusgata 18, Kristiansand",
        Latitude = 58.1467m,
        Longitude = 7.9956m,
        Condition = "God",
        Vehicle = new VehicleInput()
    };

    private static bool IsValid(object input) => Validator.TryValidateObject(
        input, new ValidationContext(input), new List<ValidationResult>(), true);

    [Test]
    public void VehicleWithoutPlateOrAvailabilityDate_IsValid()
    {
        Assert.That(IsValid(ValidVehicle()), Is.True);
    }

    [Test]
    public void VehicleWithEquipmentDetails_IsRejected()
    {
        var input = ValidVehicle();
        input.Equipment = new EquipmentInput { EquipmentType = "Drone", Quantity = 1 };
        Assert.That(IsValid(input), Is.False);
    }

    [Test]
    public void EquipmentWithoutEquipmentDetails_IsRejected()
    {
        var input = ValidVehicle();
        input.CategoryId = 2;
        input.Vehicle = null;
        Assert.That(IsValid(input), Is.False);
    }

    [TestCase(null)]
    [TestCase(0)]
    [TestCase(100001)]
    public void InvalidEquipmentQuantity_IsRejected(int? quantity)
    {
        var input = new EquipmentInput { EquipmentType = "Drone", Quantity = quantity };
        Assert.That(IsValid(input), Is.False);
    }

    [Test]
    public void MissingCoordinates_AreRejected()
    {
        var input = ValidVehicle();
        input.Latitude = null;
        Assert.That(IsValid(input), Is.False);
    }

    [TestCase("123")]
    [TestCase("abcdefgh")]
    public void InvalidPhone_IsRejected(string phone)
    {
        var input = ValidVehicle();
        input.Kontaktpunkt = phone;
        Assert.That(IsValid(input), Is.False);
    }

    [Test]
    public void MissingPhone_IsReportedWithoutThrowing()
    {
        var input = ValidVehicle();
        input.Kontaktpunkt = null!;
        Assert.That(input.Validate(new ValidationContext(input)).Any(), Is.True);
    }
}
