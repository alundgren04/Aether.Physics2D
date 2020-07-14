using Microsoft.Xna.Framework;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics.Contacts;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class HibernationManager
    {
        private World ActiveWorld { get; set; }
        public World HibernatedWorld { get; private set; }
        public List<BaseActiveArea> ActiveAreas = new List<BaseActiveArea>();
        private List<Body> BodiesToHibernate = new List<Body>();

        // TODO: only merge or spit every... 10th second?

        public HibernationManager(World activeWorld)
        {
            // store reference to active world
            this.ActiveWorld = activeWorld;

            // create a new world to store hibernated bodies
            // TODO: set properties to match active world? does it matter, since we're not stepping?
            this.HibernatedWorld = new World();

            // disable ID autosetting, as these should be set by the active world
            this.HibernatedWorld.AutoAssignMissingBodyIds = false;
            
            // do an initial game-world pass to hibernate all bodies
            // going forward, all active bodies will be gauranteed in be in an AA, so this won't be neccessary.
            // OPTIMIZATION IDEA: allow constructor to accept a list of active areas and process them prior to 
            //                    this initial hibernate, to prevent hibernating bodies we're going to instantly unhibernate.
            foreach (var body in this.ActiveWorld.BodyList)
            {
                // for now, we're just going to mark everything as neededing hibernation.
                this.BodiesToHibernate.Add(body);
            }
        }

        public void Update()
        {
            // We only process this when the world is unlocked, so we may safely modify bodies, etc.
            if (this.ActiveWorld.IsLocked)
                throw new InvalidOperationException("The active World is locked.");
            // We only process this when the world is unlocked, so we may safely modify bodies, etc.
            if (this.HibernatedWorld.IsLocked)
                throw new InvalidOperationException("The hibernated World is locked.");

            // Update all active area body AABBs
            // (all AAs)
            this.UpdateActiveAreaBodyAABBs();

            // split up large AAs
            // (BodyAA only)
            this.SplitSparseBodyActiveAreas();

            // Refresh active area AABBs, expiration timers, etc.
            // (all AAs)
            this.UpdateActiveAreasAABBs();

            // Merge body AAs which are touching. This helps things like stacks and piles stay in sync and also helps perf.
            // (BodyAA only)
            this.MergeDenseBodyActiveAreas();

            // Handle expirations.
            // (all AAs)
            this.RemoveExpiredActiveAreas();

            // un-hibernate bodies ("wake")
            // (all AAs)
            this.WakeBodiesInActiveAreas();

            // Add bodies to any AA they overlap
            // Because IAA are dynamic in size and position, we must always verify all relevant bodies are in them. (body AA should be merged in the "MergeDenseBodyActiveAreas" method.
            // (IndependentAA only)
            this.AddAwakeBodiesToIndependentActiveAreas();

            // Create body AAs for any bodies protruding outside independentAA
            this.AdjustBodyAAsForBodiesInIndependentAreas();

            this.RemoveBodyAAFullyWithinIndependentAreas();

            // Hibernate all flagged bodies.
            this.HibernateBodies();
        }

        private void RemoveBodyAAFullyWithinIndependentAreas()
        {
            var independentActiveAreas = this.ActiveAreas.Where(aa => aa.AreaType == ActiveAreaType.Independent).ToList();

            // process all independent active areas
            for (var independentActiveAreaIndex = 0; independentActiveAreaIndex < independentActiveAreas.Count; independentActiveAreaIndex++)
            {
                // get current independent active area
                var independentActiveArea = independentActiveAreas[independentActiveAreaIndex];

                var bodyActiveAreas = this.ActiveAreas.Where(aa => aa.AreaType == ActiveAreaType.BodyTracking).ToList();
                for (var bodyActiveAreaIndex = bodyActiveAreas.Count - 1; bodyActiveAreaIndex >= 0; bodyActiveAreaIndex--)
                {
                    // get current body active area
                    var bodyActiveArea = bodyActiveAreas[bodyActiveAreaIndex];

                    var bodyAAisEntirelyWithinIndependentAA = independentActiveArea.AABB.Contains(ref bodyActiveArea.AABB);
                    if( bodyAAisEntirelyWithinIndependentAA )
                    {
                        // add all bodies to this independent AA
                        this.MoveAreaBodies(bodyActiveArea, independentActiveArea);

                        // remove it...
                        this.RemoveActiveArea(bodyActiveArea);
                    }
                }
            }
        }

        private void MoveAreaBodies(BaseActiveArea fromActiveArea, BaseActiveArea toActiveArea)
        {
            var fromBodies = fromActiveArea.AreaBodies.Select(ab => ab.Body);
            var toBodies = toActiveArea.AreaBodies.Select(ab => ab.Body);
            foreach (var fromBody in fromBodies)
            {
                if (!toBodies.Contains(fromBody))
                {
                    toActiveArea.AreaBodies.Add(new AreaBody(fromBody));
                }
            }
            fromActiveArea.AreaBodies.Clear();
        }

        private void AdjustBodyAAsForBodiesInIndependentAreas()
        {
            var independentActiveAreas = this.ActiveAreas.Where(aa => aa.AreaType == ActiveAreaType.Independent).ToList();

            // process all active areas
            for (var i = 0; i < independentActiveAreas.Count; i++)
            {
                // get current active area
                var independentActiveArea = independentActiveAreas[i];

                // Find all bodies which collide with active area AABB.
                //foreach (var areaBody in activeArea.AreaBodies) {
                for (var areaBodyIndex = independentActiveArea.AreaBodies.Count - 1; areaBodyIndex >= 0; areaBodyIndex--)
                {
                    var areaBody = independentActiveArea.AreaBodies[areaBodyIndex];

                    var isFullyContained = independentActiveArea.AABB.Contains(ref areaBody.AABB);

                    if (!isFullyContained)
                    { 
                        // determine if needs to create own AA. (non-static)
                        var warrantsBodyActiveArea = areaBody.Body.BodyType != BodyType.Static; //&& body.Awake; //body.AngularVelocity != 0 || body.LinearVelocity.Length() > 0;

                        if (warrantsBodyActiveArea)
                        {
                            // determine if has own AA
                            //var bodyActiveArea = this.GetBodyActiveArea(areaBody.Body);

                            var bodyActiveAreasContainingBody = 
                                this.ActiveAreas.Where(aa => 
                                    aa.AreaType == ActiveAreaType.BodyTracking
                                    && aa.AreaBodies.Select(ab => ab.Body).Contains(areaBody.Body)
                                    );

                            if (bodyActiveAreasContainingBody.Count() == 0)
                            {
                                // create body AA
                                this.ActiveAreas.Add(new BodyActiveArea(areaBody.Body));
                            }
                            else
                            {
                                foreach (var bodyActiveArea in bodyActiveAreasContainingBody)
                                {
                                    // renew the expiration timer on this body AA, so it doesn't really begin expiring until its not touching the independent AA
                                    (bodyActiveArea as BodyActiveArea).RenewExpiration();
                                }
                            }
                        }

                        // if it's totally outside of the AA, then remove it.
                        var isOverlapping = independentActiveArea.AABB.Overlaps(ref areaBody.AABB);
                        var isTotallyOutside = !isOverlapping;
                        if (isTotallyOutside)
                        {
                            // remove it!
                            this.RemoveAreaBodyFromActiveArea(independentActiveArea, areaBody);
                        }
                    }
                }
            }
        }

        private void UpdateActiveAreaBodyAABBs()
        {
            foreach( var aa in this.ActiveAreas )
            {
                aa.UpdateAreaBodyAABBs();
            }
        }

        private void MergeDenseBodyActiveAreas()
        {
            var bodyAAs = this.ActiveAreas.Where(aa => aa.AreaType == ActiveAreaType.BodyTracking).ToList();

            // get first in list
            var curBodyAA = bodyAAs.ElementAtOrDefault(0);
            while (curBodyAA != null)
            {
                // remove first from the list
                bodyAAs.RemoveAt(0);

                // find all other bodyAA which touch this one.
                var touchingAAs = bodyAAs.Where(aa => curBodyAA.AABB.Overlaps(ref aa.AABB)).ToList();

                for( var i = touchingAAs.Count - 1; i >= 0; i--)
                {
                    var touchingAA = touchingAAs[i];

                    // add all these AABBs to this AA
                    //(curBodyAA as BodyActiveArea).AdditionalAABBs.Add(touchingAA.AABB);

                    var curBodyAABodies = curBodyAA.AreaBodies.Select(ab => ab.Body);
                    foreach (var touchingAABody in touchingAA.AreaBodies)
                    {
                        if (!curBodyAABodies.Contains(touchingAABody.Body))
                        {
                            // this body isn't in the current body AA, so let's add it.
                            curBodyAA.AreaBodies.Add(touchingAABody);
                        }
                    }

                    // ensure expiration time
                    //(curBodyAA as BodyActiveArea).EnsureExpirationNoLessThan(touchingAA as BodyActiveArea);

                    touchingAA.AreaBodies.Clear();

                    // remove them from the list of body AAs
                    bodyAAs.Remove(touchingAA);

                    // also from the global AAs
                    this.ActiveAreas.Remove(touchingAA);
                }

                // next in list
                curBodyAA = bodyAAs.ElementAtOrDefault(0);
            }
        }

        private void SplitSparseBodyActiveAreas()
        {
            // get all body AAs which have more than X bodies.
            const int LARGE_AA_BODY_COUNT = 2;
            var largeBodyAAs = this.ActiveAreas.Where(aa => aa.AreaType == ActiveAreaType.BodyTracking && aa.AreaBodies.Count >= LARGE_AA_BODY_COUNT).ToList();

            foreach (var bodyAA in largeBodyAAs)
            {
                for (var i = bodyAA.AreaBodies.Count - 1; i >= 0; i--)
                {
                    var curBodyArea = bodyAA.AreaBodies[i];

                    var isOverlappingAnotherBody = this.BodyOverlapsOtherBodiesInActiveArea(bodyAA, curBodyArea);
                    
                    if( !isOverlappingAnotherBody)
                    {
                        // we'll remove it from this body AA 
                        bodyAA.AreaBodies.RemoveAt(i);//.Remove(curBodyArea);

                        // create its own body AA
                        this.ActiveAreas.Add(new BodyActiveArea(curBodyArea.Body));

                        var nonStaticBodyCount = bodyAA.AreaBodies.Count(ab => ab.Body.BodyType != BodyType.Static);
                        if (nonStaticBodyCount == 0)
                        {
                            // there are no non-static bodies in the area, so we remove it.
                            this.RemoveActiveArea(bodyAA);

                            // now that we've decided to totally remove this AA, let's just move onto the next.
                            break;
                        }
                    }
                }
            }
        }

        private void RemoveActiveArea(BaseActiveArea activeArea)
        {
            for (var i = activeArea.AreaBodies.Count - 1; i >= 0; i--)
            {
                var areaBody = activeArea.AreaBodies[i];
                switch ( areaBody.Body.BodyType )
                {
                    case BodyType.Static:
                        // okay, remove it.
                        this.RemoveAreaBodyFromActiveArea(activeArea, areaBody);
                        break;

                    default:
                        throw new InvalidOperationException("An active area is being removed which contains bodies which are not static.");
                }
            }
            
            // remove the active area
            this.ActiveAreas.Remove(activeArea);
        }

        private bool BodyOverlapsOtherBodiesInActiveArea(BaseActiveArea activeArea, AreaBody areaBody)
        {
            for(var i = 0; i < activeArea.AreaBodies.Count; i++)
            {
                var otherAreaBody = activeArea.AreaBodies[i];

                // it's the same AreaBody so skip it.
                if (areaBody == otherAreaBody)
                    continue;

                if (areaBody.AABB.Overlaps(ref otherAreaBody.AABB))
                {
                    return true;
                }
            }

            return false;
        }

        private void HibernateBodies()
        {
            // remove any duplicates
            // NOTE: I saw a duplicate entry in this in one test. it could happen when a body leaves two AA at the same time... 
            // seems like a real edge-case. That may reveal a deeper bug... how is it that a body is ordered to be removed by two diff AA?
            // it seems this should go through all bodies... if in no AA, thne remove. 
            // I see now... bodies are marked as being outide AAs... then each AA removes bodies not in their AA... if they're not in another AA.
            // that could lead to dupes. But no harm, just remove the dupes here.
            this.BodiesToHibernate = this.BodiesToHibernate.DistinctBy(body => body.Id).ToList();

            // hibernate all flagged bodies.
            foreach (var body in this.BodiesToHibernate)
            {
                this.HibernateBody(body);
            }

            // clear the list
            this.BodiesToHibernate.Clear();
        }

        //private void ProcessActiveAreaBodyPositionChanges()
        //{

        //    // process all active areas
        //    var activeAreas = this.ActiveAreas;//.Where(aa => aa.AreaType == ActiveAreaType.Independent).ToList();

        //    // process all active areas
        //    for (var i = 0; i < activeAreas.Count; i++)
        //    {
        //        // get current active area
        //        var activeArea = this.ActiveAreas[i];

        //        // Loop over current ActiveArea bodies to update their statuses.
        //        for (var areaBodyIndex = activeArea.AreaBodies.Count - 1; areaBodyIndex >= 0; areaBodyIndex--)
        //        {
        //            // get the body
        //            var areaBody = activeArea.AreaBodies[areaBodyIndex];
        //            var body = areaBody.Body;

        //            if (areaBody.PositionStatus == AreaBodyStatus.Invalid)
        //            {
        //                throw new InvalidProgramException("All active area bodies should have their position status set by this point.");
        //            }

        //            if (areaBody.PositionStatus == AreaBodyStatus.TotallyIn)
        //            {

        //                // this body is totally within this AA, so if there's a separate AA tracking this one specifically, 
        //                // it's not needed, as that would be redundant.
        //                // OPTIMIZATION IDEA: give body a ref to its own tracking AA. if !null when set, throw. super-easy look-up.
        //                var bodyAAs = this.ActiveAreas.Where(aa =>
        //                    aa != activeArea // other AA is not this AA
        //                    && aa.AreaType == ActiveAreaType.BodyTracking // other AA is a body tracking AA...
        //                    && (aa as BodyActiveArea).TrackedBody == body // ...and it's tracking this body specifically
        //                    ); //&& aa.Bodies.Select( aab => aab.Body ).Any( b => b == body ) ); // 

        //                switch (bodyAAs.Count())
        //                {
        //                    case 0:
        //                        // no problem. no bodyAA to care about.
        //                        break;

        //                    case 1:
        //                        // destroy it! it is redundant.
        //                        this.ActiveAreas.Remove(bodyAAs.First());
        //                        break;

        //                    default:
        //                        // this really shouldn't happen. it means there is more than one bodyAA for this body.
        //                        throw new InvalidProgramException("There is more than one ActiveArea for this body. This should never happen. There is a bug elsewhere.");
        //                }
        //            }

        //            var statusHasChanged = areaBody.PriorStatus != areaBody.PositionStatus;
        //            if (statusHasChanged)
        //            {
        //                switch (areaBody.PositionStatus)
        //                {


        //                    case AreaBodyStatus.PartiallyIn:
        //                    case AreaBodyStatus.TotallyOut:

        //                        // determine if needs to create own AA. (non-static and awake)
        //                        var warrantsBodyActiveArea = body.BodyType != BodyType.Static; //&& body.Awake; //body.AngularVelocity != 0 || body.LinearVelocity.Length() > 0;

        //                        if (warrantsBodyActiveArea)
        //                        {

        //                            // determine if has own AA
        //                            var bodyActiveArea = this.GetBodyActiveArea(body);

        //                            if (bodyActiveArea == null)
        //                            {
        //                                // create body AA
        //                                this.ActiveAreas.Add(new BodyActiveArea(body));
        //                            }
        //                            else
        //                            {
        //                                if (activeArea is IndependentActiveArea)
        //                                {
        //                                    // renew the expiration timer on this AA
        //                                    (bodyActiveArea as BodyActiveArea).RenewExpiration();
        //                                }
        //                                else
        //                                {
        //                                    // just ensure expiration isn't any shorter than this AA
        //                                    (bodyActiveArea as BodyActiveArea).EnsureExpirationNoLessThan(activeArea as BodyActiveArea);
        //                                }
        //                            }
        //                        }

        //                        break;
        //                }

        //                if (areaBody.PositionStatus == AreaBodyStatus.TotallyOut)
        //                {
        //                    this.RemoveAreacrgl.f.Body(activeArea, areaBody);
        //                }
        //            }


        //        }
        //    }
        //}

        //private BodyActiveArea GetBodyActiveArea(Body body)
        //{
        //    return this.ActiveAreas.FirstOrDefault(aa => aa.AreaType == ActiveAreaType.BodyTracking && (aa as BodyActiveArea).AreaBodies.Select(ab => ab.Body).Contains(body));
        //}

        /// <summary>
        /// Adds colliding bodies to active areas and updates position statuses for all bodies within the active area.
        /// </summary>
        private void AddAwakeBodiesToIndependentActiveAreas()
        {
            var independentActiveAreas = this.ActiveAreas.Where(aa => aa.AreaType == ActiveAreaType.Independent).ToList();

            // process all active areas
            for (var i = 0; i < independentActiveAreas.Count; i++)
            {
                // get current active area
                var independentActiveArea = independentActiveAreas[i];

                // find all bodies which collide with active area AABB.
                var bodiesInActiveArea = this.ActiveWorld.FindBodiesInAABB(ref independentActiveArea.AABB);

                // add all bodies which weren't already in AA
                var independentActiveAreaBodies = independentActiveArea.AreaBodies.Select(b => b.Body).ToList();
                for (var biaaIndex = bodiesInActiveArea.Count - 1; biaaIndex >= 0; biaaIndex--)
                {
                    // get body
                    var body = bodiesInActiveArea[biaaIndex];

                    // determine if this body is already in the AA
                    var isAlreadyInActiveArea = independentActiveAreaBodies.Contains(body);

                    if (!isAlreadyInActiveArea)
                    {
                        // didn't find it. add it.
                        independentActiveArea.AreaBodies.Add(new AreaBody(body));
                    }
                }
            }
        }

        private void WakeBodiesInActiveAreas()
        {
            // process all active areas
            foreach (var activeArea in this.ActiveAreas)
            {
                // get all hibernated bodies in its aabb
                var hibernatedBodiesInActiveArea = this.HibernatedWorld.FindBodiesInAABB(ref activeArea.AABB);

                // wake them
                foreach (var hibernatedBody in hibernatedBodiesInActiveArea)
                {
                    this.WakeBody(hibernatedBody);

                    // add it to this AA
                    activeArea.AreaBodies.Add(new AreaBody(hibernatedBody));
                }
            }
        }

        private void RemoveExpiredActiveAreas()
        {
            for (var i = this.ActiveAreas.Count - 1; i >= 0; i--)
            {
                var activeArea = this.ActiveAreas[i];

                if (activeArea.IsExpired)
                {
                    this.ProcessExpiredActiveArea(activeArea);
                }
            }
        }

        private void ProcessExpiredActiveArea(BaseActiveArea activeArea)
        {
            // if this is a body tracking AA, only remove if not within any other AA
            if (activeArea.AreaType == ActiveAreaType.BodyTracking)
            {
                var bodyActiveArea = activeArea as BodyActiveArea;

                var bodies = activeArea.AreaBodies.Select(ab => ab.Body);
                var isAnotherActiveAreaContainingBodiesInCommon = this.ActiveAreas.Any(aa =>
                    aa != activeArea // ...a different active area
                    && aa.IsExpired == false // ...isn't also an expired AA
                    && aa.AreaBodies.Select(aab => aab.Body).Intersect(bodies).Any()); // contains any bodies in common

                if (isAnotherActiveAreaContainingBodiesInCommon)
                {
                    // abort further expiration processing for this active area
                    return;
                }

                // TODO:
                // next... check if all bodies connected by joints are also expired...
                // if not... abort
                // bug... if joint is expired... but can't be removed due to above logic... would be hibernated and immediately awoken.
                // also... would have to recursively check all of that joined body's joined bodies... hmm.
            }

            // remove all area bodies from this active area...
            for (var bodyIndex = activeArea.AreaBodies.Count - 1; bodyIndex >= 0; bodyIndex--)
            {
                var areaBody = activeArea.AreaBodies[bodyIndex];

                this.RemoveAreaBodyFromActiveArea(activeArea, areaBody);
            }

            // remove this active area...
            this.ActiveAreas.Remove(activeArea);
        }

        private void UpdateActiveAreasAABBs()
        {
            // process all active areas
            for (var i = this.ActiveAreas.Count - 1; i >= 0; i--)
            {
                var activeArea = this.ActiveAreas[i];

                // update its bounding box
                activeArea.UpdateAABB();
            }
        }

        private void RemoveAreaBodyFromActiveArea(BaseActiveArea activeArea, AreaBody areaBody)
        {
            // remove it from this AA.
            activeArea.AreaBodies.Remove(areaBody);

            // if it's not in any other AA at this point, hibernate it.
            var isActiveAreasContainingBody = this.ActiveAreas.Any(aa => 
                aa != activeArea // is a different active area
                && aa.AreaBodies.Select(aab => aab.Body).Contains(areaBody.Body)); // contains the body
            
            if (!isActiveAreasContainingBody)
            {
                // no other active area has this body in it, so go ahead and hibernate the body
                this.BodiesToHibernate.Add(areaBody.Body);
            }
        }

        private void HibernateBody(Body body)
        {
            // remove from active world 
            // NOTE: we don't call the World.Remove() method, as that actually destroys the body, fixtures, etc. 
            this.ActiveWorld.Remove(body);

            // add to hibernated world
            this.HibernatedWorld.Add(body);
        }
        /// <summary>
        /// This is really only called when turning off hibernation, as the woken bodies aren't added to any AA and are essentially "orphaned."
        /// </summary>
        internal void ReviveAll()
        {
            // wake them
            for (var i = this.HibernatedWorld.BodyList.Count - 1; i >= 0; i--)
            {
                var hibernatedBody = this.HibernatedWorld.BodyList[i];

                this.WakeBody(hibernatedBody);
            }
        }

        private void WakeBody( Body hibernatedBody )
        {
            // remove from the hibernated world
            this.HibernatedWorld.Remove(hibernatedBody);

            // add to active world
            this.ActiveWorld.Add(hibernatedBody);
        }
    }
}
