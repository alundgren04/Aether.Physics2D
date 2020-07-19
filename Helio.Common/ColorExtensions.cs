using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helio.Common
{
    public static class ColorExtensions
    {
        public static Color MultiplyBy( this Color color, float factor)
        {
            return Color.FromArgb(
                (int)(color.A * factor),
                (int)(color.R * factor),
                (int)(color.G * factor),
                (int)(color.B * factor)
                );
        }
    }
}
