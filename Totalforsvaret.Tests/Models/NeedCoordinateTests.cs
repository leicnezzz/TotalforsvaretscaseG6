using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.ViewModels;

namespace Totalforsvaret.Tests.Models;

public class NeedCoordinateTests
{
    [TestCase(nameof(NeedCreateViewModel.Latitude), -90, true)]
    [TestCase(nameof(NeedCreateViewModel.Latitude), 90, true)]
    [TestCase(nameof(NeedCreateViewModel.Latitude), -91, false)]
    [TestCase(nameof(NeedCreateViewModel.Latitude), 91, false)]
    [TestCase(nameof(NeedCreateViewModel.Longitude), -180, true)]
    [TestCase(nameof(NeedCreateViewModel.Longitude), 180, true)]
    [TestCase(nameof(NeedCreateViewModel.Longitude), -181, false)]
    [TestCase(nameof(NeedCreateViewModel.Longitude), 181, false)]
    public void ValidatesCoordinateBounds(string property, int coordinate, bool expected)
    {
        var input = new NeedCreateViewModel();
        var context = new ValidationContext(input) { MemberName = property };

        Assert.That(Validator.TryValidateProperty((decimal)coordinate, context, null), Is.EqualTo(expected));
    }
}
