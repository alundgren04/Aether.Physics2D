using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Utilities
{
    public static class RandomGenerator
    {
        private static System.Random random = new System.Random();

        public static float Float(float minimum, float maximum)
        {
            return (float)(random.NextDouble() * (maximum - minimum) + minimum);
        }

        public static Vector2 Vector2(float maxLength)
        {
            // get a random X
            float randomX = RandomGenerator.Float(-1, 1); 
            float randomY = RandomGenerator.Float(-1, 1);

            // create the random vector
            var randomVector = new Vector2(
                randomX,
                randomY
                );

            // scale it down to length 1
            randomVector.Normalize();

            // get the final length
            var length = RandomGenerator.Float(-maxLength, maxLength);

            // multiply it by the desired length
            randomVector *= length;

            // all set. return it.
            return randomVector;
        }
    }
}
