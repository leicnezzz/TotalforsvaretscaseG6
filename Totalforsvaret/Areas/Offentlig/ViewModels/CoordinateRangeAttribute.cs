using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

/// <summary>Applies a symmetric coordinate limit to a numeric input while preserving its numeric type.</summary>
internal sealed class CoordinateRangeAttribute(Type coordinateType, int limit) : RangeAttribute(
    coordinateType, (-limit).ToString(CultureInfo.InvariantCulture), limit.ToString(CultureInfo.InvariantCulture))
{
}
