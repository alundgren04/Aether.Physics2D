

using Aether.Physics2D.Tests;
using tainicom.Aether.Physics2D.Common;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Diagnostics;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Hibernation;
using tainicom.Aether.Physics2D.Loaders.RUBE;
using tainicom.Aether.Physics2D.Samples.Testbed.Framework;
using tainicom.Aether.Physics2D.Utilities;

namespace tainicom.Aether.Physics2D.Samples.Testbed.Tests
{
    public class WorldPerformanceTest : Test
    {
        private Dictionary<Keys, int> WorldSideSizeOptions = new Dictionary<Keys, int>
        {
            { Keys.Q, 100 },
            { Keys.W, 2000 },
            { Keys.E, 5000 },
            { Keys.R, 10000 },
            { Keys.T, 25000 },
            { Keys.Y, 100000 }
        };

        private Dictionary<Keys, int> BodyTypeOptions = new Dictionary<Keys, int>
        {
            { Keys.U, 1 },  // single body, single fixture
            { Keys.I, 2 }, // single body, multiple fixture            
            { Keys.O, 3 }, // multiple body (joined by joints), multiple fixture 
            { Keys.P, 4 }, // random 33% mix of all 3!!!
        };

        private Dictionary<Keys, int> MetersPerBodyOptions = new Dictionary<Keys, int>
        {
            { Keys.Z, 25 },  
            { Keys.X, 50 },
            { Keys.C, 100 },
            { Keys.V, 300 }, 
        };

        private Dictionary<Keys, BodyStructureType> BodyStructureTypeOptions = new Dictionary<Keys, BodyStructureType>
        {
            { Keys.J, BodyStructureType.SingleFixtureBox },
            { Keys.K, BodyStructureType.TwelveFixtureStructure }
        };

        private bool EnableCoordinateRendering { get; set; }

        private float WorldSideSize { get; set; }
        //private float WorldRadius { get { return this.WorldSideSize / 2f; } }
        private BodyStructureType BodyStructureType = BodyStructureType.TwelveFixtureStructure;

        // NOTE: This should always be greater than the biggest test body, otherwise things 
        //       could overlap, which is a huge perf reduction.
        //       Square this to get the square-meters-per-body.
        private float MetersPerBody;

        private WorldPerformanceTest()
        {
            // default to smallest world size
            this.WorldSideSize = this.WorldSideSizeOptions[Keys.W]; //Y];

            // set to 100m per body
            this.MetersPerBody = this.MetersPerBodyOptions[Keys.X];
        }



        public override void Initialize()
        {
            this.World = WorldPerformanceTestSetup.CreateWorld(this.WorldSideSize); //new World();
            //this.World.HibernationEnabled = true;

            // enable multithreading
            //this.World.ContactManager.VelocityConstraintsMultithreadThreshold = 256;
            //this.World.ContactManager.PositionConstraintsMultithreadThreshold = 256;
            //this.World.ContactManager.CollideMultithreadThreshold = 256;

            //this.World.Gravity = Vector2.Zero;

            //this.CreateBounds();

            
            // sets up debug drawing
            base.Initialize();

            // automatically enable additional performance info
            this.DebugView.AppendFlags(Diagnostics.DebugViewFlags.PerformanceGraph);
            this.DebugView.AppendFlags(Diagnostics.DebugViewFlags.DebugPanel);
            this.DebugView.AppendFlags(Diagnostics.DebugViewFlags.AABB);
            this.DebugView.Enabled = true;

            // set zoom to show a meaningful part of the world
            //this.GameInstance.ViewZoom = 0.09f;
        }

        private bool IsControlPanelRenderEnabled { get; set; }

