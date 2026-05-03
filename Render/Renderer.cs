using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using TurDay.Entities;
using TurDay.World;

namespace TurDay.Render;

public sealed class Renderer
{
    public const int LaneRows = 3;     // each logical lane spans this many cell rows on screen
    public const int CellWidth = 14;
    public const int CellHeight = 22;
    public const int FontSize = 16;

    private readonly Font _font;
    private readonly Font _hudFont;
    private readonly Font _titleFont;
    private Graphics? _g;
    private int _animTick;
    private readonly Random _shakeRng = new(1);

    private static readonly TextFormatFlags TextFlags =
        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
        TextFormatFlags.SingleLine | TextFormatFlags.HorizontalCenter |
        TextFormatFlags.VerticalCenter;

    public Renderer()
    {
        _font = new Font("Consolas", FontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        _hudFont = new Font("Consolas", 28, FontStyle.Bold, GraphicsUnit.Pixel);
        _titleFont = new Font("Consolas", 56, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    public const int HudRows = 2;     // HUD reserves this many cell rows on screen
    public const int TitleRows = 4;   // big-title text reserves this many cell rows

    public int PixelWidth(int cells)  => cells * CellWidth;
    public int PixelHeight(int cells) => cells * CellHeight;

    public void BeginFrame(Graphics g, int widthCells, int heightCells, int shakeTicks = 0)
    {
        _g = g;
        _g.SmoothingMode    = System.Drawing.Drawing2D.SmoothingMode.None;
        _g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
        _animTick++;
        _g.FillRectangle(Brushes.Black, 0, 0, widthCells * CellWidth, heightCells * CellHeight);
        if (shakeTicks > 0)
        {
            int mag = Math.Min(6, shakeTicks);
            int sx = _shakeRng.Next(-mag, mag + 1);
            int sy = _shakeRng.Next(-mag, mag + 1);
            _g.TranslateTransform(sx, sy);
        }
    }

    public void EndFrame()
    {
        _g?.ResetTransform();
        _g = null;
    }

    public int AnimTick => _animTick;

    // ───── World / lanes ─────

    public void DrawWorld(GameWorld world, int cameraY, int visibleLanes, Turtle turtle)
    {
        for (int v = visibleLanes - 1; v >= 0; v--)
        {
            int worldY = cameraY + v;
            int screenRowTop = (visibleLanes - 1 - v) * LaneRows;
            var lane = world.GetLane(worldY);
            DrawLaneBackground(lane, world.Width, screenRowTop, worldY);
            DrawLaneHazards(lane, screenRowTop);
            DrawLaneCoin(lane, screenRowTop);
        }

        int turtleVisualY = turtle.Y - cameraY;
        if (turtleVisualY >= 0 && turtleVisualY < visibleLanes)
        {
            int turtleScreenRow = (visibleLanes - 1 - turtleVisualY) * LaneRows;
            DrawTurtle(turtle, turtleScreenRow);
        }
    }

    private void DrawLaneBackground(Lane lane, int width, int screenRowTop, int worldY)
    {
        if (lane.Kind == TileKind.Beach)
        {
            DrawBeachLane(width, screenRowTop, worldY);
            return;
        }

        Color bg = LaneBgColor(lane.Kind);
        FillRect(0, screenRowTop, width, LaneRows, bg);

        for (int dr = 0; dr < LaneRows; dr++)
        {
            int r = screenRowTop + dr;
            for (int col = 0; col < width; col++)
            {
                AppendBackgroundCell(lane.Kind, col, r, dr, worldY, lane.Direction);
            }
        }
    }

    private void AppendBackgroundCell(TileKind kind, int col, int row, int laneRowIndex,
                                      int stageSeed, int laneDir)
    {
        int hash = unchecked(col * 73 + row * 31 + stageSeed * 17);
        switch (kind)
        {
            case TileKind.Grass:
            {
                // Sandbagged fortified strip
                int bucket = hash & 15;
                char ch;
                Color color;
                if (bucket < 2)        { ch = '▓'; color = Color.FromArgb(120, 130,  80); }
                else if (bucket < 4)   { ch = '▒'; color = Color.FromArgb(100, 110,  70); }
                else if (bucket < 7)   { ch = '='; color = Color.FromArgb(140, 130,  70); }
                else if (bucket < 9)   { ch = ','; color = Color.FromArgb( 80,  90,  50); }
                else if (bucket == 9)  { ch = '#'; color = Color.FromArgb(150, 140,  90); }
                else                   { ch = ' '; color = Color.Empty; }
                if (ch != ' ') DrawCell(row, col, ch, color, Color.Empty);
                break;
            }
            case TileKind.Road:
            {
                // Muddy battlefield road. Center row has tank tread marks scrolling.
                if (laneRowIndex == 1)
                {
                    int phase = ((col + (laneDir >= 0 ? -_animTick / 2 : _animTick / 2)) % 4 + 4) % 4;
                    if (phase < 2) DrawCell(row, col, '≡', Color.FromArgb(150, 130, 60), Color.Empty);
                }
                else
                {
                    int g = hash & 31;
                    if (g == 0)      DrawCell(row, col, '·', Color.FromArgb( 90,  80,  60), Color.Empty);
                    else if (g == 1) DrawCell(row, col, 'o', Color.FromArgb( 60,  50,  35), Color.Empty); // crater
                    else if (g == 2) DrawCell(row, col, ',', Color.FromArgb( 80,  70,  50), Color.Empty);
                }
                break;
            }
            case TileKind.SkyLane:
            {
                // Smoky war sky with occasional artillery flash
                int driftedCol = (col + _animTick / 8) % 23;
                int cloudHash = unchecked((driftedCol + row * 11 + stageSeed * 5)) & 31;
                if (cloudHash == 0)       DrawCell(row, col, '▒', Color.FromArgb(160, 160, 170), Color.Empty);
                else if (cloudHash == 1)  DrawCell(row, col, '░', Color.FromArgb(130, 130, 140), Color.Empty);

                // rare artillery flash
                int flash = unchecked(col * 11 + row * 17 + stageSeed * 13 + _animTick / 5);
                if ((flash & 255) == 7) DrawCell(row, col, '*', Color.FromArgb(255, 220,  80), Color.Empty);
                break;
            }
            case TileKind.DogLane:
            {
                // No-man's-land: deep mud, craters, barbed-wire wisps
                int b = hash & 15;
                if (b == 0)       DrawCell(row, col, '·', Color.FromArgb(120, 100, 70), Color.Empty);
                else if (b == 1)  DrawCell(row, col, ',', Color.FromArgb(100,  80, 55), Color.Empty);
                else if (b == 2)  DrawCell(row, col, 'o', Color.FromArgb( 80,  65, 40), Color.Empty); // crater
                else if (b == 3)  DrawCell(row, col, 'x', Color.FromArgb(150, 130, 80), Color.Empty); // wire
                else if (b == 4)  DrawCell(row, col, '░', Color.FromArgb( 90,  75, 50), Color.Empty);
                break;
            }
            case TileKind.Minefield:
            {
                int b = hash & 31;
                if (b == 0)       DrawCell(row, col, '·', Color.FromArgb(180, 160, 110), Color.Empty);
                else if (b == 1)  DrawCell(row, col, ',', Color.FromArgb(160, 140, 100), Color.Empty);
                else if (b == 2)  DrawCell(row, col, '\'', Color.FromArgb(170, 150, 100), Color.Empty);
                break;
            }
            case TileKind.TracerLane:
            {
                // Dark sky with bright tracer streaks; faint horizon glow
                int b = hash & 15;
                if (b == 0)       DrawCell(row, col, '·', Color.FromArgb(120, 110, 90), Color.Empty);
                int driftedCol = (col + _animTick / 4) & 31;
                if (driftedCol == 0) DrawCell(row, col, '·', Color.FromArgb(255, 200, 80), Color.Empty);
                break;
            }
            case TileKind.WireField:
            {
                // Muddy ground covered in barbed wire grain
                int b = hash & 15;
                if (b == 0)       DrawCell(row, col, '·', Color.FromArgb(110,  95,  60), Color.Empty);
                else if (b == 1)  DrawCell(row, col, ',', Color.FromArgb( 95,  80,  50), Color.Empty);
                else if (b == 2)  DrawCell(row, col, 'x', Color.FromArgb(140, 120,  70), Color.Empty);
                else if (b == 3)  DrawCell(row, col, '░', Color.FromArgb( 90,  75,  45), Color.Empty);
                break;
            }
        }
    }

    private static Color LaneBgColor(TileKind kind) => kind switch
    {
        TileKind.Grass      => Color.FromArgb( 60,  65,  40),
        TileKind.Road       => Color.FromArgb( 50,  42,  30),
        TileKind.SkyLane    => Color.FromArgb( 90,  95, 105),
        TileKind.DogLane    => Color.FromArgb( 65,  50,  30),
        TileKind.Minefield  => Color.FromArgb(110,  90,  60),
        TileKind.TracerLane => Color.FromArgb( 60,  55,  45),  // dusk firing-line sky
        TileKind.WireField  => Color.FromArgb( 70,  60,  40),  // mud with wire
        TileKind.Beach      => Color.FromArgb( 25,  60, 120),
        _                   => Color.Black,
    };

    private void DrawBeachLane(int width, int screenRowTop, int stageSeed)
    {
        // Three rows: smoky distant sea, banner on sand with bunkers, sand with debris
        FillRect(0, screenRowTop, width, 1, Color.FromArgb( 35,  60, 100));
        for (int col = 0; col < width; col++)
        {
            int hash = unchecked(col * 91 + screenRowTop * 13 + stageSeed * 17 + _animTick / 3);
            int b = hash & 7;
            if (b == 0)      DrawCell(screenRowTop, col, '▒', Color.FromArgb(140, 150, 165), Color.Empty); // smoke on horizon
            else if (b == 1) DrawCell(screenRowTop, col, '~', Color.FromArgb(110, 140, 170), Color.Empty);
            else if (b == 2) DrawCell(screenRowTop, col, '*', Color.FromArgb(255, 180,  60), Color.Empty); // distant flash
        }

        // Sand row 1 (background + label) + Sand row 2 (debris)
        FillRect(0, screenRowTop + 1, width, 1, Color.FromArgb(190, 160,  95));
        FillRect(0, screenRowTop + 2, width, 1, Color.FromArgb(180, 150,  85));

        // Place bunkers as proper sprites (with outline + shadow). Skip the banner area.
        const string label = "  BEACHHEAD  ";
        int labelPad = Math.Max(0, (width - label.Length) / 2);
        var bunkerColor = Color.FromArgb(140, 140, 145);
        for (int bunkerStart = 1; bunkerStart + Sprites.Bunker.Width <= width; bunkerStart += 14)
        {
            // Skip if the bunker would overlap the BEACHHEAD label
            int labelEnd = labelPad + label.Length;
            if (bunkerStart < labelEnd && bunkerStart + Sprites.Bunker.Width > labelPad) continue;
            AppendSprite(Sprites.Bunker, bunkerStart, screenRowTop + 1, bunkerColor);
        }

        // Banner text on top of sand row 1
        for (int col = labelPad; col < labelPad + label.Length && col < width; col++)
        {
            char ch = label[col - labelPad];
            if (ch != ' ') DrawCell(screenRowTop + 1, col, ch, Color.FromArgb(255, 230, 80), Color.Empty);
        }

        // Sand-grain decoration on row 1 (skip bunker columns)
        for (int col = 0; col < width; col++)
        {
            if (IsBunkerColumn(col, width)) continue;
            if (col >= labelPad && col < labelPad + label.Length) continue;
            int hash = unchecked(col * 53 + (screenRowTop + 1) * 11);
            if ((hash & 7) == 0) DrawCell(screenRowTop + 1, col, '·', Color.FromArgb(160, 130, 70), Color.Empty);
        }

        // Debris on row 2 (skip bunker columns)
        for (int col = 0; col < width; col++)
        {
            if (IsBunkerColumn(col, width)) continue;
            int hash = unchecked(col * 53 + (screenRowTop + 2) * 17 + stageSeed * 23);
            int b = hash & 31;
            if (b == 0)      DrawCell(screenRowTop + 2, col, 'x', Color.FromArgb(110, 100, 60), Color.Empty);
            else if (b == 1) DrawCell(screenRowTop + 2, col, '·', Color.FromArgb(150, 120, 70), Color.Empty);
            else if (b == 2) DrawCell(screenRowTop + 2, col, ',', Color.FromArgb(130, 110, 60), Color.Empty);
        }
    }

    private static bool IsBunkerColumn(int col, int width)
    {
        const string label = "  BEACHHEAD  ";
        int labelPad = Math.Max(0, (width - label.Length) / 2);
        int labelEnd = labelPad + label.Length;
        for (int bunkerStart = 1; bunkerStart + Sprites.Bunker.Width <= width; bunkerStart += 14)
        {
            if (bunkerStart < labelEnd && bunkerStart + Sprites.Bunker.Width > labelPad) continue;
            if (col >= bunkerStart && col < bunkerStart + Sprites.Bunker.Width) return true;
        }
        return false;
    }

    // ───── Hazards ─────

    private void DrawLaneHazards(Lane lane, int screenRowTop)
    {
        if (lane.Hazard == HazardKind.None) return;
        foreach (var pos in lane.HazardPositions)
        {
            DrawHazardSprite(lane, pos, screenRowTop);
        }
    }

    private void DrawHazardSprite(Lane lane, int pos, int screenRowTop)
    {
        Sprite sprite = lane.Hazard switch
        {
            HazardKind.Car    => lane.Direction >= 0 ? Sprites.TankRight   : Sprites.TankLeft,
            HazardKind.Bird   => SelectPlane(lane.Direction),
            HazardKind.Dog    => lane.Direction >= 0 ? Sprites.SoldierRight: Sprites.SoldierLeft,
            HazardKind.Mine   => Sprites.Mine,
            HazardKind.Tracer => lane.Direction >= 0 ? Sprites.TracerRight : Sprites.TracerLeft,
            HazardKind.Wire   => Sprites.Wire,
            _                 => Sprites.TankRight,
        };
        var fg = HazardColor(lane.Hazard);
        AppendSprite(sprite, pos, screenRowTop, fg);
    }

    private Sprite SelectPlane(int direction)
    {
        bool bank = (_animTick / 4) % 2 == 0;
        if (direction >= 0)
            return bank ? Sprites.PlaneRightA : Sprites.PlaneRightB;
        return bank ? Sprites.PlaneLeftA : Sprites.PlaneLeftB;
    }

    private static Color HazardColor(HazardKind kind) => kind switch
    {
        HazardKind.Car    => Color.FromArgb( 90, 110,  70),
        HazardKind.Bird   => Color.FromArgb(190, 190, 200),
        HazardKind.Dog    => Color.FromArgb(160, 170, 100),
        HazardKind.Mine   => Color.FromArgb(220,  80,  60),
        HazardKind.Tracer => Color.FromArgb(255, 200,  80),  // bright tracer streak
        HazardKind.Wire   => Color.FromArgb(170, 150,  80),  // rusted wire
        _                 => Color.LightGray,
    };

    // ───── Coin ─────

    private void DrawLaneCoin(Lane lane, int screenRowTop)
    {
        if (lane.CoinColumn is not int col) return;
        char frame = Sprites.CoinFrames[(_animTick / 3) % Sprites.CoinFrames.Length];
        bool blink = (_animTick / 2) % 2 == 0;
        Color fg = blink ? Color.FromArgb(255, 220, 60) : Color.FromArgb(255, 255, 200);
        DrawCell(screenRowTop + LaneRows - 2, col, frame, fg, Color.Empty);
    }

    // ───── Turtle ─────

    private void DrawTurtle(Turtle turtle, int screenRowTop)
    {
        Sprite sprite;
        Color fg;
        if (turtle.HitFlashTicks > 0)
        {
            sprite = Sprites.TurtleHit;
            fg = Color.FromArgb(255, 80, 80);
        }
        else
        {
            sprite = turtle.Facing switch
            {
                Facing.Up    => Sprites.TurtleUp,
                Facing.Down  => Sprites.TurtleDown,
                Facing.Left  => Sprites.TurtleLeft,
                Facing.Right => Sprites.TurtleRight,
                _            => Sprites.TurtleUp,
            };
            fg = ConsoleColorToColor(turtle.Character.Color);
        }
        int leftX = turtle.X - sprite.Width / 2;
        AppendSprite(sprite, leftX, screenRowTop, fg);
    }

    // ───── Sprite painter ─────

    private void AppendSprite(Sprite sprite, int leftX, int screenRowTop, Color fg)
    {
        // Pass 0: drop shadow. Strips on edges that extend past the silhouette
        // toward the bottom-right, plus a small corner at outer corners.
        const int shadowSize = 4;
        using (var shadowBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
        {
            for (int dr = 0; dr < sprite.Height; dr++)
            {
                string row = sprite.Rows[dr];
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i] == ' ') continue;
                    int col = leftX + i;
                    if (col < 0) continue;
                    int x = col * CellWidth;
                    int y = (screenRowTop + dr) * CellHeight;
                    bool rightSpace  = (i == row.Length - 1)   || sprite.Rows[dr][i + 1] == ' ';
                    bool bottomSpace = (dr == sprite.Height-1) || sprite.Rows[dr + 1][i] == ' ';
                    if (rightSpace)
                        _g!.FillRectangle(shadowBrush, x + CellWidth, y + shadowSize, shadowSize, CellHeight);
                    if (bottomSpace)
                        _g!.FillRectangle(shadowBrush, x + shadowSize, y + CellHeight, CellWidth, shadowSize);
                    if (rightSpace && bottomSpace)
                        _g!.FillRectangle(shadowBrush, x + CellWidth, y + CellHeight, shadowSize, shadowSize);
                }
            }
        }

        // Pass 1: silhouette outline. For each non-space cell, draw edges adjacent to space.
        var outlineColor = Darken(fg, 0.30);
        using (var pen = new Pen(outlineColor, 2f))
        {
            for (int dr = 0; dr < sprite.Height; dr++)
            {
                string row = sprite.Rows[dr];
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i] == ' ') continue;
                    int col = leftX + i;
                    if (col < 0) continue;
                    int x = col * CellWidth;
                    int y = (screenRowTop + dr) * CellHeight;

                    bool leftSpace   = (i == 0)                || sprite.Rows[dr][i - 1] == ' ';
                    bool rightSpace  = (i == row.Length - 1)   || sprite.Rows[dr][i + 1] == ' ';
                    bool topSpace    = (dr == 0)               || sprite.Rows[dr - 1][i] == ' ';
                    bool bottomSpace = (dr == sprite.Height-1) || sprite.Rows[dr + 1][i] == ' ';

                    if (leftSpace)   _g!.DrawLine(pen, x,             y,                  x,             y + CellHeight);
                    if (rightSpace)  _g!.DrawLine(pen, x + CellWidth, y,                  x + CellWidth, y + CellHeight);
                    if (topSpace)    _g!.DrawLine(pen, x,             y,                  x + CellWidth, y);
                    if (bottomSpace) _g!.DrawLine(pen, x,             y + CellHeight - 1, x + CellWidth, y + CellHeight - 1);
                }
            }
        }

        // Pass 2: characters on top
        for (int dr = 0; dr < sprite.Height; dr++)
        {
            string line = sprite.Rows[dr];
            for (int i = 0; i < line.Length; i++)
            {
                int col = leftX + i;
                if (col < 0) continue;
                char ch = line[i];
                if (ch == ' ') continue;
                DrawCell(screenRowTop + dr, col, ch, fg, Color.Empty);
            }
        }
    }

    private static Color Darken(Color c, double factor)
    {
        factor = Math.Clamp(factor, 0.0, 1.0);
        return Color.FromArgb(
            (int)(c.R * factor),
            (int)(c.G * factor),
            (int)(c.B * factor));
    }

    // ───── HUD / centered text ─────

    public void DrawHud(int row, Turtle turtle, int stageNumber, int score, int highScore, int width)
    {
        // HUD reserves 2 cell rows. Background fill, then big-font text on top.
        FillRect(0, row, width, HudRows, Color.FromArgb(25, 25, 22));
        // Subtle separator line above the HUD
        FillRect(0, row, width, 1, Color.FromArgb(220, 60, 60, 50));

        int x = 8;                           // pixel padding from the left edge
        int y = row * CellHeight;
        int hudPxHeight = HudRows * CellHeight;

        x = DrawHudSegment(x, y, hudPxHeight, "DIST ",                  Color.FromArgb(220, 200, 140));
        x = DrawHudSegment(x, y, hudPxHeight, score.ToString("N0"),      Color.FromArgb(255, 220, 60));
        x = DrawHudSegment(x, y, hudPxHeight, "m",                       Color.Gray);
        x = DrawHudSegment(x, y, hudPxHeight, "  BEST ",                Color.Gray);
        x = DrawHudSegment(x, y, hudPxHeight, highScore.ToString("N0"), Color.FromArgb(220, 220, 220));
        x = DrawHudSegment(x, y, hudPxHeight, "   WAVE ",               Color.FromArgb(220, 200, 140));
        x = DrawHudSegment(x, y, hudPxHeight, stageNumber.ToString(),    Color.LightGreen);
        x = DrawHudSegment(x, y, hudPxHeight, "   UNITS ",              Color.FromArgb(220, 200, 140));
        x = DrawHudSegment(x, y, hudPxHeight, new string('@', Math.Max(0, turtle.Lives)), Color.Tomato);
        x = DrawHudSegment(x, y, hudPxHeight, "   MEDALS ",             Color.FromArgb(220, 200, 140));
        x = DrawHudSegment(x, y, hudPxHeight, turtle.Coins.ToString(),   Color.FromArgb(255, 220, 60));
        x = DrawHudSegment(x, y, hudPxHeight, "   ",                     Color.White);
        DrawHudSegment(x, y, hudPxHeight, turtle.Character.Name,         ConsoleColorToColor(turtle.Character.Color));
    }

    private int DrawHudSegment(int x, int y, int height, string text, Color color)
    {
        // Use TextRenderer.MeasureText for accurate advance with the HUD font
        var size = TextRenderer.MeasureText(_g!, text, _hudFont, new Size(int.MaxValue, height),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        var rect = new Rectangle(x, y, size.Width, height);
        TextRenderer.DrawText(_g!, text, _hudFont, rect, color, Color.Transparent,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
        return x + size.Width;
    }

    public void DrawCenteredLine(int row, int width, string text, Color color)
    {
        int pad = Math.Max(0, (width - text.Length) / 2);
        DrawText(row, pad, text, color);
    }

    public void DrawCenteredLine(int row, int width, string text, ConsoleColor color)
        => DrawCenteredLine(row, width, text, ConsoleColorToColor(color));

    /// <summary>Draw text centered horizontally with the bigger HUD font; spans HudRows cell rows.</summary>
    public void DrawCenteredBig(int row, int widthCells, string text, Color color)
    {
        var size = TextRenderer.MeasureText(_g!, text, _hudFont, new Size(int.MaxValue, HudRows * CellHeight),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        int x = (widthCells * CellWidth - size.Width) / 2;
        int y = row * CellHeight;
        var rect = new Rectangle(x, y, size.Width, HudRows * CellHeight);
        TextRenderer.DrawText(_g!, text, _hudFont, rect, color, Color.Transparent,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
    }

    public void DrawCenteredBig(int row, int widthCells, string text, ConsoleColor color)
        => DrawCenteredBig(row, widthCells, text, ConsoleColorToColor(color));

    /// <summary>Draw text centered with the title font; spans TitleRows cell rows.</summary>
    public void DrawCenteredTitle(int row, int widthCells, string text, Color color)
    {
        var size = TextRenderer.MeasureText(_g!, text, _titleFont, new Size(int.MaxValue, TitleRows * CellHeight),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        int x = (widthCells * CellWidth - size.Width) / 2;
        int y = row * CellHeight;
        var rect = new Rectangle(x, y, size.Width, TitleRows * CellHeight);
        TextRenderer.DrawText(_g!, text, _titleFont, rect, color, Color.Transparent,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
    }

    // ───── primitives ─────

    public int DrawText(int row, int col, string text, Color color)
    {
        int x = col * CellWidth;
        int y = row * CellHeight;
        var rect = new Rectangle(x, y, text.Length * CellWidth, CellHeight);
        TextRenderer.DrawText(_g!, text, _font, rect, color, Color.Transparent, TextFlags);
        return col + text.Length;
    }

    public void DrawCell(int row, int col, char ch, Color fg, Color bg)
    {
        int x = col * CellWidth;
        int y = row * CellHeight;
        if (bg.A != 0 && !bg.IsEmpty)
        {
            using var brush = new SolidBrush(bg);
            _g!.FillRectangle(brush, x, y, CellWidth, CellHeight);
        }
        var rect = new Rectangle(x, y, CellWidth, CellHeight);
        TextRenderer.DrawText(_g!, ch.ToString(), _font, rect, fg, Color.Transparent, TextFlags);
    }

    public void FillRect(int col, int row, int widthCells, int heightCells, Color color)
    {
        if (color.IsEmpty || color.A == 0) return;
        using var brush = new SolidBrush(color);
        _g!.FillRectangle(brush, col * CellWidth, row * CellHeight,
                          widthCells * CellWidth, heightCells * CellHeight);
    }

    public void DrawRect(int col, int row, int widthCells, int heightCells, Color color)
    {
        if (color.IsEmpty || color.A == 0) return;
        using var pen = new Pen(color, 2f);
        _g!.DrawRectangle(pen, col * CellWidth, row * CellHeight,
                          widthCells * CellWidth - 1, heightCells * CellHeight - 1);
    }

    public static Color ConsoleColorToColor(ConsoleColor c) => c switch
    {
        ConsoleColor.Black       => Color.Black,
        ConsoleColor.DarkRed     => Color.FromArgb(160, 40, 40),
        ConsoleColor.DarkGreen   => Color.FromArgb( 60,130, 60),
        ConsoleColor.DarkYellow  => Color.FromArgb(180,140, 40),
        ConsoleColor.DarkBlue    => Color.FromArgb( 40, 60,140),
        ConsoleColor.DarkMagenta => Color.FromArgb(140, 60,140),
        ConsoleColor.DarkCyan    => Color.FromArgb( 40,140,160),
        ConsoleColor.Gray        => Color.FromArgb(200,200,200),
        ConsoleColor.DarkGray    => Color.FromArgb(110,110,110),
        ConsoleColor.Red         => Color.FromArgb(240, 80, 80),
        ConsoleColor.Green       => Color.FromArgb( 90,220, 90),
        ConsoleColor.Yellow      => Color.FromArgb(255,220, 60),
        ConsoleColor.Blue        => Color.FromArgb( 80,140,255),
        ConsoleColor.Magenta     => Color.FromArgb(220,100,220),
        ConsoleColor.Cyan        => Color.FromArgb(120,220,240),
        ConsoleColor.White       => Color.White,
        _                        => Color.Gray,
    };
}
