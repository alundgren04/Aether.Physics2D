using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helio.Physics.Compatibility.MonoGame
{
    public interface IScreen
    {
        Vector2 ConvertWorldToScreen(Vector2 position);
        Vector2 ConvertScreenToWorld(int x, int y);
    }
}
