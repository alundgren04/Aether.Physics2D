using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;

namespace tainicom.Aether.Physics2D.Controllers
{
    public class CircularGravityController : CircularController
    {
        public CircularGravityController(Vector2 center, List<RangeLimit> rangeLimits) : base(center, rangeLimits)
        {
        }

        public override void Update(float dt)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateBody(Body body, float dt)
        {
            throw new NotImplementedException();
        }
    }
}
