using System.Collections.Generic;

namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Graph representing connectivity between regions via passages.
/// </summary>
public class RegionGraph
{
    private readonly Dictionary<int, List<RegionConnection>> _connections = new();
    private readonly List<Region> _regions;

    public IReadOnlyList<Region> Regions => _regions;

    public RegionGraph(List<Region> regions, List<Passage> passages)
    {
        _regions = regions;

        // Initialize connection lists
        foreach (var region in regions)
        {
            _connections[region.Id] = new List<RegionConnection>();
        }

        // Build connections from passages
        foreach (var passage in passages)
        {
            if (!_connections.ContainsKey(passage.RegionAId))
                _connections[passage.RegionAId] = new List<RegionConnection>();
            if (!_connections.ContainsKey(passage.RegionBId))
                _connections[passage.RegionBId] = new List<RegionConnection>();

            var connectionAtoB = new RegionConnection
            {
                FromRegionId = passage.RegionAId,
                ToRegionId = passage.RegionBId,
                PassageId = passage.Id,
                Distance = passage.Length
            };

            var connectionBtoA = new RegionConnection
            {
                FromRegionId = passage.RegionBId,
                ToRegionId = passage.RegionAId,
                PassageId = passage.Id,
                Distance = passage.Length
            };

            _connections[passage.RegionAId].Add(connectionAtoB);
            _connections[passage.RegionBId].Add(connectionBtoA);
        }
    }

    /// <summary>
    /// Get all connections from a region.
    /// </summary>
    public IReadOnlyList<RegionConnection> GetConnections(int regionId)
    {
        if (_connections.TryGetValue(regionId, out var connections))
            return connections;
        return new List<RegionConnection>();
    }

    /// <summary>
    /// Find shortest path between two regions (BFS).
    /// Returns list of region IDs in path order.
    /// </summary>
    public IReadOnlyList<int> FindPath(int fromRegionId, int toRegionId)
    {
        if (fromRegionId == toRegionId)
            return new List<int> { fromRegionId };

        var visited = new HashSet<int>();
        var parent = new Dictionary<int, int>();
        var queue = new Queue<int>();

        queue.Enqueue(fromRegionId);
        visited.Add(fromRegionId);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            foreach (var connection in GetConnections(current))
            {
                int next = connection.ToRegionId;

                if (visited.Contains(next))
                    continue;

                visited.Add(next);
                parent[next] = current;

                if (next == toRegionId)
                {
                    // Reconstruct path
                    var path = new List<int>();
                    int node = toRegionId;
                    while (node != fromRegionId)
                    {
                        path.Add(node);
                        node = parent[node];
                    }
                    path.Add(fromRegionId);
                    path.Reverse();
                    return path;
                }

                queue.Enqueue(next);
            }
        }

        return new List<int>(); // No path found
    }

    /// <summary>
    /// Check if all regions are connected.
    /// </summary>
    public bool IsFullyConnected()
    {
        if (_regions.Count == 0)
            return true;

        var visited = new HashSet<int>();
        var queue = new Queue<int>();

        queue.Enqueue(_regions[0].Id);
        visited.Add(_regions[0].Id);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            foreach (var connection in GetConnections(current))
            {
                if (!visited.Contains(connection.ToRegionId))
                {
                    visited.Add(connection.ToRegionId);
                    queue.Enqueue(connection.ToRegionId);
                }
            }
        }

        return visited.Count == _regions.Count;
    }

    /// <summary>
    /// Breadth-first iteration from a starting region.
    /// </summary>
    public IEnumerable<int> BreadthFirstFrom(int startRegionId)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<int>();

        queue.Enqueue(startRegionId);
        visited.Add(startRegionId);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            yield return current;

            foreach (var connection in GetConnections(current))
            {
                if (!visited.Contains(connection.ToRegionId))
                {
                    visited.Add(connection.ToRegionId);
                    queue.Enqueue(connection.ToRegionId);
                }
            }
        }
    }
}

/// <summary>
/// Connection between two regions.
/// </summary>
public class RegionConnection
{
    public int FromRegionId { get; init; }
    public int ToRegionId { get; init; }
    public int PassageId { get; init; }
    public int Distance { get; init; }
}
