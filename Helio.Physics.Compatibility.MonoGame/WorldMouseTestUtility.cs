using tainicom.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Joints;
using Microsoft.Xna.Framework.Input;

namespace Helio.Physics.Compatibility.MonoGame
{
    public class WorldMouseTestUtility
    {
        private FixedMouseJoint _fixedMouseJoint;
        private Vector2 MouseWorldPosition { get; set; }
        private World World;
        private IScreen Screen { get; set; }
        private MouseState _oldMouseState;

        public WorldMouseTestUtility(World world, IScreen screen)
        {
            this.Screen = screen;
            this.World = world;
            this.World.JointRemoved += JointRemoved;
        }

        private void JointRemoved(World sender, Joint joint)
        {
            if (_fixedMouseJoint == joint)
                _fixedMouseJoint = null;
        }

        public virtual void Update(MouseState newState)
        {
            this.MouseWorldPosition = this.Screen.ConvertScreenToWorld(newState.X, newState.Y);

            if (newState.LeftButton == ButtonState.Released && _oldMouseState.LeftButton == ButtonState.Pressed)
                MouseUp();
            else if (newState.LeftButton == ButtonState.Pressed && _oldMouseState.LeftButton == ButtonState.Released)
                MouseDown(this.MouseWorldPosition);

            MouseMove(this.MouseWorldPosition);

            this._oldMouseState = newState;
        }

        private void MouseDown(Vector2 p)
        {
            if (_fixedMouseJoint != null)
                return;

            Fixture fixture = World.TestPoint(p);

            if (fixture != null)
            {
                Body body = fixture.Body;
                _fixedMouseJoint = new FixedMouseJoint(body, p);
                _fixedMouseJoint.MaxForce = 1000.0f * body.Mass;
                World.Add(_fixedMouseJoint);
                body.Awake = true;
            }
        }

        private void MouseUp()
        {
            if (_fixedMouseJoint != null)
            {
                World.Remove(_fixedMouseJoint);
                _fixedMouseJoint = null;
            }
        }

        private void MouseMove(Vector2 p)
        {
            if (_fixedMouseJoint != null)
                _fixedMouseJoint.WorldAnchorB = p;
        }

    }
}