        public override void Update(GameSettings settings, float elapsedSeconds)
        {
            base.Update(settings, gameTime);

            const int LINE_HEIGHT = 15;
            TextLine += LINE_HEIGHT * 14; // skip down 14 lines, so we write below the performance info.

            DrawString("Press tilde (~) to toggle the control panel.");
            TextLine += LINE_HEIGHT;

            if (this.IsControlPanelRenderEnabled)
            {
                DrawString("Press Space to switch between single-core / multi-core solvers.");
                DrawString("Press 1-3 to set VelocityConstraints Threshold. (1-(0 - Always ON), 2-(256), 3-(int.MaxValue - Always OFF))");
                DrawString("Press 4-6 to set PositionConstraints Threshold. (4-(0 - Always ON), 5-(256), 6-(int.MaxValue - Always OFF))");
                DrawString("Press 7-9 to set Collide Threshold.             (7-(0 - Always ON), 8-(256), 9-(int.MaxValue - Always OFF))");

                TextLine += LINE_HEIGHT;

                var cMgr = World.ContactManager;
                var threshold = cMgr.VelocityConstraintsMultithreadThreshold;
                if (threshold == 0) DrawString("VelocityConstraintsMultithreadThreshold: 0");
                else if (threshold == 256) DrawString("VelocityConstraintsMultithreadThreshold: 256");
                else if (threshold == int.MaxValue) DrawString("VelocityConstraintsMultithreadThreshold: int.MaxValue");
                else DrawString("VelocityConstraintsMultithreadThreshold: " + threshold);
                threshold = cMgr.PositionConstraintsMultithreadThreshold;
                if (threshold == 0) DrawString("PositionConstraintsMultithreadThreshold: 0");
                else if (threshold == 256) DrawString("PositionConstraintsMultithreadThreshold: 256");
                else if (threshold == int.MaxValue) DrawString("PositionConstraintsMultithreadThreshold: int.MaxValue");
                else DrawString("PositionConstraintsMultithreadThreshold is Currently: " + threshold);
                threshold = cMgr.CollideMultithreadThreshold;
                if (threshold == 0) DrawString("CollideMultithreadThreshold: 0");
                else if (threshold == 256) DrawString("CollideMultithreadThreshold:  256");
                else if (threshold == int.MaxValue) DrawString("CollideMultithreadThreshold: int.MaxValue");
                else DrawString("CollideMultithreadThreshold is Currently: " + threshold);

                TextLine += LINE_HEIGHT;

                DrawString("[IsRunningSlowly = " + gameTime.IsRunningSlowly.ToString().ToUpper() + "]" + "      Zoom = " + Math.Round(this.GameInstance.ViewZoom, 2) );

                TextLine += LINE_HEIGHT;
                DrawString("Press Left Control to toggle debug rendering of game world: " + this.DebugView.Enabled);

                TextLine += LINE_HEIGHT;
                DrawString("Press Left Alt to toggle coordinate rendering: " + this.EnableCoordinateRendering);

                TextLine += LINE_HEIGHT;
                DrawString("Press Right Control to toggle debug rendering of hibernated world: " + this.DebugView.HasFlag(DebugViewFlags.HibernatedBodyAABBs) );

                // World size options
                TextLine += LINE_HEIGHT;
                var worldSizeOptions = string.Empty;
                foreach (var key in this.WorldSideSizeOptions.Keys)
                {
                    worldSizeOptions += string.Format("{0} = {1}m, ", key.ToString(), this.WorldSideSizeOptions[key]);
                }
                DrawString("Current world size (width & height): " + WorldSideSize.ToString() + "m");
                DrawString(string.Format("Press one of these keys to change it: ({0})", worldSizeOptions));

                // Meters-per-body options 
                TextLine += LINE_HEIGHT;
                var metersPerBodyOptions = string.Empty;
                foreach (var key in this.MetersPerBodyOptions.Keys)
                {
                    metersPerBodyOptions += string.Format("{0} = {1}m, ", key.ToString(), this.MetersPerBodyOptions[key]);
                }
                DrawString("Current meters-per-body: " + MetersPerBody.ToString() + "m");
                DrawString(string.Format("Press one of these keys to change it: ({0})", metersPerBodyOptions));

                // Body structure type options 
                TextLine += LINE_HEIGHT;
                var bodyStructureTypeOptions = string.Empty;
                foreach (var key in this.BodyStructureTypeOptions.Keys)
                {
                    bodyStructureTypeOptions += string.Format("{0} = {1}, ", key.ToString(), this.BodyStructureTypeOptions[key].ToString());
                }
                DrawString("Current body structure type: " + this.BodyStructureType.ToString());
                DrawString(string.Format("Press one of these keys to change it: ({0})", bodyStructureTypeOptions));

                // Hibernation toggling 
                TextLine += LINE_HEIGHT;
                DrawString("Hibernation enabled: " + this.World.HibernationEnabled.ToString().ToUpper() + ". Press 'h' to toggle. Right-click to create/position an 'active area.'");

            }
        }

        private Vector2 CurrentMouseScreenPosition = Vector2.Zero;
        private Vector2 CurrentMouseWorldPosition = Vector2.Zero;
        public override void Mouse(MouseState state, MouseState oldState)
        {
            this.CurrentMouseScreenPosition = new Vector2(state.X, state.Y);
            Vector2 position = GameInstance.ConvertScreenToWorld(state.X, state.Y);
            this.CurrentMouseWorldPosition = position;

            base.Mouse(state, oldState);
        }

