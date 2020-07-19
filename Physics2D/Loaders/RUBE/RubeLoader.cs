using System.Collections.Generic;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using System.IO;
using Newtonsoft.Json;
using tainicom.Aether.Physics2D.Dynamics.Joints;
using System.Linq;
using tainicom.Aether.Physics2D.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;

namespace tainicom.Aether.Physics2D.Loaders.RUBE
{
    public class RubeLoader
    {
        /// <summary>
        /// Loads bodies, fixtures, and joints of the specified R.U.B.E. JSON export file to the provided world. A common usage is loading the bodies into
        /// a temporary world object and then plucking those items you're interested in out of that world and deep cloning them into another world.
        /// </summary>
        /// <param name="fileName">File path of the input JSON, e.g. "data/myRubeWorld.json"</param>
        /// <param name="world">The world to load the bodies, fixtures, and joints into.</param>
        public static void Load(string fileName, World world)
        {
            // load the R.U.B.E. file
            using (
                var reader = new StreamReader(fileName))
            {
                string json = reader.ReadToEnd();

                // JSON clean-up...
                // NOTE: this list is certainly not complete, so it should be added to as other issues are discovered.
                json = json.Replace("massData-", "massData");
                json = json.Replace("filter-", "filter");
                json = json.Replace(@"""anchorB"" : 0", @"""anchorB"" :  { ""x"":0, ""y"":0 }");
                json = json.Replace(@"""center"" : 0", @"""center"" :  { ""x"":0, ""y"":0 }");
                json = json.Replace(@"""linearVelocity"" : 0", @"""linearVelocity"" :  { ""x"":0, ""y"":0 }");

                var rubeData = JsonConvert.DeserializeObject<RubeRootObject>(json);

                // Key = order loaded by
                var loadedBodies = new Dictionary<int, Body>();

                for (var rubeBodyIndex = 0; rubeBodyIndex < rubeData.body.Count; rubeBodyIndex++)
                {
                    var rubeBody = rubeData.body[rubeBodyIndex];

                    var body = new Body();

                    if (rubeBody.fixture != null)
                    {
                        foreach (var rubeFixture in rubeBody.fixture)
                        {
                            Fixture f = null;

                            if (rubeFixture.polygon != null)
                            {
                                var vertices = new Vertices();
                                for (int i = 0; i < rubeFixture.polygon.vertices.x.Count; i++)
                                {
                                    var x = rubeFixture.polygon.vertices.x[i];
                                    var y = rubeFixture.polygon.vertices.y[i];

                                    vertices.Add(new Vector2(x, y));
                                }
                                f = body.CreatePolygon(vertices, rubeFixture.density);
                            }
                            else if (rubeFixture.circle != null)
                            {
                                f = body.CreateCircle(
                                    rubeFixture.circle.radius,
                                    rubeFixture.density,
                                    rubeFixture.circle.center.ToVector2());
                            }
                            else
                            {
                                //throw new InvalidDataException();
                                continue;
                            }

                            f.Restitution = rubeFixture.restitution;
                            f.Friction = rubeFixture.friction;
                        }
                    }

                    body.Rotation = rubeBody.angle;
                    body.AngularVelocity = rubeBody.angularVelocity;
                    body.Awake = rubeBody.awake;
                    body.LinearVelocity = Vector2.Zero;
                    body.Position = new Vector2(rubeBody.position.x, rubeBody.position.y);
                    body.BodyType = (BodyType)rubeBody.type;
                    body.Tag = rubeBodyIndex;

                    // reset mass data
                    // NOTE: this probably isn't needed, but it shouldn't hurt anything
                    body.ResetMassData();
                    body.ResetDynamics();

                    // add to world
                    world.Add(body);

                    // add to load array
                    loadedBodies.Add(rubeBodyIndex, body);
                }

                if (rubeData.joint != null)
                {
                    foreach (var rubeJoint in rubeData.joint)
                    {
                        var bodyA = loadedBodies[rubeJoint.bodyA];
                        var bodyB = loadedBodies[rubeJoint.bodyB];
                        var anchorA = new Vector2(rubeJoint.anchorA.x, rubeJoint.anchorA.y);
                        var anchorB = new Vector2(rubeJoint.anchorB.x, rubeJoint.anchorB.y);

                        Joint joint = null;
                        switch (rubeJoint.type)
                        {

                            case "prismatic":
                                var prisJoint = new PrismaticJoint(
                                    bodyA,
                                    bodyB,
                                    anchorA,
                                    anchorB,
                                    rubeJoint.localAxisA.ToVector2());

                                prisJoint.LowerLimit = rubeJoint.lowerLimit.Value;
                                prisJoint.UpperLimit = rubeJoint.upperLimit.Value;
                                prisJoint.LimitEnabled = rubeJoint.enableLimit.Value;

                                joint = prisJoint;
                                break;

                            case "distance":
                                DistanceJoint distJoint = new DistanceJoint(
                                    bodyA,
                                    bodyB,
                                    anchorA,
                                    anchorB);

                                joint = distJoint;
                                break;

                            case "revolute":
                                var revJoint = new RevoluteJoint(
                                    bodyA,
                                    bodyB,
                                    anchorA,
                                    anchorB
                                    );

                                revJoint.MotorSpeed = rubeJoint.motorSpeed.Value;
                                revJoint.MaxMotorTorque = rubeJoint.maxMotorTorque.Value;
                                revJoint.MotorEnabled = rubeJoint.enableMotor.Value;
                                revJoint.LowerLimit = rubeJoint.lowerLimit.Value;
                                revJoint.UpperLimit = rubeJoint.upperLimit.Value;
                                revJoint.LimitEnabled = rubeJoint.enableLimit.Value;
                                revJoint.CollideConnected = false;

                                joint = revJoint;
                                break;

                            case "wheel":
                                // TODO: There's a bug in this wheel joint. Don't have time to fix at the moment.

                                var wheelJoint = new WheelJoint(
                                    bodyA,
                                    bodyB,
                                    bodyB.Position,
                                    rubeJoint.localAxisA.ToVector2(),
                                    true
                                    );

                                wheelJoint.MotorSpeed = rubeJoint.motorSpeed.Value;
                                wheelJoint.MaxMotorTorque = rubeJoint.maxMotorTorque.Value;
                                wheelJoint.MotorEnabled = rubeJoint.enableMotor.Value;
                                wheelJoint.Frequency = rubeJoint.frequency;
                                wheelJoint.DampingRatio = rubeJoint.dampingRatio;

                                joint = wheelJoint;
                                break;

                            default:
                                throw new InvalidDataException();
                        }

                        // TODO: set all base joint properties here...

                        if (joint != null)
                        {
                            // add it!
                            world.Add(joint);
                        }
                    }
                }
            }
        }
    }
}
