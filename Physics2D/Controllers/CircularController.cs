using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics;

namespace tainicom.Aether.Physics2D.Controllers
{
    public enum RangeLimitInterpolation
    {
        Linear = 0,
        None = 1
    }

    public class RangeLimit {
        public float Distance { get; private set; }
        public float Strength { get; private set; }
        public RangeLimitInterpolation Interpolation { get; set; }
    }

    public abstract class CircularController : Controller
    {
        public Vector2 Center { get; private set; }
        public List<RangeLimit> RangeLimits { get; private set; }

        private AABB _boundingBox;
        public AABB BoundingBox { get { return _boundingBox; } }

        /// <summary>
        /// A circular controller affects any body within "range" of its center. It must have at least one range, otherwise it would have no effect.
        /// The strength of the affect interpolates from one range to the next, e.g. the center strength to the first range strength, or the first range strength to the 2nd range strength,
        /// dependending where the fixture is. Most circular controllers just have one range, but it can be useful to have several. For example, the atmosphere of a planet starts at the center
        /// of the planet but has no strength until the surface, where it jumps up instantly, then drops off to the outer range (2nd range limit).
        /// We use two Range Limits for planet gravity, so we can ensure it's 9.8m/s^2 at the surface, zero at the center, then drops off to zero off the surface.
        /// Interpolation may be selected for each range limit to decide how it transitions.
        ///   Linear = changes consistently from one to the next.
        ///   None = the inner side of the range's value is used until the next range is reached.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="rangeLimits"></param>
        public CircularController( Vector2 center, List<RangeLimit> rangeLimits )
        {
            // Validate that we have at least one range.
            if (rangeLimits == null || rangeLimits.Count == 0)
                throw new ArgumentOutOfRangeException("Circular controllers require at least one range limit defined.");

            // store center
            this.Center = center;

            // store ranges in order of distance -- outermost first
            this.RangeLimits = rangeLimits.OrderByDescending( range => range.Distance ).ToList();

            // use outermost range to make bounding box
            var maxDiameter = this.RangeLimits[0].Distance * 2.0f;
            this._boundingBox = new AABB(center, maxDiameter, maxDiameter);
        }

        public override void Update(float secondsElapsed)
        {
            // Find all bodies within the bounding rectangle (AABB).
            var bodies = World.FindBodies(ref this._boundingBox);

            foreach (var body in bodies)
            {
                // only process dynamic bodies, not static or kinematic
                if (body.BodyType != BodyType.Dynamic)
                    continue;

                // skip sleeping bodies
                if (!body.Awake)
                    continue;

                // Test to ensure is within the circle... 
                var bodyDistFromCenter = (this.Center - body.Position).Length();
                var bodyRadius = body.GetBoundingCircle().Radius;
                //var radiusCombine = this.MaxRadius + bodyRadius;

                //var isBodyWithinCircle = bodyDistFromCenter < radiusCombine;
                //if (!isBodyWithinCircle)
                //{
                //    // it's totally outside of the circle, so just skip it.
                //    continue;
                //}

                //var isBodyCenterInsideInnerRadius = bodyDistFromCenter < this.MinRadius;
                //if (isBodyCenterInsideInnerRadius)
                //{
                //    // the body center is within the inner empty area, so skip it.
                //    continue;
                //}

                //this.UpdateBody(body, secondsElapsed);
            }
        }

        protected abstract void UpdateBody(Body body, float dt);
    }
}
