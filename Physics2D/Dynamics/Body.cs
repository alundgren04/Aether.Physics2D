// Copyright (c) 2017 Kastellanos Nikolaos

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
using System.Collections.Generic;
using tainicom.Aether.Physics2D.Diagnostics;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Common.Maths;
using tainicom.Aether.Physics2D.Common.PhysicsLogic;
using tainicom.Aether.Physics2D.Controllers;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using tainicom.Aether.Physics2D.Dynamics.Joints;
//using tainicom.Aether.Physics2D.Common;

namespace tainicom.Aether.Physics2D.Dynamics
{
    public partial class Body
    {
        public int Id;

        private float _angularDamping;
        private BodyType _bodyType;
        private float _inertia;
        private float _linearDamping;
        private float _mass;
        private bool _sleepingAllowed;
        private bool _awake;
        private bool _fixedRotation;

        internal bool _enabled;
        internal float _angularVelocity;
        internal Vector2 _linearVelocity;
        internal Vector2 _force;
        internal float _invI;
        internal float _invMass;
        internal float _sleepTime;
        internal Sweep _sweep; // the swept motion for CCD
        internal float _torque;
        internal World _world;
        internal Transform _xf; // the body origin transform
        internal bool _island;
        internal int _lock;
        internal int _lockOrder;

        private int _broadphaseProxyId = -1;
        public int BroadphaseProxyId
        {
            get
            {
                return this._broadphaseProxyId;
            }
        }

        public ControllerFilter ControllerFilter = new ControllerFilter(ControllerCategory.All);

        public bool HasContacts { get { return ContactList != null; } }

        public Body()
        {
            FixtureList = new List<Fixture>();

            _enabled = true;
            _awake = true;
            _sleepingAllowed = true;
            _xf.Rotation = Complex.One;

            BodyType = BodyType.Static;

            //this.CreateProxy();
        }

        public World World { get {return _world; } }
        
        public int IslandIndex { get; set; }

        /// <summary>
        /// Set the user data. Use this to store your application specific data.
        /// </summary>
        /// <value>The user data.</value>
        public object Tag;

        /// <summary>
        /// Gets the total number revolutions the body has made.
        /// </summary>
        /// <value>The revolutions.</value>
        public float Revolutions
        {
            get { return Rotation / (float)Math.PI; }
        }

        /// <summary>
        /// Gets or sets the body type.
        /// Warning: This property is readonly during callbacks.
        /// </summary>
        /// <value>The type of body.</value>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public BodyType BodyType
        {
            get { return _bodyType; }
            set
            {
                if (World != null && World.IsLocked)
                    throw new InvalidOperationException("The World is locked.");

                if (_bodyType == value)
                    return;

                _bodyType = value;

                ResetMassData();

                if (_bodyType == BodyType.Static)
                {
                    _linearVelocity = Vector2.Zero;
                    _angularVelocity = 0.0f;
                    _sweep.A0 = _sweep.A;
                    _sweep.C0 = _sweep.C;
                    //SynchronizeFixtures();
                    Synchronize();
                }

                Awake = true;

                _force = Vector2.Zero;
                _torque = 0.0f;

                // Delete the attached contacts.
                ContactEdge ce = ContactList;
                while (ce != null)
                {
                    ContactEdge ce0 = ce;
                    ce = ce.Next;
                    World.ContactManager.Destroy(ce0.Contact);
                }
                ContactList = null;
                
                if (World != null)
                {
                    // Touch the proxies so that new contacts will be created (when appropriate)
                    _world.ContactManager.BroadPhase.TouchProxy(_broadphaseProxyId);
                    //IBroadPhase broadPhase = World.ContactManager.BroadPhase;
                    //foreach (Fixture fixture in FixtureList)
                    //    fixture.TouchProxies(broadPhase);
                }
            }
        }

        /// <summary>
        /// Get or sets the linear velocity of the center of mass.
        /// </summary>
        /// <value>The linear velocity.</value>
        public Vector2 LinearVelocity
        {
            set
            {
                Debug.Assert(!float.IsNaN(value.X) && !float.IsNaN(value.Y));

                if (_bodyType == BodyType.Static)
                    return;

                if (Vector2.Dot(value, value) > 0.0f)
                    Awake = true;

                _linearVelocity = value;
            }
            get { return _linearVelocity; }
        }

        /// <summary>
        /// Gets or sets the angular velocity. Radians/second.
        /// </summary>
        /// <value>The angular velocity.</value>
        public float AngularVelocity
        {
            set
            {
                Debug.Assert(!float.IsNaN(value));

                if (_bodyType == BodyType.Static)
                    return;

                if (value * value > 0.0f)
                    Awake = true;

                _angularVelocity = value;
            }
            get { return _angularVelocity; }
        }

        /// <summary>
        /// Gets the current linear acceleration.
        /// </summary>
        public Vector2 LinearAcceleration => _force / Mass;

        /// <summary>
        /// Gets the current angular acceleration.
        /// </summary>
        public float AngularAcceleration => _torque / Inertia;

        /// <summary>
        /// Gets or sets the linear damping.
        /// </summary>
        /// <value>The linear damping.</value>
        public float LinearDamping
        {
            get { return _linearDamping; }
            set
            {
                Debug.Assert(!float.IsNaN(value));

                _linearDamping = value;
            }
        }

