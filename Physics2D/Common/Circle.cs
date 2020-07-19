using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Common
{
    public class Circle
    {
        public Vector2 Position { get; set; }
        public float Radius { get; set; }

        public Circle(Vector2 position, float radius)
        {
            this.Position = position;
            this.Radius = radius;
        }

        public bool IsOverlapping(Circle circle)
        {
            return this.Contains(circle.Position, circle.Radius);
        }

        public bool Contains(Vector2 position, float collisionBuffer = 0f)
        {
            // determine the distance their centers are apart
            var centerDistanceApart = (this.Position - position).Length();

            // get combined radii
            var combinedRadii = this.Radius + collisionBuffer;

            // if it's less than the combination of their radii then they're overlapping.
            bool isOverlapping = centerDistanceApart < combinedRadii;

            return isOverlapping;
        }

        public static Circle operator *(Circle value, float scaleFactor)
        {
            value.Radius *= scaleFactor;
            return value;
        }
    }
}
