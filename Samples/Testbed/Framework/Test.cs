/* Original source Farseer Physics Engine:
 * Copyright (c) 2014 Ian Qvist, http://farseerphysics.codeplex.com
 * Microsoft Permissive License (Ms-PL) v1.1
 */

/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
* 
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.IO;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Diagnostics;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using tainicom.Aether.Physics2D.Dynamics.Joints;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics.Hibernation;
using System.Linq;
using tainicom.Aether.Physics2D.Utilities;

#region MonoGame Support. TO DO: Abstract this all away into Helio.Physics.Compatability.Monogame
using Helio.Physics.Compatibility.MonoGame;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GraphicsDeviceManager = Microsoft.Xna.Framework.GraphicsDeviceManager;
using Game = Microsoft.Xna.Framework.Game;
using PlayerIndex = Microsoft.Xna.Framework.PlayerIndex;
using Color = Microsoft.Xna.Framework.Color;
#endregion

namespace tainicom.Aether.Physics2D.Samples.Testbed.Framework
{
    public class Test
    {
        internal DebugView DebugView;
        internal int StepCount;
        internal World World;
        internal int TextLine;
        WorldMouseTestUtility WorldMouseTestUtility;
        private IndependentActiveArea MouseActiveArea = null;

        protected Test()
        {
            World = new World(new Vector2(0.0f, -10.0f));

            TextLine = 30;

            World.JointRemoved += JointRemoved;
            World.ContactManager.PreSolve += PreSolve;
            World.ContactManager.PostSolve += PostSolve;

            StepCount = 0;
        }

        public Game1 GameInstance { protected get; set; }

        public virtual void Initialize()
        {
            DebugView = new DebugView(World);
            DebugView.LoadContent(GameInstance.GraphicsDevice, GameInstance.Content);

            this.WorldMouseTestUtility = new WorldMouseTestUtility(World, GameInstance);
        }

        protected virtual void JointRemoved(World sender, Joint joint)
        {
            //if (_fixedMouseJoint == joint)
            //    _fixedMouseJoint = null;
        }

        public void DrawTitle(int x, int y, string title)
        {
            DebugView.DrawString(x, y, title);
        }

        public virtual void DrawDebugView(float elapsedSeconds, ref Matrix projection, ref Matrix view)
        {
            if (this.World.HibernationEnabled)
            {
                this.DebugView.BeginCustomDraw(projection, view);

                // show the user where the independent active area would be moved provided right mouse button is clicked
                Color independentActiveAreaColor = new Color(0.30f, 0.10f, 0.10f);
                AABB newActiveArea = new AABB(this.MouseWorldPosition, this.IndependentActiveAreaRadius * 2, this.IndependentActiveAreaRadius * 2);
                this.DebugView.DrawAABB(ref newActiveArea, independentActiveAreaColor);

                bool renderActiveAreaBodyCount = false; // NOTE: flip this to true to see the number of bodies currently within each active area for debug purposes.
                if (renderActiveAreaBodyCount)
                {
                    foreach (var activeArea in this.World.HibernationManager.ActiveAreas)
                    {
                        // render number of bodies within each active area
                        Vector2 position = new Vector2(activeArea.AABB.LowerBound.X, activeArea.AABB.UpperBound.Y);
                        position = GameInstance.ConvertWorldToScreen(position);
                        DebugView.DrawString((int)position.X, (int)position.Y - 5, "Contains " + activeArea.AreaBodies.Count().ToString());
                    }
                }

                this.DebugView.EndCustomDraw();
            }

            bool renderBodyIds = false; // NOTE: flip this to true to see body IDs for debug purposes.
            if (renderBodyIds)
            {
                foreach (var body in this.World.BodyList)
                {
                    // render body ID
                    var position = GameInstance.ConvertWorldToScreen(body.Position);
                    DebugView.DrawString((int)position.X - 5, (int)position.Y - 5, "Id " + body.Id.ToString());
                }
            }

            DebugView.RenderDebugData(ref projection, ref view);
        }

        public virtual void Update(GameSettings settings, float elapsedSeconds)
        {
            float timeStep = Math.Min(elapsedSeconds, (1f / 30f));

            if (settings.Pause)
            {
                if (settings.SingleStep)
                    settings.SingleStep = false;
                else
                    timeStep = 0.0f;

                DrawString("****PAUSED****");
            }
            else
                World.Step(timeStep);

            if (timeStep > 0.0f)
                ++StepCount;
        }

        public virtual void Keyboard(KeyboardManager keyboardManager)
        {
            if (keyboardManager.IsNewKeyPress(Keys.F11))
            {
                using (Stream stream = new FileStream("out.xml", FileMode.Create))
                {
                    WorldSerializer.Serialize(World, stream);
                }
            }

            if (keyboardManager.IsNewKeyPress(Keys.F12))
            {
                using (Stream stream = new FileStream("out.xml", FileMode.Open))
                {
                    World = WorldSerializer.Deserialize(stream);
                }
                Initialize();
            }

            if (keyboardManager.IsNewKeyPress(Keys.H))
            {
                this.World.HibernationEnabled = !this.World.HibernationEnabled;
            }
        }

        public virtual void Gamepad(GamePadState state, GamePadState oldState)
        {
        }

        public Vector2 MouseWorldPosition { get; private set; }
        public float IndependentActiveAreaRadius = 50f;
        public virtual void Mouse(MouseState state, MouseState oldState)
        {
            // apply world mouse updating
            this.WorldMouseTestUtility.Update(state);


            this.MouseWorldPosition = GameInstance.ConvertScreenToWorld(state.X, state.Y);

            //if (state.LeftButton == ButtonState.Released && oldState.LeftButton == ButtonState.Pressed)
            //    MouseUp();
            //else if (state.LeftButton == ButtonState.Pressed && oldState.LeftButton == ButtonState.Released)
            //    MouseDown(this.MouseWorldPosition);

            if (this.World.HibernationEnabled)
            {
                if (state.RightButton == ButtonState.Pressed)
                {
                    // get first independent active area
                    //var activeArea = this.World.HibernationManager.ActiveAreas.FirstOrDefault(aa => aa.AreaType == ActiveAreaType.Independent) as IndependentActiveArea;

                    if (this.MouseActiveArea == null)
                    {
                        // init and add
                        this.MouseActiveArea = new IndependentActiveArea();
                        this.MouseActiveArea.SetRadius(IndependentActiveAreaRadius);
                        this.World.HibernationManager.ActiveAreas.Add(this.MouseActiveArea);
                    }

                    // set it to match current click position
                    this.MouseActiveArea.SetPosition(this.MouseWorldPosition);
                } 
            }

            //MouseMove(this.MouseWorldPosition);
        }

        protected virtual void PreSolve(Contact contact, ref Manifold oldManifold)
        {
        }

        protected virtual void PostSolve(Contact contact, ContactVelocityConstraint impulse)
        {
        }

#if WINDOWS
        protected Vertices LoadDataFile(string filename)
        {
            string[] lines = File.ReadAllLines(filename);

            Vertices vertices = new Vertices(lines.Length);

            foreach (string line in lines)
            {
                string[] split = line.Split(' ');
                vertices.Add(new Vector2(float.Parse(split[0]), float.Parse(split[1])));
            }

            return vertices;
        }
#endif

        protected void DrawString(string text)
        {
            DebugView.DrawString(50, TextLine, text);
            TextLine += 15;
        }
    }
}