        /// <summary>
        /// Gets or sets the angular damping.
        /// </summary>
        /// <value>The angular damping.</value>
        public float AngularDamping
        {
            get { return _angularDamping; }
            set
            {
                Debug.Assert(!float.IsNaN(value));

                _angularDamping = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this body should be included in the CCD solver.
        /// </summary>
        /// <value><c>true</c> if this instance is included in CCD; otherwise, <c>false</c>.</value>
        public bool IsBullet { get; set; }

        /// <summary>
        /// You can disable sleeping on this body. If you disable sleeping, the
        /// body will be woken.
        /// </summary>
        /// <value><c>true</c> if sleeping is allowed; otherwise, <c>false</c>.</value>
        public bool SleepingAllowed
        {
            set
            {
                if (!value)
                    Awake = true;

                _sleepingAllowed = value;
            }
            get { return _sleepingAllowed; }
        }

        /// <summary>
        /// Set the sleep state of the body. A sleeping body has very
        /// low CPU cost.
        /// </summary>
        /// <value><c>true</c> if awake; otherwise, <c>false</c>.</value>
        public bool Awake
        {
            set
            {
                if (value)
                {
                    if (!_awake)
                    {
                        _sleepTime = 0.0f;
                        
#if USE_ACTIVE_CONTACT_SET
                        World.ContactManager.UpdateActiveContacts(ContactList, true);
#endif

#if USE_AWAKE_BODY_SET
						if (InWorld && !World.AwakeBodySet.Contains(this))
							World.AwakeBodySet.Add(this);
#endif
                    }
                }
                else
                {
#if USE_AWAKE_BODY_SET
					// Check even for BodyType.Static because if this body had just been changed to Static it will have
					// set Awake = false in the process.
					if (InWorld && World.AwakeBodySet.Contains(this))
						World.AwakeBodySet.Remove(this);
#endif
                    ResetDynamics();
                    _sleepTime = 0.0f;
                    
#if USE_ACTIVE_CONTACT_SET
                    World.ContactManager.UpdateActiveContacts(ContactList, false);
#endif
                }

                _awake = value;
            }
            get { return _awake; }
        }

        /// <summary>
        /// Set the active state of the body. An inactive body is not
        /// simulated and cannot be collided with or woken up.
        /// If you pass a flag of true, all fixtures will be added to the
        /// broad-phase.
        /// If you pass a flag of false, all fixtures will be removed from
        /// the broad-phase and all contacts will be destroyed.
        /// Fixtures and joints are otherwise unaffected. You may continue
        /// to create/destroy fixtures and joints on inactive bodies.
        /// Fixtures on an inactive body are implicitly inactive and will
        /// not participate in collisions, ray-casts, or queries.
        /// Joints connected to an inactive body are implicitly inactive.
        /// An inactive body is still owned by a b2World object and remains
        /// in the body list.
        /// Warning: This property is readonly during callbacks.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (World != null && World.IsLocked)
                    throw new InvalidOperationException("The World is locked.");

                if (value == _enabled)
                    return;

                _enabled = value;

                if (Enabled)
                {
                    if (World != null)
                        this.CreateProxy(); //CreateProxies();

                    // Contacts are created the next time step.
                }
                else
                {
                    if (World != null)
                    {
                        DestroyProxy();
                        //DestroyContacts();
                    }
                }
            }
        }

        /// <summary>
        /// Create all proxies.
        /// </summary>
        //internal void CreateProxies()
        //{   
        //    IBroadPhase broadPhase = World.ContactManager.BroadPhase;
        //    for (int i = 0; i < FixtureList.Count; i++)
        //        FixtureList[i].CreateProxies(broadPhase, ref _xf);
        //}

        ///// <summary>
        ///// Destroy all proxies.
        ///// </summary>
        //internal void DestroyProxies()
        //{
        //    IBroadPhase broadPhase = World.ContactManager.BroadPhase;
        //    for (int i = 0; i < FixtureList.Count; i++)
        //        FixtureList[i].DestroyProxies(broadPhase);
        //}

        /// <summary>
        /// Destroy the attached contacts.
        /// </summary>
        private void DestroyContacts()
        {            
            ContactEdge ce = ContactList;
            while (ce != null)
            {
                ContactEdge ce0 = ce;
                ce = ce.Next;
                World.ContactManager.Destroy(ce0.Contact);
            }
            ContactList = null;
        }


        /// <summary>
        /// Set this body to have fixed rotation. This causes the mass
        /// to be reset.
        /// </summary>
        /// <value><c>true</c> if it has fixed rotation; otherwise, <c>false</c>.</value>
        public bool FixedRotation
        {
            set
            {
                if (_fixedRotation == value)
                    return;

                _fixedRotation = value;

                _angularVelocity = 0f;
                ResetMassData();
            }
            get { return _fixedRotation; }
        }

        /// <summary>
        /// Gets all the fixtures attached to this body.
        /// </summary>
        /// <value>The fixture list.</value>
        public readonly List<Fixture> FixtureList;

        public DynamicTree<FixtureProxy> FixtureTree { get; } = new DynamicTree<FixtureProxy>();


        /// <summary>
        /// Get the list of all joints attached to this body.
        /// </summary>
        /// <value>The joint list.</value>
        public JointEdge JointList { get; internal set; }

        /// <summary>
        /// Get the list of all contacts attached to this body.
        /// Warning: this list changes during the time step and you may
        /// miss some collisions if you don't use ContactListener.
        /// </summary>
        /// <value>The contact list.</value>
        public ContactEdge ContactList { get; internal set; }

