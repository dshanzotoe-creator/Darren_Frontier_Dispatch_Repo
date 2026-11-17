
#nullable enable

using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    public sealed class AStarRouteFinder
    {
        private readonly WorldQueries _queries;
        private readonly double _minCostPerUnit;

        public AStarRouteFinder(WorldData world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            _queries = new WorldQueries(world);

            // Calculate a safe minimum travel cost per
            // unit of Manhattan distance.
            double minRate = double.PositiveInfinity;

            foreach (Connection connection in world.Connections)
            {
                Location from = _queries.FindLocationById(
                    connection.FromLocationId)
                    ?? throw new ArgumentException("Unknown connection origin.");

                Location to = _queries.FindLocationById(
                    connection.ToLocationId)
                    ?? throw new ArgumentException("Unknown connection destination.");

                double distance = ManhattanDistance(
                    from.Position, to.Position);

                if (distance > 0)
                {
                    double rate = connection.TravelCost / distance;
                    minRate = Math.Min(minRate, rate);
                }
            }

            // A zero heuristic is safe if no usable
            // geometric cost bound exists.
            _minCostPerUnit = double.IsPositiveInfinity(minRate)
                ? 0
                : minRate;
        }

        // Manhattan distance between two coordinates.
        private static double ManhattanDistance(
            WorldPosition from,
            WorldPosition goal)
        {
            double xDistance = Math.Abs((double)from.X - goal.X);
            double yDistance = Math.Abs((double)from.Y - goal.Y);

            return xDistance + yDistance;
        }

        // Retrieve the positions and calculate their distance.
        private double GetDistanceToGoal(
            int locationId,
            int goalLocationId)
        {
            Location location = _queries.FindLocationById(locationId)
                ?? throw new ArgumentException("Unknown location.");

            Location goal = _queries.FindLocationById(goalLocationId)
                ?? throw new ArgumentException("Unknown goal.");

            return ManhattanDistance(location.Position, goal.Position);
        }

        // Estimate the remaining travel cost: h(n).
        private int EstimateRemainingCost(
            int locationId,
            int goalLocationId)
        {
            double distance = GetDistanceToGoal(
                locationId, goalLocationId);

            double estimate = distance * _minCostPerUnit;

            return (int)Math.Floor(estimate);
        }

        // Ordinary A* without restrictions.
        public RouteResult FindRoute(
            int startLocationId,
            int goalLocationId)
        {
            return FindRoute(
                startLocationId,
                goalLocationId,
                null,
                null);
        }

        // A* with optional traversal constraints.
        public RouteResult FindRoute(int startLocationId, int goalLocationId, IEnumerable<int>? blockedLocationIds,IEnumerable<(int From, int To)>? blockedConnections)
        {
            HashSet<int> blockedLocations = blockedLocationIds == null ? new HashSet<int>() : new HashSet<int>(blockedLocationIds);

            HashSet<(int From, int To)> blockedEdges = blockedConnections == null? new HashSet<(int From, int To)>() : new HashSet<(int From, int To)>(blockedConnections);

            if (_queries.FindLocationById(startLocationId) == null ||
                _queries.FindLocationById(goalLocationId) == null ||
                blockedLocations.Contains(startLocationId) ||
                blockedLocations.Contains(goalLocationId))
            {
                return new RouteResult(false, Array.Empty<int>(), 0, 0);
            }

            if (startLocationId == goalLocationId)
            {
                return new RouteResult(true,new[] { startLocationId },0,0);
            }

            var bestCost = new Dictionary<int, int>();

            var cameFrom = new Dictionary<int, int>();


            MinPriorityQueue<(int LocationId, int CostSoFar)> frontier = new();

            int expandedCount = 0;

            // Initialize the start.
            bestCost[startLocationId] = 0;

            int startPriority = EstimateRemainingCost(startLocationId, goalLocationId);

            frontier.Enqueue((startLocationId, 0), startPriority);

 
            while (frontier.Count > 0)
            {
                var entry = frontier.Dequeue();

                int current = entry.LocationId;
                int currentCost = entry.CostSoFar;


                if (currentCost != bestCost[current])
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

                    return new RouteResult( true, route, bestCost[goalLocationId], expandedCount);
                }

                foreach (Connection connection in _queries.FindOutgoingConnections(current))
                {
                    if (blockedLocations.Contains(connection.ToLocationId))
                        continue;

                    if (blockedEdges.Contains(( connection.FromLocationId, connection.ToLocationId)))
                    {
                        continue;
                    }

                    // Calculate actual travel cost: g(n).
                    int candidateCost = bestCost[current] + connection.TravelCost;

                    if (!bestCost.TryGetValue(connection.ToLocationId, out int knownCost)|| candidateCost < knownCost)
                    {
                        bestCost[connection.ToLocationId] = candidateCost;

                        cameFrom[connection.ToLocationId] = current;

                        int estimatedRemaining = EstimateRemainingCost(connection.ToLocationId, goalLocationId);

                        int priority = candidateCost + estimatedRemaining;

                       
                        frontier.Enqueue((connection.ToLocationId, candidateCost), priority);
                    }
                }
            }

            return new RouteResult(false,Array.Empty<int>(),0,expandedCount);
        }
    }
}
