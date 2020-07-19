/* Original source Farseer Physics Engine:
 * Copyright (c) 2014 Ian Qvist, http://farseerphysics.codeplex.com
 * Microsoft Permissive License (Ms-PL) v1.1
 */

using System;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;
//using tainicom.Aether.Physics2D.Common;

namespace tainicom.Aether.Physics2D.Collision
{
    public interface IBroadPhase<TProxy>
    {
        int ProxyCount { get; }

        void UpdatePairs(BroadphaseDelegate<TProxy> callback);

        bool TestOverlap(int proxyIdA, int proxyIdB);

        int AddProxy(ref TProxy proxy);

        void RemoveProxy(int proxyId);

        void MoveProxy(int proxyId, ref AABB aabb, Vector2 displacement);

        TProxy GetProxy(int proxyId);

        void TouchProxy(int proxyId);

        void GetFatAABB(int proxyId, out AABB aabb);

        void Query(Func<AABB, int, object, bool> callback, ref AABB aabb, out bool proceeded, object userData);

        void RayCast(Func<RayCastInput, int, object, float> callback, ref RayCastInput input, out float maxFraction, object userData);

        void ShiftOrigin(Vector2 newOrigin);
    }
}