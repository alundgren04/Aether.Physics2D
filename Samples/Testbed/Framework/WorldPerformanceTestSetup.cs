
using System;
using System.Collections.Generic;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Loaders.RUBE;
using tainicom.Aether.Physics2D.Utilities;

namespace tainicom.Aether.Physics2D.Samples.Testbed.Framework
{
    public static class WorldPerformanceTestSetup
    {
        public static World CreateWorld(float worldSideLength = 2000, float metersPerBody = 50, BodyStructureType bodyType = BodyStructureType.TwelveFixtureStructure)
        {
            var world = new World();

            // enable multithreading
            world.ContactManager.VelocityConstraintsMultithreadThreshold = 256;
            world.ContactManager.PositionConstraintsMultithreadThreshold = 256;
            world.ContactManager.CollideMultithreadThreshold = 256;

            // no grav
            world.Gravity = Vector2.Zero;

            WorldPerformanceTestSetup.CreateBounds(world, worldSideLength);

            WorldPerformanceTestSetup.CreateBodies(world, worldSideLength, metersPerBody, bodyType);

            world.HibernationEnabled = true;

            return world;
        }


        private static void CreateBodies(World world, float worldSideLength, float metersPerBody, BodyStructureType bodyType)
        {
            // load the game world
            var tempWorld = new World();
            RubeLoader.Load("data/testbodies.json", tempWorld);

            // settings
            const float MAX_LINEAR_VELOCITY = 20.0f;
            const float MAX_ANGULAR_VELOCITY = 1.5F;

            // add the specific body
            //const int COMPLEX_BODY_INDEX = 2;
            //const int C_BODY_INDEX = 1;
            //const int BOX_BODY_INDEX = 0;
            var worldRadius = worldSideLength / 2f;
            var selectedBodyIndex = (int)bodyType; //BOX_BODY_INDEX;

            for (var x = metersPerBody; x <= worldSideLength - metersPerBody; x += metersPerBody)
            {
                for (var y = metersPerBody; y <= worldSideLength - metersPerBody; y += metersPerBody)
                {
                    var bodyDef = tempWorld.BodyList[selectedBodyIndex];
                    var body = bodyDef.DeepClone(world);

                    body.Position = new Vector2(x - worldRadius, y - worldRadius);
                    body.LinearVelocity = RandomGenerator.Vector2(MAX_LINEAR_VELOCITY);
                    body.AngularVelocity = RandomGenerator.Float(-MAX_ANGULAR_VELOCITY, MAX_ANGULAR_VELOCITY);
                }
            }
        }

        private static void CreateBounds(World world, float worldSideSize)
        {
            // create ring of edges
            var edgeVertices = new List<Vector2>();
            const float EDGE_SIZE = 10f;
            var edgesPerSide = (int)Math.Ceiling(worldSideSize / EDGE_SIZE);
            var edgeXOffset = (edgesPerSide * EDGE_SIZE) / -2.0f;
            var edgeYOffset = edgeXOffset;
            for (var i = 0; i < edgesPerSide; i++)
            {
                var horz1 = new Vector2(i * EDGE_SIZE + edgeXOffset, edgeYOffset);
                var horz2 = new Vector2((i + 1) * EDGE_SIZE + edgeXOffset, edgeYOffset);

                // create a body for the TOP edge
                var topBody = world.CreateBody(Vector2.Zero, 0, BodyType.Static);
                topBody.CreateFixture(new EdgeShape(horz1, horz2));

                // create a body for the BOTTOM edge
                var bottomBody = world.CreateBody(Vector2.Zero, 0, BodyType.Static);
                var bottomOffset = new Vector2(0, worldSideSize);
                bottomBody.CreateFixture(new EdgeShape(horz1 + bottomOffset, horz2 + bottomOffset));


                var vert1 = new Vector2(edgeXOffset, i * EDGE_SIZE + edgeYOffset);
                var vert2 = new Vector2(edgeXOffset, (i + 1) * EDGE_SIZE + edgeYOffset);

                // create a body for the LEFT edge
                var leftBody = world.CreateBody(Vector2.Zero, 0, BodyType.Static);
                leftBody.CreateFixture(new EdgeShape(vert1, vert2));

                // create a body for the RIGHT edge
                var rightBody = world.CreateBody(Vector2.Zero, 0, BodyType.Static);
                var rightOffset = new Vector2(worldSideSize, 0);
                rightBody.CreateFixture(new EdgeShape(vert1 + rightOffset, vert2 + rightOffset));

            };
        }

    }
}
