using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Common;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public abstract class BaseActiveArea
    {

        public AABB AABB;
        public Vector2 Position { get; protected set; }
        public float Radius { get; protected set; }
        public ActiveAreaType AreaType { get; protected set; }
        public ObservableList<AreaBody> AreaBodies { get; private set; }
        public bool IsExpired { get; protected set; }
        public HashSet<int> BodyIds { get; protected set; }


        public BaseActiveArea()
        {
            this.AreaBodies = new ObservableList<AreaBody>();
            this.BodyIds = new HashSet<int>();
        }
        
        internal void UpdateAreaBodyAABBs()
        {
            foreach( var areaBody in this.AreaBodies )
            {
                areaBody.UpdateAABB();
            }
        }

        internal virtual void UpdateAABB()
        {
            throw new NotImplementedException("Update method must be overridden in child class of BaseActiveArea.");
        }

        internal static AABB CalculateBodyAABB(Body body, float margin = Settings.BodyActiveAreaMargin)
        {
            // get body AABB
            AABB bodyAabb;
            body.World.ContactManager.BroadPhase.GetFatAABB(body.BroadphaseProxyId, out bodyAabb);

            // add a little margin 
            bodyAabb = new AABB(bodyAabb.Center, bodyAabb.Width + margin, bodyAabb.Height + margin);

            return bodyAabb;
        }

        public void AddBody ( Body body )
        {
            this.AreaBodies.Add(new AreaBody(body));
            this.BodyIds.Add(body.Id);
        }

        public void RemoveBody(AreaBody areaBody)
        {
            this.AreaBodies.Remove(areaBody);
            this.BodyIds.Remove(areaBody.Body.Id);
        }

        public void ClearBodies()
        {
            this.AreaBodies.Clear();
            this.BodyIds.Clear();
        }
    }
}
