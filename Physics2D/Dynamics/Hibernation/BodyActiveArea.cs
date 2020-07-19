using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class BodyActiveArea : BaseActiveArea
    {
        //internal Body TrackedBody { get; set; }

        /// <summary>
        /// Creation time in UTC and ticks.
        /// </summary>
        internal long CreationUtcTime { get; private set; }

        public float SecondsAgoCreated
        {
            get
            {
                var ts = new TimeSpan(DateTime.UtcNow.Ticks - this.CreationUtcTime);
                return (float)ts.TotalSeconds;
            }
        }

        internal BodyActiveArea(Body body) : base()
        {
            // store the body to track
            //this.TrackedBody = trackedBody;

            // automatically add it to the list of bodies as "totally in"
            this.AddBody(body);

            this.UpdateAABB();
                
            // set type
            this.AreaType = ActiveAreaType.BodyTracking;

            // store creation time
            this.RenewExpiration();
        }

        //public List<AABB> AdditionalAABBs = new List<AABB>();
        //public float BodyAABBMargin = Settings.BodyActiveAreaMargin;

        internal override void UpdateAABB()
        {
            //this.Center();

            var firstBody = this.AreaBodies[0].Body;
                
            // simply center on the body.
            this.Position = firstBody.Position;

            // update AABB to body's AABB
            //this.TrackedBody.World.ContactManager.BroadPhase.GetFatAABB(this.TrackedBody.BroadphaseProxyId, out this.AABB);

            //// add a little margin 
            //const float AABB_MARGIN = Settings.BodyActiveAreaMargin;
            //this.AABB = new AABB(this.AABB.Center, this.AABB.Width + AABB_MARGIN, this.AABB.Height + AABB_MARGIN);

            //this.AABB = BaseActiveArea.CalculateBodyAABB(this.TrackedBody, BodyAABBMargin);

            // add all other additional AABBs
            //for( var i = 0; i <this.AdditionalAABBs.Count; i++)
            //{
            //    var addtlAABB = this.AdditionalAABBs[i];
            //    this.AABB.Combine(ref addtlAABB);
            //}

            // add all other body AABBs
            var isAABBSet = false;
            for (var i = 0; i < this.AreaBodies.Count; i++)
            {
                var areaBody = this.AreaBodies[i];

                if( areaBody.Body.BodyType == BodyType.Static)
                {
                    continue;
                }

                if( isAABBSet )
                {
                    this.AABB.Combine(ref areaBody.AABB);
                }
                else
                {
                    this.AABB = areaBody.AABB;
                    isAABBSet = true;
                }                
            }

            // update whether is expired
            this.IsExpired = this.SecondsAgoCreated >= Settings.SecondsUntilHibernate;
        }

        //public Vector2 BodiesCenter;
        //private void Center()
        //{
        //    // TODO: instead of doing this off of positions, use AABBs. combine all body AABBs, get center, find body closest to center.

        //    // if it has multiple bodies, then center on the one closest to the center.
        //    if (this.Bodies.Count > 1)
        //    {
        //        // find average body position
        //        var averagePosition = Vector2.Zero;
        //        foreach (var body in this.Bodies.Select(ab => ab.Body))
        //        {
        //            // sum 'em all
        //            averagePosition += body.Position;
        //        }
        //        // divide by #
        //        averagePosition /= this.Bodies.Count;


        //        //Body closestBody = this.Bodies[0].Body;
        //        //var closestDistance = Vector2.Distance(this.closestBody.Position)
        //        //BodyclosestBody = this.Bodies[0];
        //        //for (var i = 1; i < this.Bodies.Count)
        //        //    var centerMostBody = this.Bodies.Min(ab => Vector ab.Body.Position)

        //        BodiesCenter = averagePosition;
        //    }
        //    else
        //    {
        //        // just use the center of the tracked body.
        //        BodiesCenter = this.TrackedBody.Position;
        //    }
        //}

        /// <summary>
        /// If secondsToAdd is null, then renews expiration time as if it was just created, otherwise adds the specified number of seconds.
        /// </summary>
        /// <param name="secondsToAdd"></param>
        internal void RenewExpiration(float? secondsToAdd = null)
        {
            if (secondsToAdd.HasValue)
            {
                // add seconds
                this.CreationUtcTime += (long)(TimeSpan.TicksPerSecond * secondsToAdd.Value);
            } else {
                // snapshot this time. it could be creation or renewal.
                this.CreationUtcTime = DateTime.UtcNow.Ticks;
            }

            // reset expiration.
            this.IsExpired = false;
        }

        internal void EnsureExpirationNoLessThan( BodyActiveArea bodyActiveArea )
        {
            if( this.CreationUtcTime < bodyActiveArea.CreationUtcTime)
            {
                this.CreationUtcTime = bodyActiveArea.CreationUtcTime;
            }
        }
    }
}
