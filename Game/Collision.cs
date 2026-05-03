using TurDay.Entities;
using TurDay.World;

namespace TurDay.Game;

public static class Collision
{
    /// <summary>True if the lane at laneY would block movement at the given turtle X.</summary>
    public static bool IsBlocked(GameWorld world, int laneY, int x)
    {
        var lane = world.GetLane(laneY);
        if (lane.Hazard != HazardKind.Wire) return false;
        return lane.OccupiesRange(x - Turtle.HalfWidth, x + Turtle.HalfWidth);
    }

    public static bool TurtleHitHazard(Turtle turtle, GameWorld world)
    {
        var lane = world.GetLane(turtle.Y);
        if (lane.Hazard == HazardKind.None || lane.Hazard == HazardKind.Wire) return false;
        return lane.OccupiesRange(turtle.X - Turtle.HalfWidth, turtle.X + Turtle.HalfWidth);
    }

    public static bool TurtleOnCoin(Turtle turtle, GameWorld world)
    {
        var lane = world.GetLane(turtle.Y);
        if (lane.CoinColumn is not int cc) return false;
        return cc >= turtle.X - Turtle.HalfWidth && cc <= turtle.X + Turtle.HalfWidth;
    }

    public static bool TurtleOnBeach(Turtle turtle, GameWorld world)
        => Generator.IsBeachLane(turtle.Y);
}
