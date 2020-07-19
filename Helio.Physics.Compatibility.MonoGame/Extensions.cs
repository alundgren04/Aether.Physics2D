using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Common;

namespace Helio.Physics.Compatibility.MonoGame
{
    public static class Extensions
    {
        public static Microsoft.Xna.Framework.Vector2 ToMonoGame(this Vector2 vector2)
        {
            return new Microsoft.Xna.Framework.Vector2(vector2.X, vector2.Y);
        }

        public static Microsoft.Xna.Framework.Vector3 ToMonoGame(this Vector3 vector3)
        {
            return new Microsoft.Xna.Framework.Vector3(vector3.X, vector3.Y, vector3.Z);
        }

        public static Microsoft.Xna.Framework.Matrix ToMonoGame(this Matrix matrix)
        {
            return new Microsoft.Xna.Framework.Matrix(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
                );
        }

        public static Microsoft.Xna.Framework.Color ToMonoGame(this Color color)
        {
            return new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
        }

        public static Vector2 ToCommon(this Microsoft.Xna.Framework.Vector2 vector2)
        {
            return new Vector2(vector2.X, vector2.Y);
        }
    }
}