        /// <summary>
        /// Get the world body origin position.
        /// </summary>
        /// <returns>Return the world position of the body's origin.</returns>
        public Vector2 Position
        {
            get { return _xf.Position; }
            set
            {
                Debug.Assert(!float.IsNaN(value.X) && !float.IsNaN(value.Y));

                if (World == null)
                    _xf.Position = value;
                else
                    SetTransform(ref value, Rotation);
            }
        }

        /// <summary>
        /// Get the angle in radians.
        /// </summary>
        /// <returns>Return the current world rotation angle in radians.</returns>
        public float Rotation
        {
            get { return _sweep.A; }
            set
            {
                Debug.Assert(!float.IsNaN(value));

                if (World == null)
                    _sweep.A = value;
                else
                    SetTransform(ref _xf.Position, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this body is static.
        /// </summary>
        /// <value><c>true</c> if this instance is static; otherwise, <c>false</c>.</value>
        /// <remarks>Deprecated in version 1.2</remarks>
        [Obsolete("Use BodyType")]
        public bool IsStatic
        {
            get { return _bodyType == BodyType.Static; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this body is kinematic.
        /// </summary>
        /// <value><c>true</c> if this instance is kinematic; otherwise, <c>false</c>.</value>
        /// <remarks>Deprecated in version 1.2</remarks>
        [Obsolete("Use BodyType")]
        public bool IsKinematic
        {
            get { return _bodyType == BodyType.Kinematic; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this body ignores gravity.
        /// </summary>
        /// <value><c>true</c> if  it ignores gravity; otherwise, <c>false</c>.</value>
        public bool IgnoreGravity { get; set; }

        /// <summary>
        /// Get the world position of the center of mass.
        /// </summary>
        /// <value>The world position.</value>
        public Vector2 WorldCenter
        {
            get { return _sweep.C; }
        }

        /// <summary>
        /// Get the local position of the center of mass.
        /// Warning: This property is readonly during callbacks.
        /// </summary>
        /// <value>The local position.</value>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public Vector2 LocalCenter
        {
            get { return _sweep.LocalCenter; }
            set
            {
                if (World != null && World.IsLocked)
                    throw new InvalidOperationException("The World is locked.");

                if (_bodyType != BodyType.Dynamic)
                    return;

                // Move center of mass.
                Vector2 oldCenter = _sweep.C;
                _sweep.LocalCenter = value;
                _sweep.C0 = _sweep.C = Transform.Multiply(ref _sweep.LocalCenter, ref _xf);

                // Update center of mass velocity.
                Vector2 a = _sweep.C - oldCenter;
                _linearVelocity += new Vector2(-_angularVelocity * a.Y, _angularVelocity * a.X);
            }
        }

        /// <summary>
        /// Gets or sets the mass. Usually in kilograms (kg).
        /// Warning: This property is readonly during callbacks.
        /// </summary>
        /// <value>The mass.</value>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public float Mass
        {
            get { return _mass; }
            set
            {
                if (World != null && World.IsLocked)
                    throw new InvalidOperationException("The World is locked.");

                Debug.Assert(!float.IsNaN(value));

                if (_bodyType != BodyType.Dynamic) //Make an assert
                    return;

                _mass = value;

                if (_mass <= 0.0f)
                    _mass = 1.0f;

                _invMass = 1.0f / _mass;
            }
        }

        /// <summary>
        /// Get or set the rotational inertia of the body about the local origin. usually in kg-m^2.
        /// Warning: This property is readonly during callbacks.
        /// </summary>
        /// <value>The inertia.</value>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public float Inertia
        {
            get { return _inertia + Mass * Vector2.Dot(_sweep.LocalCenter, _sweep.LocalCenter); }
            set
            {
                if (World != null && World.IsLocked)
                    throw new InvalidOperationException("The World is locked.");

                Debug.Assert(!float.IsNaN(value));

                if (_bodyType != BodyType.Dynamic) //Make an assert
                    return;

                if (value > 0.0f && !_fixedRotation) //Make an assert
                {
                    _inertia = value - Mass * Vector2.Dot(LocalCenter, LocalCenter);
                    Debug.Assert(_inertia > 0.0f);
                    _invI = 1.0f / _inertia;
                }
            }
        }

        public bool IgnoreCCD { get; set; }

        /// <summary>
        /// Resets the dynamics of this body.
        /// Sets torque, force and linear/angular velocity to 0
        /// </summary>
        public void ResetDynamics()
        {
            _torque = 0;
            _angularVelocity = 0;
            _force = Vector2.Zero;
            _linearVelocity = Vector2.Zero;
        }

        // These support body activation/deactivation.
        internal void CreateProxy()
        {
            Debug.Assert(_broadphaseProxyId < 0);

            // Create proxy.
            BodyProxy proxy;
            proxy.AABB = FixtureTree.RootAABB;
            AABB.Transform(ref _xf, ref proxy.AABB);
            proxy.Body = this;
            proxy.ProxyId = -1; // NOTE: We set to -1 only initially. It will be set to actual ID within AddProxy.
            _broadphaseProxyId = _world.ContactManager.BroadPhase.AddProxy(ref proxy);
        }

        internal void DestroyProxy()
        {
            Debug.Assert(_broadphaseProxyId >= 0);

            _world.ContactManager.BroadPhase.RemoveProxy(_broadphaseProxyId);
            _broadphaseProxyId = -1;
        }

        ///<summary>
        /// Warning: This method is locked during callbacks.
        /// </summary>>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void Add(Fixture fixture)
        {
            if (World != null && World.IsLocked)
                throw new InvalidOperationException("The World is locked.");
            if (fixture == null)
                throw new ArgumentNullException("fixture");
            if (fixture.Body != null)
            {
                if (fixture.Body == this)
                    throw new ArgumentException("You are adding the same fixture more than once.", "fixture");
                else
                    throw new ArgumentException("fixture belongs to another body.", "fixture");
            }

            fixture.Body = this;
            this.FixtureList.Add(fixture);
#if DEBUG
            if (fixture.Shape.ShapeType == ShapeType.Polygon)
                ((PolygonShape)fixture.Shape).Vertices.AttachedToBody = true;
#endif

            // Adjust mass properties if needed.
            if (fixture.Shape._density > 0.0f)
                ResetMassData();

            if (World != null)
            {
                if (Enabled)
                {
                    //IBroadPhase broadPhase = World.ContactManager.BroadPhase;
                    //fixture.CreateProxies(broadPhase, ref _xf);
                    //fixture.CreateProxies(this.FixtureTree, ref _xf);

                    // (Or perhaps CreateProxies should be refactored to not even take a transform parameter.)
                    fixture.CreateProxies(this.FixtureTree, ref Transform.Identity);

                    // problem with static bodies. A static body's AABB never gets updated after adding fixtures
                    this.Synchronize();
                }

                // Let the world know we have a new fixture. This will cause new contacts
                // to be created at the beginning of the next time step.
                World._worldHasNewFixture = true;

                if (World.FixtureAdded != null)
                    World.FixtureAdded(World, this, fixture);
            }
        }

        /// <summary>
        /// Destroy a fixture. This removes the fixture from the broad-phase and
        /// destroys all contacts associated with this fixture. This will
        /// automatically adjust the mass of the body if the body is dynamic and the
        /// fixture has positive density.
        /// All fixtures attached to a body are implicitly destroyed when the body is destroyed.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="fixture">The fixture to be removed.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public virtual void Remove(Fixture fixture)
        {
            if (World != null && World.IsLocked)
                throw new InvalidOperationException("The World is locked.");
            if (fixture == null)
                throw new ArgumentNullException("fixture");
            if (fixture.Body != this)
                throw new ArgumentException("You are removing a fixture that does not belong to this Body.", "fixture");

            // Destroy any contacts associated with the fixture.
            ContactEdge edge = ContactList;
            while (edge != null)
            {
                Contact c = edge.Contact;
                edge = edge.Next;

                Fixture fixtureA = c.FixtureA;
                Fixture fixtureB = c.FixtureB;

                if (fixture == fixtureA || fixture == fixtureB)
                {
                    // This destroys the contact and removes it from
                    // this body's contact list.
                    World.ContactManager.Destroy(c);
                }
            }

            fixture.DestroyProxies(FixtureTree);

            FixtureList.Remove(fixture);
            //fixture.Destroy();
            fixture.Body = null;
#if DEBUG
            if (fixture.Shape.ShapeType == ShapeType.Polygon)
                ((PolygonShape)fixture.Shape).Vertices.AttachedToBody = false;
#endif

            if (World.FixtureRemoved != null)
                World.FixtureRemoved(World, this, fixture);

            ResetMassData();
        }

        /// <summary>
        /// Set the position of the body's origin and rotation.
        /// This breaks any contacts and wakes the other bodies.
        /// Manipulating a body's transform may cause non-physical behavior.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="position">The world position of the body's local origin.</param>
        /// <param name="rotation">The world rotation in radians.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void SetTransform(ref Vector2 position, float rotation)
        {
            SetTransformIgnoreContacts(ref position, rotation);

            World.ContactManager.FindNewContacts();
        }

        /// <summary>
        /// Set the position of the body's origin and rotation.
        /// This breaks any contacts and wakes the other bodies.
        /// Manipulating a body's transform may cause non-physical behavior.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="position">The world position of the body's local origin.</param>
        /// <param name="rotation">The world rotation in radians.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void SetTransform(Vector2 position, float rotation)
        {
            SetTransform(ref position, rotation);
        }

        /// <summary>
        /// For teleporting a body without considering new contacts immediately.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="angle">The angle.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void SetTransformIgnoreContacts(ref Vector2 position, float angle)
        {
            Debug.Assert(World != null);
            if (World.IsLocked)
                throw new InvalidOperationException("The World is locked.");

            _xf.Rotation.Phase = angle;
            _xf.Position = position;

            _sweep.C = Transform.Multiply(ref _sweep.LocalCenter, ref _xf);
            _sweep.A = angle;

            _sweep.C0 = _sweep.C;
            _sweep.A0 = angle;

            Synchronize(ref _xf, ref _xf);
            //IBroadPhase broadPhase = World.ContactManager.BroadPhase;
            //for (int i = 0; i < FixtureList.Count; i++)
            //    FixtureList[i].Synchronize(broadPhase, ref _xf, ref _xf);
        }

        /// <summary>
        /// Get the body transform for the body's origin.
        /// </summary>
        /// <param name="transform">The transform of the body's origin.</param>
        public Transform GetTransform()
        {
            return _xf;
        }

        /// <summary>
        /// Get the body transform for the body's origin.
        /// </summary>
        /// <param name="transform">The transform of the body's origin.</param>
        public void GetTransform(out Transform transform)
        {
            transform = _xf;
        }

        /// <summary>
        /// Apply a force at a world point. If the force is not
        /// applied at the center of mass, it will generate a torque and
        /// affect the angular velocity. This wakes up the body.
        /// </summary>
        /// <param name="force">The world force vector, usually in Newtons (N).</param>
        /// <param name="point">The world position of the point of application.</param>
        public void ApplyForce(Vector2 force, Vector2 point)
        {
            ApplyForce(ref force, ref point);
        }

        /// <summary>
        /// Applies a force at the center of mass.
        /// </summary>
        /// <param name="force">The force.</param>
        public void ApplyForce(ref Vector2 force)
        {
            ApplyForce(ref force, ref _xf.Position);
        }

        /// <summary>
        /// Applies a force at the center of mass.
        /// </summary>
        /// <param name="force">The force.</param>
        public void ApplyForce(Vector2 force)
        {
            ApplyForce(ref force, ref _xf.Position);
        }

        /// <summary>
        /// Apply a force at a world point. If the force is not
        /// applied at the center of mass, it will generate a torque and
        /// affect the angular velocity. This wakes up the body.
        /// </summary>
        /// <param name="force">The world force vector, usually in Newtons (N).</param>
        /// <param name="point">The world position of the point of application.</param>
        public void ApplyForce(ref Vector2 force, ref Vector2 point)
        {
            Debug.Assert(!float.IsNaN(force.X));
            Debug.Assert(!float.IsNaN(force.Y));
            Debug.Assert(!float.IsNaN(point.X));
            Debug.Assert(!float.IsNaN(point.Y));

            if (_bodyType == BodyType.Dynamic)
            {
                if (Awake == false)
                    Awake = true;

                _force += force;
                _torque += (point.X - _sweep.C.X) * force.Y - (point.Y - _sweep.C.Y) * force.X;
            }
        }

        /// <summary>
        /// Apply a torque. This affects the angular velocity
        /// without affecting the linear velocity of the center of mass.
        /// This wakes up the body.
        /// </summary>
        /// <param name="torque">The torque about the z-axis (out of the screen), usually in N-m.</param>
        public void ApplyTorque(float torque)
        {
            Debug.Assert(!float.IsNaN(torque));

            if (_bodyType == BodyType.Dynamic)
            {
                if (Awake == false)
                    Awake = true;

                _torque += torque;
            }
        }

        /// <summary>
        /// Apply an impulse at a point. This immediately modifies the velocity.
        /// This wakes up the body.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N-seconds or kg-m/s.</param>
        public void ApplyLinearImpulse(Vector2 impulse)
        {
            ApplyLinearImpulse(ref impulse);
        }

        /// <summary>
        /// Apply an impulse at a point. This immediately modifies the velocity.
        /// It also modifies the angular velocity if the point of application
        /// is not at the center of mass.
        /// This wakes up the body.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N-seconds or kg-m/s.</param>
        /// <param name="point">The world position of the point of application.</param>
        public void ApplyLinearImpulse(Vector2 impulse, Vector2 point)
        {
            ApplyLinearImpulse(ref impulse, ref point);
        }

        /// <summary>
        /// Apply an impulse at a point. This immediately modifies the velocity.
        /// This wakes up the body.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N-seconds or kg-m/s.</param>
        public void ApplyLinearImpulse(ref Vector2 impulse)
        {
            if (_bodyType != BodyType.Dynamic)
            {
                return;
            }
            if (Awake == false)
            {
                Awake = true;
            }
            _linearVelocity += _invMass * impulse;
        }

        /// <summary>
        /// Apply an impulse at a point. This immediately modifies the velocity.
        /// It also modifies the angular velocity if the point of application
        /// is not at the center of mass.
        /// This wakes up the body.
        /// </summary>
        /// <param name="impulse">The world impulse vector, usually in N-seconds or kg-m/s.</param>
        /// <param name="point">The world position of the point of application.</param>
        public void ApplyLinearImpulse(ref Vector2 impulse, ref Vector2 point)
        {
            if (_bodyType != BodyType.Dynamic)
                return;

            if (Awake == false)
                Awake = true;

            _linearVelocity += _invMass * impulse;
            _angularVelocity += _invI * ((point.X - _sweep.C.X) * impulse.Y - (point.Y - _sweep.C.Y) * impulse.X);
        }

        /// <summary>
        /// Apply an angular impulse.
        /// </summary>
        /// <param name="impulse">The angular impulse in units of kg*m*m/s.</param>
        public void ApplyAngularImpulse(float impulse)
        {
            if (_bodyType != BodyType.Dynamic)
            {
                return;
            }

            if (Awake == false)
            {
                Awake = true;
            }

            _angularVelocity += _invI * impulse;
        }

        /// <summary>
        /// This resets the mass properties to the sum of the mass properties of the fixtures.
        /// This normally does not need to be called unless you called SetMassData to override
        /// the mass and you later want to reset the mass.
        /// </summary>
        public void ResetMassData()
        {
            // Compute mass data from shapes. Each shape has its own density.
            _mass = 0.0f;
            _invMass = 0.0f;
            _inertia = 0.0f;
            _invI = 0.0f;
            _sweep.LocalCenter = Vector2.Zero;

            // Kinematic bodies have zero mass.
            if (BodyType == BodyType.Kinematic)
            {
                _sweep.C0 = _xf.Position;
                _sweep.C = _xf.Position;
                _sweep.A0 = _sweep.A;
                return;
            }

            Debug.Assert(BodyType == BodyType.Dynamic || BodyType == BodyType.Static);

            // Accumulate mass over all fixtures.
            Vector2 localCenter = Vector2.Zero;
            foreach (Fixture f in FixtureList)
            {
                if (f.Shape._density == 0)
                {
                    continue;
                }

                MassData massData = f.Shape.MassData;
                _mass += massData.Mass;
                localCenter += massData.Mass * massData.Centroid;
                _inertia += massData.Inertia;
            }

            //FPE: Static bodies only have mass, they don't have other properties. A little hacky tho...
            if (BodyType == BodyType.Static)
            {
                _sweep.C0 = _sweep.C = _xf.Position;
                return;
            }

            // Compute center of mass.
            if (_mass > 0.0f)
            {
                _invMass = 1.0f / _mass;
                localCenter *= _invMass;
            }
            else
            {
                // Force all dynamic bodies to have a positive mass.
                _mass = 1.0f;
                _invMass = 1.0f;
            }

            if (_inertia > 0.0f && !_fixedRotation)
            {
                // Center the inertia about the center of mass.
                _inertia -= _mass * Vector2.Dot(localCenter, localCenter);

                Debug.Assert(_inertia > 0.0f);
                _invI = 1.0f / _inertia;
            }
            else
            {
                _inertia = 0.0f;
                _invI = 0.0f;
            }

            // Move center of mass.
            Vector2 oldCenter = _sweep.C;
            _sweep.LocalCenter = localCenter;
            _sweep.C0 = _sweep.C = Transform.Multiply(ref _sweep.LocalCenter, ref _xf);

            // Update center of mass velocity.
            Vector2 a = _sweep.C - oldCenter;
            _linearVelocity += new Vector2(-_angularVelocity * a.Y, _angularVelocity * a.X);
        }

        /// <summary>
        /// Get the world coordinates of a point given the local coordinates.
        /// </summary>
        /// <param name="localPoint">A point on the body measured relative the the body's origin.</param>
        /// <returns>The same point expressed in world coordinates.</returns>
        public Vector2 GetWorldPoint(ref Vector2 localPoint)
        {
            return Transform.Multiply(ref localPoint, ref _xf);
        }

        /// <summary>
        /// Get the world coordinates of a point given the local coordinates.
        /// </summary>
        /// <param name="localPoint">A point on the body measured relative the the body's origin.</param>
        /// <returns>The same point expressed in world coordinates.</returns>
        public Vector2 GetWorldPoint(Vector2 localPoint)
        {
            return GetWorldPoint(ref localPoint);
        }

        /// <summary>
        /// Get the world coordinates of a vector given the local coordinates.
        /// Note that the vector only takes the rotation into account, not the position.
        /// </summary>
        /// <param name="localVector">A vector fixed in the body.</param>
        /// <returns>The same vector expressed in world coordinates.</returns>
        public Vector2 GetWorldVector(ref Vector2 localVector)
        {
            return Complex.Multiply(ref localVector, ref _xf.Rotation);
        }

        /// <summary>
        /// Get the world coordinates of a vector given the local coordinates.
        /// </summary>
        /// <param name="localVector">A vector fixed in the body.</param>
        /// <returns>The same vector expressed in world coordinates.</returns>
        public Vector2 GetWorldVector(Vector2 localVector)
        {
            return GetWorldVector(ref localVector);
        }

        /// <summary>
        /// Gets a local point relative to the body's origin given a world point.
        /// Note that the vector only takes the rotation into account, not the position.
        /// </summary>
        /// <param name="worldPoint">A point in world coordinates.</param>
        /// <returns>The corresponding local point relative to the body's origin.</returns>
        public Vector2 GetLocalPoint(ref Vector2 worldPoint)
        {
            return Transform.Divide(ref worldPoint, ref _xf);
        }

        /// <summary>
        /// Gets a local point relative to the body's origin given a world point.
        /// </summary>
        /// <param name="worldPoint">A point in world coordinates.</param>
        /// <returns>The corresponding local point relative to the body's origin.</returns>
        public Vector2 GetLocalPoint(Vector2 worldPoint)
        {
            return GetLocalPoint(ref worldPoint);
        }

        /// <summary>
        /// Gets a local vector given a world vector.
        /// Note that the vector only takes the rotation into account, not the position.
        /// </summary>
        /// <param name="worldVector">A vector in world coordinates.</param>
        /// <returns>The corresponding local vector.</returns>
        public Vector2 GetLocalVector(ref Vector2 worldVector)
        {
            return Complex.Divide(ref worldVector, ref _xf.Rotation);
        }

        /// <summary>
        /// Gets a local vector given a world vector.
        /// Note that the vector only takes the rotation into account, not the position.
        /// </summary>
        /// <param name="worldVector">A vector in world coordinates.</param>
        /// <returns>The corresponding local vector.</returns>
        public Vector2 GetLocalVector(Vector2 worldVector)
        {
            return GetLocalVector(ref worldVector);
        }

        /// <summary>
        /// Get the world linear velocity of a world point attached to this body.
        /// </summary>
        /// <param name="worldPoint">A point in world coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromWorldPoint(Vector2 worldPoint)
        {
            return GetLinearVelocityFromWorldPoint(ref worldPoint);
        }

        /// <summary>
        /// Get the world linear velocity of a world point attached to this body.
        /// </summary>
        /// <param name="worldPoint">A point in world coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromWorldPoint(ref Vector2 worldPoint)
        {
            return _linearVelocity +
                   new Vector2(-_angularVelocity * (worldPoint.Y - _sweep.C.Y),
                               _angularVelocity * (worldPoint.X - _sweep.C.X));
        }

        /// <summary>
        /// Get the world velocity of a local point.
        /// </summary>
        /// <param name="localPoint">A point in local coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromLocalPoint(Vector2 localPoint)
        {
            return GetLinearVelocityFromLocalPoint(ref localPoint);
        }

        /// <summary>
        /// Get the world velocity of a local point.
        /// </summary>
        /// <param name="localPoint">A point in local coordinates.</param>
        /// <returns>The world velocity of a point.</returns>
        public Vector2 GetLinearVelocityFromLocalPoint(ref Vector2 localPoint)
        {
            return GetLinearVelocityFromWorldPoint(GetWorldPoint(ref localPoint));
        }

        internal void Synchronize()
        {
            Transform xf1 = new Transform(Vector2.Zero, _sweep.A0);
            xf1.Position = _sweep.C0 - Complex.Multiply(ref _sweep.LocalCenter, ref xf1.Rotation);

            Synchronize(ref xf1, ref _xf);
            //IBroadPhase broadPhase = World.ContactManager.BroadPhase;
            //for (int i = 0; i < FixtureList.Count; i++)
            //{
            //    FixtureList[i].Synchronize(broadPhase, ref xf1, ref _xf);
            //}
        }

        internal Vector2 _displacement;
        private void Synchronize(ref Transform transform1, ref Transform transform2)
        {
            // Compute an AABB that covers the swept area.
            AABB aabb1 = FixtureTree.RootAABB;
            AABB aabb2 = aabb1;
            AABB.Transform(ref transform1, ref aabb1);
            AABB.Transform(ref transform2, ref aabb2);
            aabb1.Combine(ref aabb2);

            _displacement = transform2.Position - transform1.Position;

            _world.ContactManager.BroadPhase.MoveProxy(_broadphaseProxyId, ref aabb1, _displacement);
        }

        internal void SynchronizeTransform()
        {
            _xf.Rotation.Phase = _sweep.A;
            _xf.Position = _sweep.C - Complex.Multiply(ref _sweep.LocalCenter, ref _xf.Rotation);
        }

        /// <summary>
        /// This is used to prevent connected bodies from colliding.
        /// It may lie, depending on the collideConnected flag.
        /// </summary>
        /// <param name="other">The other body.</param>
        /// <returns></returns>
        internal bool ShouldCollide(Body other)
        {
            // At least one body should be dynamic.
            if (_bodyType != BodyType.Dynamic && other._bodyType != BodyType.Dynamic)
            {
                return false;
            }

            // Does a joint prevent collision?
            for (JointEdge jn = JointList; jn != null; jn = jn.Next)
            {
                if (jn.Other == other)
                {
                    if (jn.Joint.CollideConnected == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal void Advance(float alpha)
        {
            // Advance to the new safe time. This doesn't sync the broad-phase.
            _sweep.Advance(alpha);
            _sweep.C = _sweep.C0;
            _sweep.A = _sweep.A0;
            _xf.Rotation.Phase = _sweep.A;
            _xf.Position = _sweep.C - Complex.Multiply(ref _sweep.LocalCenter, ref _xf.Rotation);
        }

        internal OnBeginContactEventHandler onBeginContactEventHandler;
        public event OnBeginContactEventHandler OnBeginContact
        {
            add { onBeginContactEventHandler += value; }
            remove { onBeginContactEventHandler -= value; }
        }

        internal OnEndContactEventHandler onEndContactEventHandler;
        public event OnEndContactEventHandler OnEndContact
        {
            add { onEndContactEventHandler += value; }
            remove { onEndContactEventHandler -= value; }
        }

        /// <summary>
        /// Fires after two shapes has collided and are solved. This gives you a chance to get the impact force.
        /// </summary>
        public CollisionPostSolveEventHandler CollisionPostSolve;

        /// <summary>
        /// Set restitution on all fixtures.
        /// Warning: This method applies the value on existing Fixtures. It's not a property of Body.
        /// </summary>
        /// <param name="restitution"></param>
        public void SetRestitution(float restitution)
        {
            for (int i = 0; i < FixtureList.Count; i++)
                FixtureList[i].Restitution = restitution;
        }

        /// <summary>
        /// Set friction on all fixtures.
        /// Warning: This method applies the value on existing Fixtures. It's not a property of Body.
        /// </summary>
        /// <param name="friction"></param>
        public void SetFriction(float friction)
        {
            for (int i = 0; i < FixtureList.Count; i++)
                FixtureList[i].Friction = friction;
        }

        /// <summary>
        /// Warning: This method applies the value on existing Fixtures. It's not a property of Body.
        /// </summary>
        public void SetCollisionCategories(Category category)
        {
            for (int i = 0; i < FixtureList.Count; i++)
                FixtureList[i].CollisionCategories = category;
        }

        /// <summary>
        /// Warning: This method applies the value on existing Fixtures. It's not a property of Body.
        /// </summary>
        public void SetCollidesWith(Category category)
        {
            for (int i = 0; i < FixtureList.Count; i++)
                FixtureList[i].CollidesWith = category;
        }

        /// <summary>
        /// Warning: This method applies the value on existing Fixtures. It's not a property of Body.
        /// </summary>
        public void SetCollisionGroup(short collisionGroup)
        {
            for (int i = 0; i < FixtureList.Count; i++)
                FixtureList[i].CollisionGroup = collisionGroup;
        }

        /// <summary>
        /// Warning: This method applies the value on existing Fixtures. It's not a property of Body.
        /// </summary>
        public void SetIsSensor(bool isSensor)
        {
            for (int i = 0; i < FixtureList.Count; i++)
                FixtureList[i].IsSensor = isSensor;
        }

        /// <summary>
        /// Makes a clone of the body. Fixtures and therefore shapes are not included.
        /// Use DeepClone() to clone the body, as well as fixtures and shapes.
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public Body Clone(World world = null)
        {
            world = world ?? World;
            Body body = world.CreateBody(Position, Rotation);
            body._bodyType = _bodyType;
            body._linearVelocity = _linearVelocity;
            body._angularVelocity = _angularVelocity;
            body.Tag = Tag;
            body._enabled = _enabled;
            body._fixedRotation = _fixedRotation;
            body._sleepingAllowed = _sleepingAllowed;
            body._linearDamping = _linearDamping;
            body._angularDamping = _angularDamping;
            body._awake = _awake;
            body.IsBullet = IsBullet;
            body.IgnoreCCD = IgnoreCCD;
            body.IgnoreGravity = IgnoreGravity;
            body._torque = _torque;

            return body;
        }

        /// <summary>
        /// Clones the body and all attached fixtures and shapes. Simply said, it makes a complete copy of the body.
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public Body DeepClone(World world = null)
        {
            Body body = Clone(world ?? World);

            int count = FixtureList.Count; //Make a copy of the count. Otherwise it causes an infinite loop.
            for (int i = 0; i < count; i++)
            {
                FixtureList[i].CloneOnto(body);
            }

            return body;
        }


        #region Custom Code - AetherX

        /// <summary>
        /// Override which just uses the body's center as the bounding box center position.
        /// </summary>
        /// <param name="includeBodyRotation"></param>
        /// <returns></returns>
        public AABB GetBoundingBox()
        {
            //return this.GetBoundingBox(this.WorldCenter, includeBodyRotation);
            var boundingBox = this.GetBoundingBoxAtOrigin();

            // apply change to center position.
            // NOTE: should this be "Position" instead of "WorldCenter"? "WorldCenter" is the center of mass...
            var boxCenterPosition = this.WorldCenter - this.LocalCenter.Rotate(this.Rotation);
            boundingBox.MoveCenter(boxCenterPosition);

            return boundingBox;
        }

        public AABB GetBoundingBoxAtOrigin()
        {
            // TODO: could potentially cache the zero-origin unrotated body, at least until body changes.

            AABB physicalObjectBoundingBox = new AABB();

            foreach (var fixture in this.FixtureList)
            {
                // But the b2Fixture::GetAABB() method now requires a childIndex parameter. In reviewing the code, I don't see any comments about what this childIndex is, or what I should use if I'm in the process 
                // of converting. I'm inclined to use 0 (zero), but I don't know if that's the right thing to do.
                // Yeah every fixture has 1 to m child fixtures. For fixtures with a single child (circle, polygon, edge), the index is zero. For chain shapes, the index is 0 to m-1.
                const int FixtureBoundingBoxIndex = 0;

                // get the fixture bounding box.
                AABB fixtureBoundingBox = new AABB();
                fixture.GetAABB(out fixtureBoundingBox, FixtureBoundingBoxIndex);

                // add this fixture's bounding box to the total
                physicalObjectBoundingBox.Combine(ref fixtureBoundingBox);
            }

            return physicalObjectBoundingBox;
        }

        // NOTE: vertices, not an AABB -- it has to be able to rotate. AABB are always unrotated.
        public Vertices GetBoundingRectVertices()
        {
            //return this.GetBoundingBox(this.WorldCenter, includeBodyRotation);
            var boundingBox = this.GetBoundingBoxAtOrigin();

            // NOTE: Can't just rotate the upper and lower corners. We need to get the vertices, and rotate them.
            var rotatedVertices = new List<Vector2>();

            for (var i = 0; i < boundingBox.Vertices.Count; i++)
            {
                // rotate
                var newVertex = boundingBox.Vertices[i].Rotate(this.Rotation);

                // add to list
                rotatedVertices.Add(newVertex);
            }

            var vertices = new Vertices(rotatedVertices);

            // move to world center
            // NOTE: should this be "Position" instead of "WorldCenter"? "WorldCenter" is the center of mass...
            var boxCenterPosition = this.WorldCenter - this.LocalCenter.Rotate(this.Rotation);
            vertices.Translate(boxCenterPosition);

            return vertices;
        }

        public Circle GetBoundingCircle()
        {
            // get bounding rect
            Vertices rotatedBoundingBoxVertices = this.GetBoundingRectVertices();

            // return bounding circle
            return new Circle(
                rotatedBoundingBoxVertices.GetCentroid(),
                rotatedBoundingBoxVertices.GetRadius()
                );
        }

        #endregion
    }
}