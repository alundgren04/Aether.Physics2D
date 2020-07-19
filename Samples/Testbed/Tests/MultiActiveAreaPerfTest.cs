

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
    public class MultiActiveAreaPerfTest : Test
    {
        private MultiActiveAreaPerfTest()
        {

        }



        public override void Initialize()
        {
            const int WorldSideSize = 2000;
            this.World = WorldPerformanceTestSetup.CreateWorld(WorldSideSize); // 2km

            // add an interest area ever X meters
            const int AAradius = 100;
            const int AAmargin = 250;
            for( var x = -WorldSideSize/2 + AAmargin; x < WorldSideSize/2 - AAmargin/2; x += AAmargin)
            {
                for (var y = -WorldSideSize / 2 + AAmargin; y < WorldSideSize / 2 - AAmargin/2; y += AAmargin)
                {
                    // init and add
                    var activeArea = new IndependentActiveArea();
                    activeArea.SetPosition(new Vector2(x, y));
                    activeArea.SetRadius(AAradius);
                    this.World.HibernationManager.ActiveAreas.Add(activeArea);
                }
            }


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
                DrawString("Press Right Control to toggle debug rendering of hibernated world: " + this.DebugView.HasFlag(DebugViewFlags.HibernatedBodyAABBs) );

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
            return new MultiActiveAreaPerfTest();
        }
    }


}