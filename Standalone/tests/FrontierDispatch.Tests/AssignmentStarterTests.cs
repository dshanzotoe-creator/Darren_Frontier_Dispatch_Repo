using System;
using System.Linq;
using FrontierDispatch.Core;
using NUnit.Framework;

namespace FrontierDispatch.Tests
{
    /// <summary>
    /// Small contract examples for later assignment phases.
    ///
    /// These are not an exhaustive assignment test suite.
    /// Students are expected to enable the relevant tests as they reach each
    /// phase and add meaningful tests of their own, including edge cases.
    /// </summary>
    public sealed class AssignmentStarterTests
    {
        // These tests document later assignment contracts without making the
        // untouched baseline red. Remove Ignore when you reach the named phase.

        [Test]
        public void PriorityQueue_EnqueueAndDequeue_ReturnsLowestPriorityFirst()
        {
            var queue = new MinPriorityQueue<string>();
            queue.Enqueue("expensive", 9);
            queue.Enqueue("cheap", 2);
            queue.Enqueue("medium", 5);
            Assert.That(queue.Dequeue(), Is.EqualTo("cheap"));
        }

        [Test]
        public void Dijkstra_StartEqualsGoal_ReturnsZeroCostRoute()
        {
            var finder = new DijkstraRouteFinder(WorldDatasetFactory.CreateSmall());
            RouteResult result = finder.FindRoute(0, 0);
            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.EqualTo(0));
            Assert.That(result.LocationIds, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Reachability_ZeroBudget_IncludesStartAtZeroCost()
        {
            var query = new ReachabilityQuery(WorldDatasetFactory.CreateSmall());
            var result = query.FindReachable(0, 0);
            Assert.That(result.ContainsKey(0), Is.True);
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void PriorityQueue_HandlesDuplicatesAndEmptyQueue()
        {
            var queue = new MinPriorityQueue<string>();

            queue.Enqueue("high", 9);
            queue.Enqueue("lowA", 2);
            queue.Enqueue("lowB", 2);
            queue.Enqueue("medium", 5);

            Assert.That(queue.Count, Is.EqualTo(4));

            // Peek must not remove an item.
            Assert.That(queue.Peek(), Is.AnyOf("lowA", "lowB"));
            Assert.That(queue.Count, Is.EqualTo(4));

            string first = queue.Dequeue();
            string second = queue.Dequeue();

            // Equal priorities don't have a required relative order.
            Assert.That(new[] { first, second }, Is.EquivalentTo(new[] { "lowA", "lowB" }));

            Assert.That(queue.Dequeue(), Is.EqualTo("medium"));
            Assert.That(queue.Dequeue(), Is.EqualTo("high"));
            Assert.That(queue.Count, Is.Zero);

            Assert.That(() => queue.Dequeue(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Reachability_FindsCheaperRouteWithinBudget()
        {
            WorldData world = new WorldData(
                new[]
                {
                new Location(1, "A", new WorldPosition(0, 0), "Test"),
                new Location(2, "B", new WorldPosition(1, 0), "Test"),
                new Location(3, "C", new WorldPosition(0, 1), "Test")
                },
                new[]
                {
                new Connection(1, 2, 10),
                new Connection(1, 3, 2),
                new Connection(3, 2, 2)
                });

            var query = new ReachabilityQuery(world);

            var result = query.FindReachable(1, 4);

            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result[1], Is.EqualTo(0));
            Assert.That(result[3], Is.EqualTo(2));
            Assert.That(result[2], Is.EqualTo(4));
        }
    }


    [TestFixture]
    public sealed class AStarVgTests
    {
        // Small deterministic graph for correctness testing.
        //
        // 1 --10--> 2 --3--> 4
        // |         ^
        // 2         |
        // v         2
        // 3 --------+
        //
        // Also: 1->4 costs 30, 3->4 costs 20,
        // and 2->3 costs 1.
        // Location 5 is disconnected.
        //
        // Cheapest 1->4 route: 1->3->2->4, cost 7.
        private static WorldData CreateTestWorld()
        {
            var locations = new[]
            {
                new Location(1, "Start", new WorldPosition(0, 0), "Test"),
                new Location(2, "Middle", new WorldPosition(1, 0), "Test"),
                new Location(3, "Detour", new WorldPosition(0, 1), "Test"),
                new Location(4, "Goal", new WorldPosition(2, 0), "Test"),
                new Location(5, "Isolated", new WorldPosition(9, 9), "Test")
            };

            var connections = new[]
            {
                new Connection(1, 2, 10),
                new Connection(1, 3, 2),
                new Connection(3, 2, 2),
                new Connection(2, 4, 3),
                new Connection(3, 4, 20),
                new Connection(1, 4, 30),
                new Connection(2, 3, 1)
            };

            return new WorldData(locations, connections);
        }

        // Verify that the returned path uses real connections
        // and that their costs add up to the reported total.
        private static void AssertValidRoute(
            WorldData world,
            RouteResult result,
            int start,
            int goal)
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.LocationIds, Is.Not.Empty);
            Assert.That(result.LocationIds[0], Is.EqualTo(start));
            Assert.That(result.LocationIds[^1], Is.EqualTo(goal));

            int sum = 0;

            for (int i = 0; i < result.LocationIds.Count - 1; i++)
            {
                int from = result.LocationIds[i];
                int to = result.LocationIds[i + 1];

                Connection edge = world.Connections.Single(c =>
                    c.FromLocationId == from &&
                    c.ToLocationId == to);

                sum += edge.TravelCost;
            }

            Assert.That(sum, Is.EqualTo(result.TotalCost));
        }

        // TEST 1: A* must find the cheapest route,
        // not the route with the fewest edges.
        [Test]
        public void AStar_FindsCheaperIndirectRoute()
        {
            WorldData world = CreateTestWorld();
            var finder = new AStarRouteFinder(world);

            RouteResult result = finder.FindRoute(1, 4);

            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.EqualTo(7));
            Assert.That(result.LocationIds,
                Is.EqualTo(new[] { 1, 3, 2, 4 }));

            AssertValidRoute(world, result, 1, 4);
        }

        // TEST 2: Compare A* and Dijkstra on several grid sizes.
        // Also record the number of expanded locations for VG.
        [TestCase(5, 5, 0, 24)]
        [TestCase(20, 20, 0, 399)]
        [TestCase(20, 20, 19, 380)]
        [TestCase(30, 30, 0, 899)]
        public void AStar_MatchesDijkstra_OnGrid(
            int width, int height, int start, int goal)
        {
            WorldData world = WorldDatasetFactory.CreateGrid(width, height);

            var aStar = new AStarRouteFinder(world);
            var dijkstra = new DijkstraRouteFinder(world);

            RouteResult a = aStar.FindRoute(start, goal);
            RouteResult d = dijkstra.FindRoute(start, goal);

            Assert.That(a.Found, Is.True);
            Assert.That(d.Found, Is.True);
            Assert.That(a.TotalCost, Is.EqualTo(d.TotalCost));

            AssertValidRoute(world, a, start, goal);

            TestContext.WriteLine(
                $"{width}x{height} | {start}->{goal} | " +
                $"Cost: {a.TotalCost} | " +
                $"Dijkstra expanded: {d.ExpandedCount} | " +
                $"A* expanded: {a.ExpandedCount}");
        }

        // TEST 3: Compare A* and Dijkstra on the irregular
        // coordinate data from SampleWorldFactory.
        [TestCase(101, 205)]
        [TestCase(101, 734)]
        [TestCase(901, 512)]
        [TestCase(734, 101)]
        public void AStar_MatchesDijkstra_OnSampleWorld(
            int start, int goal)
        {
            WorldData world = SampleWorldFactory.CreateSmall();

            RouteResult a = new AStarRouteFinder(world)
                .FindRoute(start, goal);

            RouteResult d = new DijkstraRouteFinder(world)
                .FindRoute(start, goal);

            Assert.That(a.Found, Is.EqualTo(d.Found));
            Assert.That(a.TotalCost, Is.EqualTo(d.TotalCost));

            AssertValidRoute(world, a, start, goal);

            TestContext.WriteLine(
                $"{start}->{goal} | " +
                $"Dijkstra: {d.ExpandedCount} expansions | " +
                $"A*: {a.ExpandedCount} expansions");
        }

        // TEST 4: A blocked location must not appear in the route.
        [Test]
        public void AStar_BlockedLocation_UsesAlternativeRoute()
        {
            WorldData world = SampleWorldFactory.CreateSmall();
            var finder = new AStarRouteFinder(world);

            // Block Crossroads, location 310.
            RouteResult result = finder.FindRoute(
                101, 734, new[] { 310 }, null);

            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.EqualTo(16));

            Assert.That(result.LocationIds,
                Is.EqualTo(new[] { 101, 205, 901, 734 }));

            Assert.That(result.LocationIds,
                Does.Not.Contain(310));

            AssertValidRoute(world, result, 101, 734);
        }

