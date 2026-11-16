using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    // Assignment Phase 4.

    /// <summary>
    /// Finds minimum-total-cost routes through a Frontier Dispatch world
    /// using Dijkstra's shortest-path algorithm.
    ///
    /// This class is intentionally incomplete in the assignment starter.
    /// It should eventually use the student's prepared world representation
    /// and MinPriorityQueue implementation.
    /// </summary>
    public sealed class DijkstraRouteFinder
    {
        private readonly WorldData _world;
        private readonly WorldQueries _queries;


        /// <summary>
        /// Constructor. Creates a route finder operating on the supplied world.
        /// </summary>
        public DijkstraRouteFinder(WorldData world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _queries = new WorldQueries(_world);
        }

        /// <summary>
        /// Finds a minimum-total-travel-cost route from the start location
        /// to the goal location.
        ///
        /// A successful RouteResult should contain the route as location IDs,
        /// including both the start and goal locations.
        /// </summary>
        public RouteResult FindRoute(int startLocationId, int goalLocationId)
        {
            // TODO: implement Dijkstra using your prepared world representation
            // and MinPriorityQueue<T>.

            if (_queries.FindLocationById(startLocationId) == null || _queries.FindLocationById(goalLocationId) == null)
            {
                return new RouteResult(false, Array.Empty<int>(), 0, 0);
            }

            if (startLocationId == goalLocationId)
            {
                return new RouteResult(true, new[] { startLocationId }, 0, 0);
            }

            var bestCost = new Dictionary<int, int>();
            var cameFrom = new Dictionary<int, int>();
            MinPriorityQueue<int> frontier = new();
            HashSet<int> finalized = new();
            int expandedCount = 0;

            bestCost[startLocationId] = 0;
            frontier.Enqueue(startLocationId, 0);



            while(frontier.Count > 0)
            {
                int current =  frontier.Dequeue();

                if (!finalized.Add(current))
                    continue;

                expandedCount++;

                if (current == goalLocationId)
                {
                    List<int> route = new();
                    route.Add(current);

                    while (current != startLocationId)
                    {
                        current = cameFrom[current];
                        route.Add(current);
                    }

                    route.Reverse();

                    return new RouteResult(true, route, bestCost[goalLocationId], expandedCount);
                }

                foreach (Connection connection in _queries.FindOutgoingConnections(current))
                {
                    int candidateCost = bestCost[current] + connection.TravelCost;

                    if (!bestCost.TryGetValue(connection.ToLocationId, out int knownCost) || candidateCost < knownCost)
                    {
                        bestCost[connection.ToLocationId] = candidateCost;
                        cameFrom[connection.ToLocationId] = current;

                        frontier.Enqueue(connection.ToLocationId, candidateCost);
                    }

                 
                }
            }

            return new RouteResult(false, Array.Empty<int>(), 0, expandedCount);
          
        }
    }
}
