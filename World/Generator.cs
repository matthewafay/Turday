namespace TurDay.World;

public static class Generator
{
    public const int LanesPerStage = 30;     // beach milestone every N lanes

    /// <summary>
    /// Builds a single lane at the given world Y, deterministically from the world seed.
    /// </summary>
    public static Lane BuildLane(int worldY, int width, int worldSeed)
    {
        // First two rows are always safe grass (turtle starts here).
        if (worldY <= 1) return SafeLane(rng: null, width, withCoin: false);

        // Beach milestone at every multiple of LanesPerStage (Y > 0).
        if (worldY > 0 && worldY % LanesPerStage == 0)
        {
            return new Lane
            {
                Kind = TileKind.Beach,
                Hazard = HazardKind.None,
                Direction = 0,
                Speed = 0,
            };
        }

        var rng = new Random(HashSeed(worldSeed, worldY));
        int stage = StageNumberFor(worldY);
        int speedCap = Math.Min(4, 1 + stage / 2);
        int densityBonus = Math.Min(stage - 1, 4);

        // 1 in 6 lanes is a safe grass strip (roughly).
        if (rng.NextDouble() < 0.18)
        {
            return SafeLane(rng, width, withCoin: rng.NextDouble() < 0.45);
        }

        return BuildHazardLane(rng, width, speedCap, densityBonus);
    }

    public static int StageNumberFor(int worldY)
    {
        if (worldY <= 0) return 1;
        return worldY / LanesPerStage + 1;
    }

    public static bool IsBeachLane(int worldY) =>
        worldY > 0 && worldY % LanesPerStage == 0;

    private static int HashSeed(int worldSeed, int worldY)
    {
        unchecked
        {
            int h = worldSeed;
            h = h * 1664525 + worldY;
            h ^= h >> 13;
            h *= 1103515245;
            h ^= h >> 16;
            return h;
        }
    }

    private static Lane SafeLane(Random? rng, int width, bool withCoin)
    {
        var lane = new Lane
        {
            Kind = TileKind.Grass,
            Hazard = HazardKind.None,
            Direction = 0,
            Speed = 0,
        };
        if (withCoin && rng is not null)
        {
            lane.CoinColumn = rng.Next(2, width - 2);
        }
        return lane;
    }

    private static Lane BuildHazardLane(Random rng, int width, int speedCap, int densityBonus)
    {
        // Distribution: tank 28%, plane 18%, soldier 14%, mine 14%, tracer 14%, wire 12%.
        TileKind kind;
        HazardKind hazard;
        int hazardWidth;
        int minSpacing;
        bool isStatic = false;
        int speedOverride = -1;   // <0 means use rng

        var roll = rng.NextDouble();
        if (roll < 0.28)
        {
            kind = TileKind.Road; hazard = HazardKind.Car;
            hazardWidth = 5;
            minSpacing  = 13;
        }
        else if (roll < 0.46)
        {
            kind = TileKind.SkyLane; hazard = HazardKind.Bird;
            hazardWidth = 3;
            minSpacing  = 10;
        }
        else if (roll < 0.60)
        {
            kind = TileKind.DogLane; hazard = HazardKind.Dog;
            hazardWidth = 3;
            minSpacing  = 11;
        }
        else if (roll < 0.74)
        {
            kind = TileKind.Minefield; hazard = HazardKind.Mine;
            hazardWidth = 1;
            minSpacing  = 7;
            isStatic    = true;
        }
        else if (roll < 0.88)
        {
            kind = TileKind.TracerLane; hazard = HazardKind.Tracer;
            hazardWidth = 3;
            minSpacing  = 9;     // bursts: tighter spacing
            speedOverride = Math.Min(5, speedCap + 2); // tracers fly faster than vehicles
        }
        else
        {
            kind = TileKind.WireField; hazard = HazardKind.Wire;
            hazardWidth = 3;
            // Wire spacing must leave gaps the turtle can pass through (footprint 5).
            minSpacing  = 11;    // gap >= 8 columns between wire coils
            isStatic    = true;
        }

        int direction = isStatic ? 0 : (rng.Next(2) == 0 ? -1 : 1);
        int speed = isStatic
            ? 0
            : (speedOverride > 0 ? speedOverride : Math.Max(1, rng.Next(1, speedCap + 1)));

        // Target spacing for this lane, clamped to the per-kind minimum.
        int target = rng.Next(minSpacing + 4, minSpacing + 12) - densityBonus;
        int spacing = Math.Max(minSpacing, target);

        // Need N hazards such that N*spacing >= visible-plus-margin width.
        // visibleSpan = width + 2*(hazardWidth + 4). Hazards uniformly spaced over Period = N*spacing.
        int visibleSpan = width + 2 * (hazardWidth + 4);
        int n = Math.Max(2, (int)Math.Ceiling((double)visibleSpan / spacing));
        int period = n * spacing;

        var lane = new Lane
        {
            Kind = kind,
            Hazard = hazard,
            Direction = direction,
            Speed = speed,
            HazardWidth = hazardWidth,
            Period = period,
        };

        int phase = rng.Next(spacing);
        int leftEdge = -hazardWidth - 4;
        for (int i = 0; i < n; i++)
        {
            lane.HazardPositions.Add(leftEdge + phase + i * spacing);
        }
        return lane;
    }
}