        public override void DrawDebugView(float elapsedSeconds, ref Matrix projection, ref Matrix view)
        {
            base.DrawDebugView(gameTime, ref projection, ref view);

            #region render game center and axii

            if (this.EnableCoordinateRendering)
            {
                this.DebugView.BeginCustomDraw(projection, view);
                
                // draw X
                var xAxisMark = new Vector2(1, 0);
                this.DebugView.DrawSegment(Vector2.Zero, xAxisMark, Color.Green);
                var xAxisMarkLabel = GameInstance.ConvertWorldToScreen(xAxisMark);
                this.DebugView.DrawString((int)xAxisMarkLabel.X, (int)xAxisMarkLabel.Y, "x = 1");

                // draw Y
                var yAxisMark = new Vector2(0, 1);
                this.DebugView.DrawSegment(Vector2.Zero, yAxisMark, Color.Red);
                var yAxisMarkLabel = GameInstance.ConvertWorldToScreen(yAxisMark);
                this.DebugView.DrawString((int)yAxisMarkLabel.X, (int)yAxisMarkLabel.Y, "y = 1");

                // mouse poso
                this.DebugView.DrawString((int)this.CurrentMouseScreenPosition.X, (int)this.CurrentMouseScreenPosition.Y - 15, string.Format( "{0}x {1}y", Math.Round( this.CurrentMouseWorldPosition.X, 0), Math.Round( this.CurrentMouseWorldPosition.Y, 0)));

                this.DebugView.EndCustomDraw();
            }
            #endregion
        }

        public override void Keyboard(KeyboardManager keyboardManager)
        {
            base.Keyboard(keyboardManager);

            var cMgr = World.ContactManager;        

            if (keyboardManager.IsNewKeyPress(Keys.D1))
                cMgr.VelocityConstraintsMultithreadThreshold = 0;
            if (keyboardManager.IsNewKeyPress(Keys.D2))
                cMgr.VelocityConstraintsMultithreadThreshold = 256;
            if (keyboardManager.IsNewKeyPress(Keys.D3))
                cMgr.VelocityConstraintsMultithreadThreshold = int.MaxValue;

            if (keyboardManager.IsNewKeyPress(Keys.D4))
                cMgr.PositionConstraintsMultithreadThreshold = 0;
            if (keyboardManager.IsNewKeyPress(Keys.D5))
                cMgr.PositionConstraintsMultithreadThreshold = 256;
            if (keyboardManager.IsNewKeyPress(Keys.D6))
                cMgr.PositionConstraintsMultithreadThreshold = int.MaxValue;
            
            if (keyboardManager.IsNewKeyPress(Keys.D7))
                cMgr.CollideMultithreadThreshold = 0;
            if (keyboardManager.IsNewKeyPress(Keys.D8))
                cMgr.CollideMultithreadThreshold = 256;
            if (keyboardManager.IsNewKeyPress(Keys.D9))
                cMgr.CollideMultithreadThreshold = int.MaxValue;

            if (keyboardManager.IsNewKeyPress(Keys.Space))
            {
                if (cMgr.VelocityConstraintsMultithreadThreshold == int.MaxValue)
                    cMgr.VelocityConstraintsMultithreadThreshold = 0;
                else
                    cMgr.VelocityConstraintsMultithreadThreshold = int.MaxValue;
                cMgr.PositionConstraintsMultithreadThreshold = cMgr.VelocityConstraintsMultithreadThreshold;
                cMgr.CollideMultithreadThreshold = cMgr.VelocityConstraintsMultithreadThreshold;

            }
            
            // World side size.
            foreach( var key in this.WorldSideSizeOptions.Keys)
            {
                if( keyboardManager.IsNewKeyPress(key) )
                {
                    var pressedSize = this.WorldSideSizeOptions[key];
                    if( pressedSize != this.WorldSideSize )
                    {
                        this.WorldSideSize = pressedSize;
                        this.Initialize();
                    }
                    break;
                }
            }

            // Meters per body.
            foreach (var key in this.MetersPerBodyOptions.Keys)
            {
                if (keyboardManager.IsNewKeyPress(key))
                {
                    var newMetersPerBody = this.MetersPerBodyOptions[key];
                    if (newMetersPerBody != this.MetersPerBody)
                    {
                        this.MetersPerBody = newMetersPerBody;
                        this.Initialize();
                    }
                    break;
                }
            }

            // Body structure type.
            foreach (var key in this.BodyStructureTypeOptions.Keys)
            {
                if (keyboardManager.IsNewKeyPress(key))
                {
                    var newBodyStructureType = this.BodyStructureTypeOptions[key];
                    if (newBodyStructureType != this.BodyStructureType)
                    {
                        this.BodyStructureType = newBodyStructureType;
                        this.Initialize();
                    }
                    break;
                }
            }

            if( keyboardManager.IsNewKeyPress(Keys.LeftAlt))
            {
                this.EnableCoordinateRendering = !this.EnableCoordinateRendering;
            }

            if( keyboardManager.IsNewKeyPress(Keys.RightControl) )
            {
                this.DebugView.ToggleFlag(DebugViewFlags.HibernatedBodyAABBs);
            }

            if (keyboardManager.IsNewKeyPress(Keys.LeftControl))
            {
                this.DebugView.Enabled = !this.DebugView.Enabled;
            }

            if( keyboardManager.IsNewKeyPress(Keys.OemTilde))
            {
                this.IsControlPanelRenderEnabled = !this.IsControlPanelRenderEnabled;
            }
        }

        public static Test Create()
        {
            return new WorldPerformanceTest();
        }
    }


}