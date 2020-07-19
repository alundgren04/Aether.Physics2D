using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Samples.Testbed.Framework;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Loaders.RUBE;

namespace tainicom.Aether.Physics2D.Samples.Testbed.Tests
{
    public class RubeLoaderTest : Test
    {
        private RubeLoaderTest()
        {
            RubeLoader.Load("Data/rubegoldberg.json", this.World);
        }

        public static Test Create()
        {
            return new RubeLoaderTest();
        }
    }
}