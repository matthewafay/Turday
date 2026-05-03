namespace TurDay.Render;

/// <summary>
/// Multi-row sprite. Rows are equal-length. Space ' ' is treated as transparent.
/// Block chars (█▀▄▌▐░▒▓) render as filled cells in the foreground color.
/// </summary>
public sealed record Sprite(string[] Rows)
{
    public int Width  => Rows[0].Length;
    public int Height => Rows.Length;
}

public static class Sprites
{
    // ───────────── Turtle 5×3 ─────────────
    public static readonly Sprite TurtleUp = new(new[]
    {
        " ▄▄▄ ",
        "(▒█▒)",
        " ▀ ▀ ",
    });
    public static readonly Sprite TurtleDown = new(new[]
    {
        " ▄ ▄ ",
        "(▒█▒)",
        " ▀▀▀ ",
    });
    public static readonly Sprite TurtleLeft = new(new[]
    {
        " ▄▄▄ ",
        "<▒█▒)",
        " ▀ ▀ ",
    });
    public static readonly Sprite TurtleRight = new(new[]
    {
        " ▄▄▄ ",
        "(▒█▒>",
        " ▀ ▀ ",
    });
    public static readonly Sprite TurtleHit = new(new[]
    {
        "\\*X*/",
        "*XXX*",
        "/*X*\\",
    });

    // ───────────── Tanks (was cars) 7×3 ─────────────
    public static readonly Sprite TankRight = new(new[]
    {
        " ╓════>",
        "[█████]",
        " ●●●●● ",
    });
    public static readonly Sprite TankLeft = new(new[]
    {
        "<════╖ ",
        "[█████]",
        " ●●●●● ",
    });

    // ───────────── Planes (was birds) 5×3, 2-frame banking animation ─────────────
    public static readonly Sprite PlaneRightA = new(new[]
    {
        "  ╱  ",
        "═█══>",
        "  ╲  ",
    });
    public static readonly Sprite PlaneRightB = new(new[]
    {
        "  ╲  ",
        "═█══>",
        "  ╱  ",
    });
    public static readonly Sprite PlaneLeftA = new(new[]
    {
        "  ╱  ",
        "<══█═",
        "  ╲  ",
    });
    public static readonly Sprite PlaneLeftB = new(new[]
    {
        "  ╲  ",
        "<══█═",
        "  ╱  ",
    });

    // ───────────── Soldiers (was dogs) 5×3 ─────────────
    public static readonly Sprite SoldierRight = new(new[]
    {
        " ╓╖  ",
        "(█▓)>",
        " ║║  ",
    });
    public static readonly Sprite SoldierLeft = new(new[]
    {
        "  ╓╖ ",
        "<(█▓)",
        "  ║║ ",
    });

    // ───────────── Mine (static hazard) 3×3 ─────────────
    public static readonly Sprite Mine = new(new[]
    {
        " ! ",
        "(*)",
        " ¯ ",
    });

    // ───────────── Tracer rounds (fast 1-row projectiles) 3×3 ─────────────
    public static readonly Sprite TracerRight = new(new[]
    {
        "   ",
        "═>>",
        "   ",
    });
    public static readonly Sprite TracerLeft = new(new[]
    {
        "   ",
        "<<═",
        "   ",
    });

    // ───────────── Barbed wire (static blocker) 3×3 ─────────────
    public static readonly Sprite Wire = new(new[]
    {
        "\\X/",
        "X*X",
        "/X\\",
    });

    // ───────────── Bunker (concrete pillbox on the beach) 3×2 ─────────────
    public static readonly Sprite Bunker = new(new[]
    {
        "╔═╗",
        "║█║",
    });

    // ───────────── Coin animation ─────────────
    public static readonly char[] CoinFrames = { '$', '*', 'o', '*' };
}