        // TEST 5: A blocked directed connection must not be used.
        [Test]
        public void AStar_BlockedConnection_FindsAnotherRoute()
        {
            WorldData world = SampleWorldFactory.CreateSmall();
            var finder = new AStarRouteFinder(world);

            // Block Old Mine -> North Gate.
            RouteResult result = finder.FindRoute(
                101, 734, null, new[] { (512, 734) });

            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.EqualTo(15));

            for (int i = 0; i < result.LocationIds.Count - 1; i++)
            {
                var edge = (
                    result.LocationIds[i],
                    result.LocationIds[i + 1]);

                Assert.That(edge, Is.Not.EqualTo((512, 734)));
            }

            AssertValidRoute(world, result, 101, 734);
        }

        // TEST 6: Blocking A->B must not automatically block B->A.
        [Test]
        public void AStar_BlockedConnection_IsDirectional()
        {
            WorldData world = new WorldData(
                new[]
                {
                    new Location(1, "A", new WorldPosition(0, 0), "Test"),
                    new Location(2, "B", new WorldPosition(1, 0), "Test")
                },
                new[]
                {
                    new Connection(1, 2, 2),
                    new Connection(2, 1, 2)
                });

            var finder = new AStarRouteFinder(world);

            var blockedForward = finder.FindRoute(
                1, 2, null, new[] { (1, 2) });

            var allowedReverse = finder.FindRoute(
                2, 1, null, new[] { (1, 2) });

            Assert.That(blockedForward.Found, Is.False);

            Assert.That(allowedReverse.Found, Is.True);
            Assert.That(allowedReverse.TotalCost, Is.EqualTo(2));
        }

