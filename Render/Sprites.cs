using System.Drawing;

namespace TurDay.Render;

/// <summary>
/// Multi-row sprite. Rows are equal-length. Space ' ' is transparent.
///
/// <para>Optional <see cref="Tints"/> is a parallel grid of palette codes —
/// same shape as <see cref="Rows"/> — letting individual cells override the
/// caller-supplied primary color. Tint codes resolve in this order:</para>
/// <list type="bullet">
///   <item><c>' '</c> or unmatched: use primary fg</item>
///   <item>Brightness modifiers: <c>'-'</c> = 0.65× , <c>'_'</c> = 0.40×,
///         <c>'+'</c> = 1.30×, <c>'*'</c> = 1.60× the primary</item>
///   <item>Anything else: looked up in <see cref="Palette"/>;
///         miss = primary fg</item>
/// </list>
/// </summary>
public sealed record Sprite(string[] Rows, string[]? Tints = null,
                            IReadOnlyDictionary<char, Color>? Palette = null)
{
    public int Width  => Rows[0].Length;
    public int Height => Rows.Length;
}

public static class Sprites
{
    // ───────────── Shared palettes ─────────────
    private static readonly Dictionary<char, Color> TankPalette = new()
    {
        ['T'] = Color.FromArgb(150, 150, 155),  // turret/cannon (steel)
        ['t'] = Color.FromArgb(110, 110, 115),  // cannon shadow side
        ['W'] = Color.FromArgb( 25,  25,  25),  // wheel rims (near-black)
        ['H'] = Color.FromArgb( 50,  60,  35),  // hull edge dark
        ['B'] = Color.FromArgb( 30,  35,  20),  // hull bottom shadow
    };

    private static readonly Dictionary<char, Color> PlanePalette = new()
    {
        ['C'] = Color.FromArgb(220, 100,  60),  // engine cowling (rusty orange)
        ['G'] = Color.FromArgb( 90,  95, 100),  // wing shadow
        ['B'] = Color.FromArgb( 70,  75,  85),  // body shadow underside
        ['M'] = Color.FromArgb(255, 220, 100),  // muzzle flash on tracers
    };

    private static readonly Dictionary<char, Color> SoldierPalette = new()
    {
        ['H'] = Color.FromArgb( 80,  90,  55),  // helmet (darker olive)
        ['F'] = Color.FromArgb(240, 200, 170),  // face skin tone
        ['R'] = Color.FromArgb( 60,  45,  30),  // rifle stock
        ['B'] = Color.FromArgb( 30,  35,  20),  // boots
    };

    private static readonly Dictionary<char, Color> MinePalette = new()
    {
        ['W'] = Color.FromArgb(255, 220,  50),  // warning marker
        ['R'] = Color.FromArgb(220,  60,  60),  // mine body red
        ['D'] = Color.FromArgb( 80,  20,  20),  // mine body shadow
    };

    private static readonly Dictionary<char, Color> WirePalette = new()
    {
        ['B'] = Color.FromArgb( 50,  40,  20),  // dark spike base
        ['R'] = Color.FromArgb(190, 165,  90),  // rusty wire highlight
    };

    private static readonly Dictionary<char, Color> BunkerPalette = new()
    {
        ['C'] = Color.FromArgb(110, 110, 115),  // concrete light
        ['D'] = Color.FromArgb( 50,  50,  55),  // concrete dark
        ['S'] = Color.FromArgb( 25,  25,  30),  // slit / opening
    };

    private static readonly Dictionary<char, Color> TracerPalette = new()
    {
        ['M'] = Color.FromArgb(255, 240, 180),  // hot tip
        ['T'] = Color.FromArgb(255, 180,  60),  // trail
    };

    // ───────────── Turtle 5×3 ─────────────
    // The turtle's primary color comes from the equipped character.
    // Brightness mods carve the shell into a domed shape, legs slightly darker.
    private static readonly string[] TurtleUpTints = new[]
    {
        " ++- ",
        "-=*=-",
        " --- ",
    };
    private static readonly string[] TurtleDownTints = new[]
    {
        " --- ",
        "-=*=-",
        " ++- ",
    };
    private static readonly string[] TurtleSideTints = new[]
    {
        " ++- ",
        "-=*=-",
        " -=- ",
    };
    public static readonly Sprite TurtleUp    = new(new[] { " ▄▄▄ ", "(▒█▒)", " ▀ ▀ " }, TurtleUpTints);
    public static readonly Sprite TurtleDown  = new(new[] { " ▄ ▄ ", "(▒█▒)", " ▀▀▀ " }, TurtleDownTints);
    public static readonly Sprite TurtleLeft  = new(new[] { " ▄▄▄ ", "<▒█▒)", " ▀ ▀ " }, TurtleSideTints);
    public static readonly Sprite TurtleRight = new(new[] { " ▄▄▄ ", "(▒█▒>", " ▀ ▀ " }, TurtleSideTints);

