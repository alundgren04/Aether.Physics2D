using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Helio.Common
{
    public static class ColorHelper
    {
        public static Color FromPercentages(float redPercent, float greenPercent, float bluePercent, float alphaPercent = 1.0f)
        {
            const int MAX = 255;
            return Color.FromArgb(
                (int)(MAX * alphaPercent),
                (int)(MAX * redPercent),
                (int)(MAX * greenPercent),
                (int)(MAX * bluePercent)
                );
        }
    }
}
