using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

/// <summary>
/// Applies a symmetric coordinate limit to a numeric input while preserving its numeric type.
/// </summary>
internal sealed class CoordinateRangeAttribute : RangeAttribute
{
    public CoordinateRangeAttribute(Type coordinateType, int limit)
        : base(coordinateType, (-limit).ToString(CultureInfo.InvariantCulture), limit.ToString(CultureInfo.InvariantCulture))
    {
    }
}
