using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;

/*
 * 1) Export from RUBE as JSON
 * 2) Replace all "massData-" with "massData" to remove the dash which is invalid with C#, replace all "filter-" with "filter", etc. (see RubeLoader for complete list)
 * 3) Create C# classes to match the JSON. http://json2csharp.com/
 * 4) Paste them here!
 * 5) Replace all "double" with "float"
 */

namespace tainicom.Aether.Physics2D.Loaders.RUBE
{
    public class RubeVertices
    {
        public List<float> x { get; set; }
        public List<float> y { get; set; }
    }

    public class RubePolygon
    {
        public RubeVertices vertices { get; set; }
    }

    public class RubeCircle
    {
        public RubeVector2 center { get; set; }
        public float radius { get; set; }
    }

    public class RubeFixture
    {
        public float density { get; set; }
        public float friction { get; set; }
        public string name { get; set; }
        public RubePolygon polygon { get; set; }
        public float restitution { get; set; }
        public int? filtergroupIndex { get; set; }
        public RubeCircle circle { get; set; }
    }

    public class RubeVector2
    {
        public float x { get; set; }
        public float y { get; set; }

        public Vector2 ToVector2()
        {
            return new Vector2(this.x, this.y);
        }
    }

    public class RubeBody
    {
        public float angle { get; set; }
        public float angularVelocity { get; set; }
        public bool awake { get; set; }
        public List<RubeFixture> fixture { get; set; }
        public RubeVector2 linearVelocity { get; set; }
        public float massDataI { get; set; }
        public RubeVector2 massDatacenter { get; set; }
        public float massDatamass { get; set; }
        public string name { get; set; }
        public RubeVector2 position { get; set; }
        public int type { get; set; }
    }

    public class RubeCollisionbitplanes
    {
        public List<string> names { get; set; }
    }

    public class RubeAnchorA
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    public class RubeAnchorB
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    public class RubeJoint
    {
        public RubeAnchorA anchorA { get; set; }
        public RubeAnchorB anchorB { get; set; }
        public int bodyA { get; set; }
        public int bodyB { get; set; }
        public float dampingRatio { get; set; }
        public float frequency { get; set; }
        public float length { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public bool? enableLimit { get; set; }
        public bool? enableMotor { get; set; }
        public float? jointSpeed { get; set; }
        public float? lowerLimit { get; set; }
        public float? maxMotorTorque { get; set; }
        public float? motorSpeed { get; set; }
        public float? refAngle { get; set; }
        public float? upperLimit { get; set; }
        public RubeVector2 localAxisA { get; set; }
        public float? maxMotorForce { get; set; }
    }

    public class RubeRootObject
    {
        public bool allowSleep { get; set; }
        public bool autoClearForces { get; set; }
        public List<RubeBody> body { get; set; }
        public RubeCollisionbitplanes collisionbitplanes { get; set; }
        public bool continuousPhysics { get; set; }
        public RubeVector2 gravity { get; set; }
        public List<RubeJoint> joint { get; set; }
        public int positionIterations { get; set; }
        public float stepsPerSecond { get; set; }
        public bool subStepping { get; set; }
        public int velocityIterations { get; set; }
        public bool warmStarting { get; set; }
        public List<RubeImage> image { get; set; }
    }

    public class RubeCorners
    {
        public List<float> x { get; set; }
        public List<float> y { get; set; }
    }

    public class RubeImage
    {
        public int aspectScale { get; set; }
        public int body { get; set; }
        public RubeVector2 center { get; set; }
        public RubeCorners corners { get; set; }
        public string file { get; set; }
        public int filter { get; set; }
        public List<int> glDrawElements { get; set; }
        public List<float> glTexCoordPointer { get; set; }
        public List<float> glVertexPointer { get; set; }
        public string name { get; set; }
        public float opacity { get; set; }
        public float scale { get; set; }
    }
}
