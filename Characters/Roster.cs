namespace TurDay.Characters;

public sealed record Character(
    string Id,
    string Name,
    string Glyph,
    string AsciiGlyph,
    ConsoleColor Color,
    int Cost,
    int BonusLives,
    double SpeedMultiplier,
    int FreePassesPerStage,
    string PerkText);

public static class Roster
{
    public static readonly IReadOnlyList<Character> All = new List<Character>
    {
        new("classic",  "Shelly",  "\U0001F422", "@", ConsoleColor.Green,        0, 0, 1.00, 0, "free starter"),
        new("snapper",  "Snapper", "\U0001F422", "S", ConsoleColor.DarkGreen,   50, 1, 1.00, 0, "+1 starting life"),
        new("speedy",   "Zippy",   "\U0001F422", "z", ConsoleColor.Yellow,      75, 0, 0.85, 0, "hazards 15% slower"),
        new("ghost",    "Wraith",  "\U0001F422", "*", ConsoleColor.DarkCyan,   150, 0, 1.00, 1, "1 free hazard hit/stage"),
    };

    public static Character ById(string id) =>
        All.FirstOrDefault(c => c.Id == id) ?? All[0];
}
