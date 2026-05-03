namespace TurDay.World;

public enum TileKind
{
    Empty,
    Grass,
    Road,
    SkyLane,
    DogLane,
    Minefield,
    TracerLane,
    WireField,
    Beach,
}

public enum HazardKind
{
    None,
    Car,
    Bird,
    Dog,
    Mine,
    Tracer,
    Wire,
}

public static class Tiles
{
    public const char GrassChar = '.';
    public const char RoadChar  = '-';
    public const char SkyChar   = ' ';
    public const char DogChar   = '_';
    public const char BeachSand = '~';
    public const char BeachWord = '~'; // background fill
    public const char CoinChar  = 'o';

    public static ConsoleColor LaneBackground(TileKind kind) => ConsoleColor.Black;

    public static ConsoleColor LaneForeground(TileKind kind) => kind switch
    {
        TileKind.Grass     => ConsoleColor.DarkGreen,
        TileKind.Road      => ConsoleColor.DarkGray,
        TileKind.SkyLane   => ConsoleColor.DarkBlue,
        TileKind.DogLane   => ConsoleColor.DarkYellow,
        TileKind.Minefield => ConsoleColor.Yellow,
        TileKind.Beach     => ConsoleColor.Yellow,
        _                  => ConsoleColor.Gray,
    };

    public static char LaneFiller(TileKind kind) => kind switch
    {
        TileKind.Grass     => GrassChar,
        TileKind.Road      => RoadChar,
        TileKind.SkyLane   => SkyChar,
        TileKind.DogLane   => DogChar,
        TileKind.Minefield => '.',
        TileKind.Beach     => BeachSand,
        _                  => ' ',
    };
}