    public static readonly Sprite TurtleHit = new(new[]
    {
        "\\*X*/",
        "*XXX*",
        "/*X*\\",
    });

    // ───────────── Tanks (was cars) 7×3 ─────────────
    public static readonly Sprite TankRight = new(
        new[]
        {
            " ╓════>",
            "[█████]",
            " ●●●●● ",
        },
        Tints: new[]
        {
            " TtTTTT",
            "H+_+_+H",
            " WWWWW ",
        },
        Palette: TankPalette);

    public static readonly Sprite TankLeft = new(
        new[]
        {
            "<════╖ ",
            "[█████]",
            " ●●●●● ",
        },
        Tints: new[]
        {
            "TTTTTtT",
            "H+_+_+H",
            " WWWWW ",
        },
        Palette: TankPalette);

    // ───────────── Planes 5×3, 2-frame banking animation ─────────────
    private static readonly string[] PlaneRightTints = new[]
    {
        "  G  ",
        "BC++M",
        "  G  ",
    };
    private static readonly string[] PlaneLeftTints = new[]
    {
        "  G  ",
        "M++CB",
        "  G  ",
    };
    public static readonly Sprite PlaneRightA = new(new[]{ "  ╱  ", "═█══>", "  ╲  " }, PlaneRightTints, PlanePalette);
    public static readonly Sprite PlaneRightB = new(new[]{ "  ╲  ", "═█══>", "  ╱  " }, PlaneRightTints, PlanePalette);
    public static readonly Sprite PlaneLeftA  = new(new[]{ "  ╱  ", "<══█═", "  ╲  " }, PlaneLeftTints,  PlanePalette);
    public static readonly Sprite PlaneLeftB  = new(new[]{ "  ╲  ", "<══█═", "  ╱  " }, PlaneLeftTints,  PlanePalette);

    // ───────────── Soldiers 5×3 ─────────────
    public static readonly Sprite SoldierRight = new(
        new[]
        {
            " ╓╖  ",
            "(█▓)>",
            " ║║  ",
        },
        Tints: new[]
        {
            " HH  ",
            "-F+-R",
            " BB  ",
        },
        Palette: SoldierPalette);

    public static readonly Sprite SoldierLeft = new(
        new[]
        {
            "  ╓╖ ",
            "<(█▓)",
            "  ║║ ",
        },
        Tints: new[]
        {
            "  HH ",
            "R-+F-",
            "  BB ",
        },
        Palette: SoldierPalette);

    // ───────────── Mine (static hazard) 3×3 ─────────────
    public static readonly Sprite Mine = new(
        new[]
        {
            " ! ",
            "(*)",
            " ¯ ",
        },
        Tints: new[]
        {
            " W ",
            "DRD",
            " D ",
        },
        Palette: MinePalette);

    // ───────────── Tracer rounds 3×3 ─────────────
    public static readonly Sprite TracerRight = new(
        new[] { "   ", "═>>", "   " },
        Tints: new[] { "   ", "TTM", "   " },
        Palette: TracerPalette);
    public static readonly Sprite TracerLeft = new(
        new[] { "   ", "<<═", "   " },
        Tints: new[] { "   ", "MTT", "   " },
        Palette: TracerPalette);

    // ───────────── Barbed wire 3×3 ─────────────
    public static readonly Sprite Wire = new(
        new[]
        {
            "\\X/",
            "X*X",
            "/X\\",
        },
        Tints: new[]
        {
            "BRB",
            "RRR",
            "BRB",
        },
        Palette: WirePalette);

    // ───────────── Bunker 3×2 ─────────────
    public static readonly Sprite Bunker = new(
        new[]
        {
            "╔═╗",
            "║█║",
        },
        Tints: new[]
        {
            "CCC",
            "CSC",
        },
        Palette: BunkerPalette);

    // ───────────── Coin animation ─────────────
    public static readonly char[] CoinFrames = { '$', '*', 'o', '*' };
}
