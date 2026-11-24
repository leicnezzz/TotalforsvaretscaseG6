using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using Totalforsvaret.Areas.Offentlig.Models;
using Totalforsvaret.Areas.Offentlig.ViewModels;

namespace Totalforsvaret.Tests.Models;

public class NeedStatusTests
{
    [TestCase(NeedStatus.Open, "Åpen")]
    [TestCase(NeedStatus.InProgress, "Under behandling")]
    [TestCase(NeedStatus.Resolved, "Løst")]
    [TestCase(NeedStatus.Unspecified, "")]
    public void PreservesStoredValues(NeedStatus status, string storedValue)
    {
        Assert.That(status.ToNorwegianText(), Is.EqualTo(storedValue));
        Assert.That(NeedStatusText.FromStoredValue(storedValue), Is.EqualTo(status));
    }

    [TestCase(null, false)]
    [TestCase(NeedStatus.Unspecified, false)]
    [TestCase((NeedStatus)99, false)]
    [TestCase(NeedStatus.Open, true)]
    [TestCase(NeedStatus.InProgress, true)]
    [TestCase(NeedStatus.Resolved, true)]
    public void ValidatesSubmittedStatus(NeedStatus? status, bool expected)
    {
        var input = new NeedCreateViewModel { Status = status };
        var context = new ValidationContext(input) { MemberName = nameof(input.Status) };

        Assert.That(Validator.TryValidateProperty(status, context, null), Is.EqualTo(expected));
    }
}
