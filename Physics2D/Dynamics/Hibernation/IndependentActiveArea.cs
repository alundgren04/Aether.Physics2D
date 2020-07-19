using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class IndependentActiveArea : BaseActiveArea
    {
        public IndependentActiveArea() : base()
        {
            this.AreaType = ActiveAreaType.Independent;
        }

        public void Expire()
        {
            this.IsExpired = true;
        }

        public void SetPosition(Vector2 position)
        {
            this.Position = position;
        }

        public void SetRadius(float radius)
        {
            this.Radius = radius;
        }

        internal override void UpdateAABB()
        {
            // update AABB
            var diameter = this.Radius * 2.0f;
            this.AABB = new AABB(this.Position, diameter, diameter);
        }
    }
}
