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
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Diagnostics;
using tainicom.Aether.Physics2D.Samples.Testbed.Framework;
using tainicom.Aether.Physics2D.Samples.Testbed.Tests;
using tainicom.Aether.Physics2D.Utilities;


#region MonoGame Support. TO DO: Abstract this all away into Helio.Physics.Compatability.Monogame
using Helio.Physics.Compatibility.MonoGame;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GraphicsDeviceManager = Microsoft.Xna.Framework.GraphicsDeviceManager;
using Game = Microsoft.Xna.Framework.Game;
using PlayerIndex = Microsoft.Xna.Framework.PlayerIndex;
using System.Drawing;
#endregion

namespace tainicom.Aether.Physics2D.Samples.Testbed
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game, IScreen
    {
        private TestEntry _entry;
        private GraphicsDeviceManager _graphics;
        private KeyboardManager _keyboardManager = new KeyboardManager();
        private Vector2 _lower;
        private GamePadState _oldGamePad;
        private MouseState _oldMouseState;
        public Matrix Projection;
        private GameSettings _settings = new GameSettings();
        private Test _test;
        private int _testCount;
        private int _testIndex;
        private int _testSelection;
        private Vector2 _upper;
        public Matrix View;
        private Vector2 _viewCenter;

        private float _viewZoom;

        private PerformanceTree PerformanceTree { get; set; }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.GraphicsProfile = GraphicsProfile.Reach;
            _graphics.PreferMultiSampling = true;
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            IsFixedTimeStep = true;
            _graphics.SynchronizeWithVerticalRetrace = true;
        }

        public float ViewZoom
        {
            get { return _viewZoom; }
            set
            {
                _viewZoom = value;
                UpdateProjection();
            }
        }

        public Vector2 ViewCenter
        {
            get { return _viewCenter; }
            set
            {
                _viewCenter = value;
                UpdateView();
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // init the perf tree
            this.PerformanceTree = new PerformanceTree();

            //Set window defaults. Parent game can override in constructor
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += WindowClientSizeChanged;

            //Default projection and view
            ResetCamera();

            _testCount = 0;
            while (TestEntries.TestList[_testCount].CreateTest != null)
            {
                ++_testCount;
            }

            _testIndex = MathUtils.Clamp(_testIndex, 0, _testCount - 1);
            _testSelection = _testIndex;
            StartTest(_testIndex);
        }


        private void StartTest(int index)
        {
            // save previous flags
            DebugViewFlags flags = (DebugViewFlags)0;
            if (_test != null)
                flags = _test.DebugView.Flags;

            _entry = TestEntries.TestList[index];
            _test = _entry.CreateTest();
            _test.GameInstance = this;
            _test.Initialize();

            // re-enable previous flags
            _test.DebugView.Flags |= flags;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _keyboardManager._oldKeyboardState = Keyboard.GetState();
            _oldMouseState = Mouse.GetState();
            _oldGamePad = GamePad.GetState(PlayerIndex.One);
        }

        protected override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            this.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected void Update(float elapsedSeconds)
        {
            // update the update FPS
            this.UpdateFrequency.Update();

            // begin "update" region
            this.PerformanceTree.StartRegion("Update");

            // clear graphics here because some tests already draw during update
            GraphicsDevice.Clear(Color.Black.ToMonoGame());

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            _keyboardManager._newKeyboardState = Keyboard.GetState();
            GamePadState newGamePad = GamePad.GetState(PlayerIndex.One);
            MouseState newMouseState = Mouse.GetState();

            //if (_keyboardManager.IsKeyDown(Keys.Z)) // Press 'z' to zoom out.
            //    ViewZoom = Math.Min((float)Math.Pow(Math.E, -0.05f) * ViewZoom, 20.0f);
            //else if (_keyboardManager.IsKeyDown(Keys.X)) // Press 'x' to zoom in.
            //    ViewZoom = Math.Max((float)Math.Pow(Math.E, +0.05f) * ViewZoom, 0.02f);
            const float MIN_ZOOM = .002F;
            const float MAX_ZOOM = 20.0F;
            if (_keyboardManager.IsKeyDown(Keys.Subtract)) // Press '-' to zoom out.
                ViewZoom = Math.Min((float)Math.Pow(Math.E, -0.05f) * ViewZoom, MAX_ZOOM);
            else if (_keyboardManager.IsKeyDown(Keys.Add)) // Press 'x' to zoom in.
                ViewZoom = Math.Max((float)Math.Pow(Math.E, +0.05f) * ViewZoom, MIN_ZOOM); //0.02f);
            else if (newMouseState.ScrollWheelValue != _oldMouseState.ScrollWheelValue) // Mouse Wheel to Zoom.
            {
                var wheelDelta = (newMouseState.ScrollWheelValue - _oldMouseState.ScrollWheelValue) / 120f;
                var zoomFactor = (float)Math.Pow(Math.E, 0.05f * wheelDelta);
                ViewZoom = Math.Min(Math.Max(zoomFactor * ViewZoom, MIN_ZOOM), MAX_ZOOM);
            }
            else if (_keyboardManager.IsNewKeyPress(Keys.R)) // Press 'r' to reset.
                Restart();
            else if (_keyboardManager.IsNewKeyPress(Keys.P) || newGamePad.IsButtonDown(Buttons.Start) && _oldGamePad.IsButtonUp(Buttons.Start)) // Press I to prev test.
                _settings.Pause = !_settings.Pause;
            else if (_keyboardManager.IsNewKeyPress(Keys.Left) || newGamePad.IsButtonDown(Buttons.LeftShoulder) && _oldGamePad.IsButtonUp(Buttons.LeftShoulder))
            {
                --_testSelection;
                if (_testSelection < 0)
                    _testSelection = _testCount - 1;
            }
            else if (_keyboardManager.IsNewKeyPress(Keys.Right) || newGamePad.IsButtonDown(Buttons.RightShoulder) && _oldGamePad.IsButtonUp(Buttons.RightShoulder)) // Press O to next test.
            {
                ++_testSelection;
                if (_testSelection == _testCount)
                    _testSelection = 0;
            }

            var viewCenterMoveVelocityMultiplier = 1.0f / this.ViewZoom;
            if (_keyboardManager.IsKeyDown(Keys.NumPad4)) // Press left to pan left.
                ViewCenter = new Vector2(ViewCenter.X - 0.5f * viewCenterMoveVelocityMultiplier, ViewCenter.Y);
            else if (_keyboardManager.IsKeyDown(Keys.NumPad6)) // Press right to pan right.
                ViewCenter = new Vector2(ViewCenter.X + 0.5f * viewCenterMoveVelocityMultiplier, ViewCenter.Y);
            if (_keyboardManager.IsKeyDown(Keys.NumPad2)) // Press down to pan down.
                ViewCenter = new Vector2(ViewCenter.X, ViewCenter.Y - 0.5f * viewCenterMoveVelocityMultiplier);
            else if (_keyboardManager.IsKeyDown(Keys.NumPad8)) // Press up to pan up.
                ViewCenter = new Vector2(ViewCenter.X, ViewCenter.Y + 0.5f * viewCenterMoveVelocityMultiplier);
            if (_keyboardManager.IsNewKeyPress(Keys.Home)) // Press home to reset the view.
                ResetCamera();
            else if (_keyboardManager.IsNewKeyPress(Keys.F1))
                EnableOrDisableFlag(DebugViewFlags.Shape);
            else if (_keyboardManager.IsNewKeyPress(Keys.F2))
                EnableOrDisableFlag(DebugViewFlags.DebugPanel);
            else if (_keyboardManager.IsNewKeyPress(Keys.F3))
                EnableOrDisableFlag(DebugViewFlags.PerformanceGraph);
            else if (_keyboardManager.IsNewKeyPress(Keys.F4))
                EnableOrDisableFlag(DebugViewFlags.AABB);
            else if (_keyboardManager.IsNewKeyPress(Keys.F5))
                EnableOrDisableFlag(DebugViewFlags.CenterOfMass);
            else if (_keyboardManager.IsNewKeyPress(Keys.F6))
                EnableOrDisableFlag(DebugViewFlags.Joint);
            else if (_keyboardManager.IsNewKeyPress(Keys.F7))
            {
                EnableOrDisableFlag(DebugViewFlags.ContactPoints);
                EnableOrDisableFlag(DebugViewFlags.ContactNormals);
            }
            else if (_keyboardManager.IsNewKeyPress(Keys.F8))
                EnableOrDisableFlag(DebugViewFlags.PolygonPoints);
            else if (_keyboardManager.IsNewKeyPress(Keys.F9))
                EnableOrDisableFlag(DebugViewFlags.PolygonPoints);
            else
            {
                if (_test != null)
                    _test.Keyboard(_keyboardManager);
            }

            if (_test != null)
                _test.Mouse(newMouseState, _oldMouseState);

            if (_test != null && newGamePad.IsConnected)
                _test.Gamepad(newGamePad, _oldGamePad);

            _keyboardManager._oldKeyboardState = _keyboardManager._newKeyboardState;
            _oldMouseState = newMouseState;
            _oldGamePad = newGamePad;

            if (_test != null)
            {
                _test.TextLine = 30;
                _test.Update(_settings, elapsedSeconds);
            }

            _test.DebugView.UpdatePerformanceGraph(_test.World.UpdateTime);

            this.PerformanceTree.EndRegion();
        }

        private void EnableOrDisableFlag(DebugViewFlags flag)
        {
            if ((_test.DebugView.Flags & flag) == flag)
                _test.DebugView.RemoveFlags(flag);
            else
                _test.DebugView.AppendFlags(flag);
        }

        private FrequencyTracker DrawFrequency = new FrequencyTracker();
        private FrequencyTracker UpdateFrequency = new FrequencyTracker();

        /// <summary>
        /// TODO: abstract this and remove MG specificicity.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            this.Draw((float)gameTime.ElapsedGameTime.TotalSeconds);

            base.Draw(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected void Draw(float elapsedSeconds)
        {
            // mark the end of the previous loop and start the next
            this.PerformanceTree.End();
            this.PerformanceTree.Start();

            this.PerformanceTree.StartRegion("Draw");

            this.DrawFrequency.Update();

            _test.DrawTitle(50, 15, _entry.Name);

            // show the "update" FPS
            var performanceLinePos = new Vector2(800, 200);
            _test.DebugView.DrawString(performanceLinePos, "Game Loops Per Sec (update): " + Math.Round(this.UpdateFrequency.AverageOccurrencesPerSecond, 1));

            // show the "draw" FPS
            performanceLinePos += new Vector2(0, 15);
            _test.DebugView.DrawString(performanceLinePos, "Game Loops Per Sec (draw): " + Math.Round(this.DrawFrequency.AverageOccurrencesPerSecond, 1));

            // draw the performance tree
            foreach (var perfLine in this.PerformanceTree.PerformanceDetailLines)
            {
                performanceLinePos += new Vector2(0, 15);
                _test.DebugView.DrawString(performanceLinePos, perfLine);
            }

            if (_testSelection != _testIndex)
            {
                _testIndex = _testSelection;
                StartTest(_testIndex);
                ResetCamera();
            }

            _test.DrawDebugView(elapsedSeconds, ref Projection, ref View);

            //base.Draw(gameTime);

            this.PerformanceTree.EndRegion();
        }

        private void ResetCamera()
        {
            ViewZoom = 0.8f;
            ViewCenter = new Vector2(0.0f, 20.0f);
        }

        private void UpdateProjection()
        {
            _lower = -new Vector2(25.0f * GraphicsDevice.Viewport.AspectRatio, 25.0f) / ViewZoom;
            _upper =  new Vector2(25.0f * GraphicsDevice.Viewport.AspectRatio, 25.0f) / ViewZoom;

            // L/R/B/T
            Projection = Matrix.CreateOrthographicOffCenter(_lower.X, _upper.X, _lower.Y, _upper.Y, 0f, 2f);
        }

        private void UpdateView()
        {
            View = Matrix.CreateLookAt(new Vector3(ViewCenter, 1), new Vector3(ViewCenter, 0), Vector3.Up);
        }

        public Vector2 ConvertWorldToScreen(Vector2 position)
        {
            Microsoft.Xna.Framework.Vector3 temp = GraphicsDevice.Viewport.Project(
                new Vector3(position, 0).ToMonoGame(), 
                Projection.ToMonoGame(), 
                View.ToMonoGame(), 
                Matrix.Identity.ToMonoGame());

            return new Vector2(temp.X, temp.Y);
        }

        public Vector2 ConvertScreenToWorld(int x, int y)
        {
            Microsoft.Xna.Framework.Vector3 temp = GraphicsDevice.Viewport.Unproject(
                new Vector3(x, y, 0).ToMonoGame(), 
                Projection.ToMonoGame(), 
                View.ToMonoGame(), 
                Matrix.Identity.ToMonoGame());

            return new Vector2(temp.X, temp.Y);
        }

        private void Restart()
        {
            StartTest(_testIndex);
        }

        private void WindowClientSizeChanged(object sender, EventArgs e)
        {
            if (Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            }

            //We want to keep aspec ratio. Recalcuate the projection matrix.
            UpdateProjection();
        }
    }
}