#nullable enable

using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    /// <summary>
    /// Straightforward queries over the supplied WorldData.
    ///
    /// The starter project intentionally performs these queries directly over the stored
    /// collections. The implementation is correct, but students should analyze
    /// how its work scales as the world becomes larger and queries are repeated.
    ///
    /// Do not assume that every method must be replaced. Identify the operations
    /// that matter first, then justify any representation changes.
    /// </summary>
    public sealed class WorldQueries
    {
        private readonly WorldData _world;

        private readonly Dictionary<int, List<Connection>> _outgoingByLocation = new();

        private readonly Dictionary<int, Location> _locationById = new();

        public WorldQueries(WorldData world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));

            foreach(Location location in _world.Locations)
            {
                _outgoingByLocation.Add(location.Id, new List<Connection>());
                _locationById.Add(location.Id, location);
            }

            foreach(Connection connection in _world.Connections)
            {
                _outgoingByLocation[connection.FromLocationId].Add(connection);
            }
        }

        public Location? FindLocationById(int id)
        {
            if(_locationById.TryGetValue(id, out var location))
                return location;

            return null;
        }

        public IReadOnlyList<Connection> FindOutgoingConnections(int fromLocationId)
        {
            if (_outgoingByLocation.TryGetValue(fromLocationId, out var connections))
            {
                 return connections;
            }

            return Array.Empty<Connection>();
            
        }
    }
}
