# AetherX Physics Engine

Box2D is the foundational physics engine, which was then ported to C# as Farseer and evolved independently. Farseer is now abandoned and lives on in Aether.Physics2D, which is still maintained as of Dec 2018. This is a fork of Aether.Physics2D which is enhanced to include further optimizations specifically for many-fixtured bodies in a huge, expansive world.

# Getting Started

1) Install MonoGame. (tested with v3.16: http://www.monogame.net/2017/03/01/monogame-3-6/)
2) Pull down the "master" branch from this repo.
3) Open the "Monogame Samples" solution (Samples.WINDOWS.MG.sln) in Visual Studio (tested with VS 2017)
4) Set "Testbed.WINDOWS.MG" as the start-up project.
5) Start the solution. 

# Added Features

**Per-Body Broadphase AABB Prior to Fixture AABB Test**
* Greatly reduces collision detection time for bodies containing more than one fixture.
* PR: https://github.com/alundgren04/AetherX/pull/4
* Special thanks to Walt Destler (https://github.com/waltdestler)

**Implemented "Hibernation" (optional setting)**
* No matter how performant the engine is, it will eventually slow as bodies are added. Hibernation is the solution to this. All bodies are offloaded to a shadow world which conducts no processing. You may add "Active Areas" around player within the world, and as they traverse the area, bodies will be brought back from hibernation and added to the world when the player approaches and then returned to hibernation if they are out of view. Seemingly simple, but surprisingly complex to get right. You can see the MASSIVE performance gains best with one of the first three test cases: "World Performance Test," "Sparse bodies test," or "Sparse bodies with many fixtures Test." Press tilde (~) to get advanced options. Press 'h' to toggle hibernation. The grey bodies are those which are hibernated. You may add an "Active Area" which will wake bodies by right clicking. Picture this "Active Area" being slightly larger than the player's view, so they would never be able to see anything beyond it. Move the "Active Area" around and observe the behavior while also watching the performance graph. Toggle hibernation to see the before and after. This is what will allow you to have enormous worlds. There are some limitations -- joints currently aren't supported. The logic needs to be extended to find any associated bodies and hibernate/unhibernate them in sync if both are out of view or one is in view. Not terribly complicated, just hasn't been done yet. Hibernation is fully optional and can be disabled.
* I plan to create a video demonstrating how this works, when time permits. If you're interested, please email me and I'll see if I can get to it sooner rather than later.
* Here are VERY rough notes I kept during implementation. This is not up-to-date, but may show some of the decision making and concepts, in case someone is interested in the details. Again, if I hear anyone is interested in this, I will work on updating it. https://github.com/andrew-sidereal/AetherX/wiki/Optimization:-Body-Hibernation

**Added unique integer IDs to each body.**

**Detailed performance debugging and options. Press tilde (~) to see advanced options.**

**Added WorldPerformanceTest to benchmark performance of very large worlds.**

**Ability to load a R.U.B.E. (Really Useful Box2D Editor) world via JSON.**


# Existing Features

The below items are all inherited from the original repo at: https://github.com/tainicom/Aether.Physics2D
* Continuous collision detection (with time of impact solver)
* Contact callbacks: begin, end, pre-solve, post-solve
* Convex and concave polyons and circles.
* Multiple shapes per body
* Dynamic tree and quad tree broadphase
* Fast broadphase AABB queries and raycasts
* Collision groups and categories
* Sleep management
* Friction and restitution
* Stable stacking with a linear-time solver
* Revolute, prismatic, distance, pulley, gear, mouse joint, and other joint types
* Joint limits and joint motors
* Controllers (gravity, force generators)
* Tools to decompose concave polygons, find convex hulls and boolean operations
* Factories to simplify the creation of bodies
* Fluid dynamics

# Feature Roadmap / Proposals

**Improved diagnostics**
* Record the "ticks" at the time each AABB is tested for collision, so the DebugView may highlight their rendering accordingly. This will help ensure there is no unneccesary processing. For example, ensuring a fixture AABB isn't ever compared unless the body AABB has collided.
* Any time AABB is recalculated, record "tick" time. In DebugView, highlight if it happened recently. Goal: low # highlights.

**Collision Optimizations**
* Only calculate and populate the fixture AABB tree for a given body if the body AABB is colliding. Otherwise, it shouldn't be needed at all. If the body AABB calculation has a dependency on the fixtureAABBs, then perhaps it calculates once and rotates as needed, or even uses a simple radius check.

* Only do collision tests for bodies which already have their body AABB collided or have had their bady AABB recalculated/moved. Otherwise, there should be no need. Moving bodies will test for collisions. Bodies which haven't moved don't need to test.

* Tighten up the CircleShape AABB. Currently it's factoring in rotation in a way which seems wrong. Needs more investigation.

* Tighten up the body AABBs, as they seem to have a margin beyond the fixture AABBs. I believe they should match the outer extents of the body's fixture AABBs with no additional margin/fattening.

* If a body only has one fixture, then don't calc/store fixture AABBs and if the body AABB collides, count it as that single fixture colliding.

* Offload bodies which haven't collided recently and which aren't near an "interest area" to a shadow World object, where they may be "hibernated"  until needed. More to come on this. Architecture draft: https://github.com/alundgren04/AetherX/wiki/Optimization:-Body-Hibernation

* Rename the project to aid discoverability. Search combinations of "Aether" and "Physics" yields a massive amount of search results which are unrelated. Perhaps revert back to a name closer to a term users may be searching for, such as "Box2D" or "Farseer." Perhaps "Farseer 2" or "Sharp Box2D." 
