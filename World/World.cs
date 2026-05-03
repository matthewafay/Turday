namespace TurDay.World;

/// <summary>
/// Infinite procedural world. Lanes are generated lazily and cached.
/// World Y grows upward; turtle starts at Y=0.
/// </summary>
public sealed class GameWorld
{
    private readonly Dictionary<int, Lane> _lanes = new();
    public int Width { get; }
    public int Seed { get; }

    public GameWorld(int width, int seed)
    {
        Width = width;
        Seed = seed;
    }

    public Lane GetLane(int worldY)
    {
        if (_lanes.TryGetValue(worldY, out var lane)) return lane;
        lane = Generator.BuildLane(worldY, Width, Seed);
        _lanes[worldY] = lane;
        return lane;
    }

    public IEnumerable<Lane> StepAllLanes()
    {
        foreach (var lane in _lanes.Values) yield return lane;
    }

    /// <summary>Step every cached lane's hazards once.</summary>
    public void StepHazards()
    {
        foreach (var lane in _lanes.Values) lane.Step(Width);
    }

    /// <summary>Drop lanes that are far below the camera (turtle can't reach them).</summary>
    public void TrimBelow(int minRetainedY)
    {
        if (_lanes.Count < 64) return; // cheap fast-path
        var toRemove = new List<int>();
        foreach (var key in _lanes.Keys)
            if (key < minRetainedY) toRemove.Add(key);
        foreach (var k in toRemove) _lanes.Remove(k);
    }
}