        // TEST 7: Constraints must only apply to the current query.
        [Test]
        public void AStar_RestrictionsDoNotPersistBetweenSearches()
        {
            var finder = new AStarRouteFinder(
                SampleWorldFactory.CreateSmall());

            RouteResult restricted = finder.FindRoute(
                101, 734, new[] { 310 }, null);

            RouteResult unrestricted = finder.FindRoute(101, 734);

            Assert.That(restricted.TotalCost, Is.EqualTo(16));
            Assert.That(unrestricted.TotalCost, Is.EqualTo(14));
        }

        // TEST 8: A* with a blocked location should agree with
        // Dijkstra on an equivalent graph with that location isolated.
        [Test]
        public void AStar_ConstrainedRoute_MatchesDijkstra()
        {
            WorldData world = SampleWorldFactory.CreateSmall();

            WorldData filtered = new WorldData(
                world.Locations,
                world.Connections.Where(c =>
                    c.FromLocationId != 310 &&
                    c.ToLocationId != 310));

            RouteResult a = new AStarRouteFinder(world)
                .FindRoute(101, 734, new[] { 310 }, null);

            RouteResult d = new DijkstraRouteFinder(filtered)
                .FindRoute(101, 734);

            Assert.That(a.Found, Is.EqualTo(d.Found));
            Assert.That(a.TotalCost, Is.EqualTo(d.TotalCost));

            TestContext.WriteLine(
                $"Constrained search | " +
                $"Dijkstra expanded: {d.ExpandedCount} | " +
                $"A* expanded: {a.ExpandedCount}");
        }

        // TEST 9: Missing or unreachable destinations.
        [Test]
        public void AStar_UnreachableGoal_ReturnsNotFound()
        {
            var finder = new AStarRouteFinder(CreateTestWorld());

            // Location 5 exists but is disconnected.
            RouteResult result = finder.FindRoute(1, 5);

            Assert.That(result.Found, Is.False);
            Assert.That(result.LocationIds, Is.Empty);
        }

        [Test]
        public void AStar_UnknownGoal_ReturnsNotFound()
        {
            var finder = new AStarRouteFinder(CreateTestWorld());

            RouteResult result = finder.FindRoute(1, 999);

            Assert.That(result.Found, Is.False);
            Assert.That(result.LocationIds, Is.Empty);
        }

        // TEST 10: Blocked starting locations and goals.
        [Test]
        public void AStar_BlockedStartOrGoal_ReturnsNotFound()
        {
            var finder = new AStarRouteFinder(CreateTestWorld());

            RouteResult blockedStart = finder.FindRoute(
                1, 4, new[] { 1 }, null);

            RouteResult blockedGoal = finder.FindRoute(
                1, 4, new[] { 4 }, null);

            Assert.That(blockedStart.Found, Is.False);
            Assert.That(blockedGoal.Found, Is.False);
        }

        // TEST 11: Same start and goal.
        [Test]
        public void AStar_StartEqualsGoal_ReturnsZeroCost()
        {
            var finder = new AStarRouteFinder(CreateTestWorld());

            RouteResult result = finder.FindRoute(1, 1);

            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.Zero);
            Assert.That(result.LocationIds, Is.EqualTo(new[] { 1 }));
            Assert.That(result.ExpandedCount, Is.Zero);
        }

        // TEST 12: Zero-cost connections must work correctly.
        [Test]
        public void AStar_ZeroCostEdges_FindZeroCostRoute()
        {
            WorldData world = new WorldData(
                new[]
                {
                    new Location(1, "A", new WorldPosition(0, 0), "Test"),
                    new Location(2, "B", new WorldPosition(1, 0), "Test"),
                    new Location(3, "C", new WorldPosition(2, 0), "Test")
                },
                new[]
                {
                    new Connection(1, 2, 0),
                    new Connection(2, 3, 0),
                    new Connection(1, 3, 5)
                });

            RouteResult result = new AStarRouteFinder(world)
                .FindRoute(1, 3);

            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.Zero);

            Assert.That(result.LocationIds,
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        // TEST 13: Stale heap entries should not count as expansions.
        [Test]
        public void AStar_StaleEntries_AreNotCountedAsExpansions()
        {
            var finder = new AStarRouteFinder(CreateTestWorld());

            // Location 5 is unreachable, so the search exhausts
            // the frontier, including its outdated entries.
            RouteResult result = finder.FindRoute(1, 5);

            Assert.That(result.Found, Is.False);

            // Locations 1, 2, 3 and 4 are reachable.
            Assert.That(result.ExpandedCount, Is.EqualTo(4));
        }
    }

}
