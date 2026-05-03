namespace TurDay.World;

public sealed class Lane
{
    public TileKind Kind { get; init; }
    public HazardKind Hazard { get; init; }
    public int Direction { get; init; } // -1 left, +1 right, 0 static
    public int Speed { get; init; }     // tiles per tick (1..4)
    public int HazardWidth { get; init; } = 2;
    public int Period { get; init; }    // wrap period (= count × spacing); 0 = no wrap
    public List<int> HazardPositions { get; } = new();
    public int? CoinColumn { get; set; }

    public bool IsSafe => Kind == TileKind.Grass || Kind == TileKind.Beach;

    public void Step(int width)
    {
        if (Direction == 0 || Speed == 0 || Period <= 0) return;
        // Each hazard moves by Direction*Speed and wraps modulo Period, keeping
        // a uniform spacing across all hazards.
        int leftEdge  = -HazardWidth - 4;
        int rightEdge = leftEdge + Period;
        for (int i = 0; i < HazardPositions.Count; i++)
        {
            var p = HazardPositions[i] + Direction * Speed;
            int rel = ((p - leftEdge) % Period + Period) % Period;
            HazardPositions[i] = leftEdge + rel;
        }
    }

    public bool OccupiesColumn(int col)
    {
        if (Hazard == HazardKind.None) return false;
        foreach (var pos in HazardPositions)
        {
            if (col >= pos && col < pos + HazardWidth) return true;
        }
        return false;
    }

    public bool OccupiesRange(int col0, int col1Inclusive)
    {
        if (Hazard == HazardKind.None) return false;
        foreach (var pos in HazardPositions)
        {
            int hazEnd = pos + HazardWidth - 1;
            if (hazEnd >= col0 && pos <= col1Inclusive) return true;
        }
        return false;
    }
}
