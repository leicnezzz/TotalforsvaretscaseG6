using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Totalforsvaret.Areas.Offentlig.ViewModels;

internal sealed class CoordinateRangeAttribute(Type coordinateType, int limit) : RangeAttribute(
    coordinateType, (-limit).ToString(CultureInfo.InvariantCulture), limit.ToString(CultureInfo.InvariantCulture))
{
}
