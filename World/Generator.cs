namespace TurDay.World;

public static class Generator
{
    public const int LanesPerStage = 30;     // beach milestone every N lanes

    /// <summary>
    /// Builds a single lane at the given world Y, deterministically from the world seed.
    /// </summary>
    public static Lane BuildLane(int worldY, int width, int worldSeed)
    {
        // First three rows are always safe grass — gives the player room to think on launch.
        if (worldY <= 2) return SafeLane(rng: null, width, withCoin: false);

        // Beach milestone at every multiple of LanesPerStage.
        if (IsBeachLane(worldY))
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
        int speedCap = SpeedCapForStage(stage);
        int densityBonus = DensityBonusForStage(stage);
        double safeChance = SafeLaneChance(stage);

        if (rng.NextDouble() < safeChance)
        {
            return SafeLane(rng, width, withCoin: rng.NextDouble() < 0.45);
        }

        return BuildHazardLane(rng, width, speedCap, densityBonus, stage);
    }

    public static int StageNumberFor(int worldY)
    {
        if (worldY <= 0) return 1;
        return worldY / LanesPerStage + 1;
    }

    public static bool IsBeachLane(int worldY) =>
        worldY > 0 && worldY % LanesPerStage == 0;

    // ─── Difficulty curve ───────────────────────────────────────────────
    // Designed so stage 1 is genuinely a warm-up: tanks only, slow, lots of
    // safe grass strips. Each subsequent stage introduces one new hazard
    // type and tightens spacing/speed slightly.

    private static int SpeedCapForStage(int stage) => stage switch
    {
        1 => 1,
        2 => 2,
        3 or 4 => 2,
        5 or 6 => 3,
        _      => 4,
    };

    private static int DensityBonusForStage(int stage) =>
        Math.Clamp((stage - 3) / 2, 0, 4);
        // Stage 1-3: 0 (target spacing as generated)
        // Stage 4-5: 1
        // Stage 6-7: 2
        // Stage 8-9: 3
        // Stage 10+: 4 (max compression)

    private static double SafeLaneChance(int stage) => stage switch
    {
        1 => 0.45,
        2 => 0.34,
        3 => 0.26,
        4 => 0.22,
        _ => 0.18,
    };

    /// <summary>
    /// Pool of hazard kinds available at this stage. Each new stage unlocks one more.
    /// </summary>
    private static (TileKind kind, HazardKind hazard, int width, int minSpacing, bool isStatic, double weight)[]
        HazardPool(int stage)
    {
        var pool = new List<(TileKind, HazardKind, int, int, bool, double)>
        {
            // stage 1: just tanks
            (TileKind.Road, HazardKind.Car, 5, 13, false, 1.00),
        };
        if (stage >= 2) pool.Add((TileKind.SkyLane,    HazardKind.Bird,   3, 10, false, 0.70));
        if (stage >= 3) pool.Add((TileKind.DogLane,    HazardKind.Dog,    3, 11, false, 0.55));
        if (stage >= 4) pool.Add((TileKind.Minefield,  HazardKind.Mine,   1,  7, true,  0.45));
        if (stage >= 5) pool.Add((TileKind.TracerLane, HazardKind.Tracer, 3,  9, false, 0.45));
        if (stage >= 6) pool.Add((TileKind.WireField,  HazardKind.Wire,   3, 11, true,  0.40));
        return pool.ToArray();
    }

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

    private static Lane BuildHazardLane(Random rng, int width, int speedCap, int densityBonus, int stage)
    {
        // Pick a hazard kind from the stage's pool, weighted.
        var pool = HazardPool(stage);
        double totalWeight = 0;
        for (int i = 0; i < pool.Length; i++) totalWeight += pool[i].weight;
        double roll = rng.NextDouble() * totalWeight;
        var pick = pool[0];
        for (int i = 0; i < pool.Length; i++)
        {
            if (roll < pool[i].weight) { pick = pool[i]; break; }
            roll -= pool[i].weight;
        }
        var (kind, hazard, hazardWidth, minSpacing, isStatic, _) = pick;

        // Tracer lanes get a speed override above the regular speed cap.
        int speedOverride = (hazard == HazardKind.Tracer) ? Math.Min(5, speedCap + 2) : -1;

        int direction = isStatic ? 0 : (rng.Next(2) == 0 ? -1 : 1);
        int speed = isStatic
            ? 0
            : (speedOverride > 0 ? speedOverride : Math.Max(1, rng.Next(1, speedCap + 1)));

        // Target spacing for this lane, clamped to the per-kind minimum.
        int target = rng.Next(minSpacing + 4, minSpacing + 12) - densityBonus;
        int spacing = Math.Max(minSpacing, target);

        // Need N hazards such that N*spacing >= visible-plus-margin width.
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
