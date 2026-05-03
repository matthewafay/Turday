using System.Drawing;
using TurDay.Characters;
using TurDay.Entities;
using TurDay.Render;
using TurDay.Save;
using TurDay.World;

namespace TurDay.Game;

public enum Mode { Title, CharacterSelect, Play, Paused, GameOver }

public sealed class GameApp
{
    public const int Width = 70;                 // cell columns
    public const int VisibleLanes = 11;          // lanes shown on screen at once
    public const int VisualPlayRows = VisibleLanes * Renderer.LaneRows; // 33 rows
    public const int HudRow = VisualPlayRows;
    public const int FlashRow = HudRow + 1;
    public const int ScreenHeight = FlashRow + 1; // 35 rows

    /// <summary>Visual row (from bottom) the camera tries to keep the turtle at when scrolling.</summary>
    private const int TurtlePinRow = 4;

    private readonly Random _rng;
    private readonly Renderer _renderer;
    private SaveData _save;
    private Mode _mode = Mode.Title;

    private GameWorld? _world;
    private Turtle? _turtle;
    private int _cameraY;            // bottom-most lane currently on screen (world Y)
    private int _bestRowEver;        // farthest world Y the turtle has reached this run
    private int _stagesCleared;      // number of beach milestones banked this run
    private int _menuCursor;
    private int _charCursor;
    private string _flash = "";
    private int _flashTicks;

    private int _score;
    private bool _newHighScoreThisRun;

    private double _hazardAccumMs;
    private int _shakeTicks;

    private readonly int _debugStartY;

    public GameApp(Renderer renderer, int? seed = null, int debugStartY = 0)
    {
        _renderer = renderer;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        _save = SaveStore.Load();
        _debugStartY = Math.Max(0, debugStartY);
    }

    public bool Tick(double elapsedMs, Intent intent)
    {
        switch (_mode)
        {
            case Mode.Title:           return HandleTitle(intent);
            case Mode.CharacterSelect: return HandleCharacterSelect(intent);
            case Mode.Play:            return HandlePlay(intent, elapsedMs);
            case Mode.Paused:          return HandlePaused(intent);
            case Mode.GameOver:        return HandleGameOver(intent);
        }
        return false;
    }

    public void Render(Graphics g)
    {
        _renderer.BeginFrame(g, Width, ScreenHeight, _shakeTicks);
        switch (_mode)
        {
            case Mode.Title:           DrawTitle(); break;
            case Mode.CharacterSelect: DrawCharacterSelect(); break;
            case Mode.Play:            DrawPlay(); break;
            case Mode.Paused:          DrawPlay(); DrawPausedOverlay(); break;
            case Mode.GameOver:        DrawPlay(); DrawGameOverOverlay(); break;
        }
        if (_flashTicks > 0)
        {
            _renderer.DrawCenteredLine(FlashRow, Width, _flash, ConsoleColor.Cyan);
            _flashTicks--;
        }
        _renderer.EndFrame();
    }

    // ───── Title ─────
    private static readonly string[] TitleOptions = { "Play", "Characters", "Reset Save", "Quit" };

    private bool HandleTitle(Intent intent)
    {
        switch (intent)
        {
            case Intent.Up:    _menuCursor = (_menuCursor - 1 + TitleOptions.Length) % TitleOptions.Length; break;
            case Intent.Down:  _menuCursor = (_menuCursor + 1) % TitleOptions.Length; break;
            case Intent.Quit:  return true;
            case Intent.Confirm:
                switch (_menuCursor)
                {
                    case 0: StartRun(); break;
                    case 1: _mode = Mode.CharacterSelect; _charCursor = IndexOfCurrentCharacter(); break;
                    case 2: ResetSave(); break;
                    case 3: return true;
                }
                break;
        }
        return false;
    }

    private int IndexOfCurrentCharacter()
    {
        for (int i = 0; i < Roster.All.Count; i++)
            if (Roster.All[i].Id == _save.CurrentCharacter) return i;
        return 0;
    }

    private void ResetSave()
    {
        _save = new SaveData();
        SaveStore.Save(_save);
        Flash("Save reset.");
    }

    private void Flash(string msg)
    {
        _flash = msg;
        _flashTicks = 30;
    }

    // ───── Character select ─────
    private bool HandleCharacterSelect(Intent intent)
    {
        switch (intent)
        {
            case Intent.Up:
            case Intent.Left:
                _charCursor = (_charCursor - 1 + Roster.All.Count) % Roster.All.Count; break;
            case Intent.Down:
            case Intent.Right:
                _charCursor = (_charCursor + 1) % Roster.All.Count; break;
            case Intent.Pause:
            case Intent.Quit:
                _mode = Mode.Title; break;
            case Intent.Confirm:
                var c = Roster.All[_charCursor];
                if (_save.Unlocked.Contains(c.Id))
                {
                    _save.CurrentCharacter = c.Id;
                    SaveStore.Save(_save);
                    Flash($"Selected {c.Name}.");
                }
                else if (_save.Coins >= c.Cost)
                {
                    _save.Coins -= c.Cost;
                    _save.Unlocked.Add(c.Id);
                    _save.CurrentCharacter = c.Id;
                    SaveStore.Save(_save);
                    Flash($"Unlocked {c.Name}!");
                }
                else
                {
                    Flash($"Need {c.Cost - _save.Coins} more coins.");
                }
                break;
        }
        return false;
    }

    // ───── Play ─────
    private void StartRun()
    {
        var character = Roster.ById(_save.CurrentCharacter);
        _world = new GameWorld(Width, _rng.Next());
        int startY = _debugStartY;
        _turtle = new Turtle(character, Width / 2, startY);
        _cameraY = Math.Max(0, startY - 4);
        _bestRowEver = startY;
        _stagesCleared = startY / Generator.LanesPerStage;
        _hazardAccumMs = 0;
        _score = 0;
        _newHighScoreThisRun = false;
        _mode = Mode.Play;
    }

    private bool HandlePlay(Intent intent, double elapsedMs)
    {
        if (_world is null || _turtle is null) { _mode = Mode.Title; return false; }

        int minX = Turtle.HalfWidth;
        int maxX = _world.Width - 1 - Turtle.HalfWidth;
        switch (intent)
        {
            case Intent.Up:
                _turtle.Facing = Facing.Up;
                if (!Collision.IsBlocked(_world, _turtle.Y + 1, _turtle.X)) _turtle.Y += 1;
                break;
            case Intent.Down:
                _turtle.Facing = Facing.Down;
                int targetDownY = Math.Max(_cameraY, _turtle.Y - 1);
                if (!Collision.IsBlocked(_world, targetDownY, _turtle.X)) _turtle.Y = targetDownY;
                break;
            case Intent.Left:
                _turtle.Facing = Facing.Left;
                int targetLeftX = Math.Max(minX, _turtle.X - 1);
                if (!Collision.IsBlocked(_world, _turtle.Y, targetLeftX)) _turtle.X = targetLeftX;
                break;
            case Intent.Right:
                _turtle.Facing = Facing.Right;
                int targetRightX = Math.Min(maxX, _turtle.X + 1);
                if (!Collision.IsBlocked(_world, _turtle.Y, targetRightX)) _turtle.X = targetRightX;
                break;
            case Intent.Pause: _mode = Mode.Paused; return false;
            case Intent.Quit:  EndRun("Quit run."); return false;
        }

        // Track the farthest row reached this run; +1 score per new max row.
        if (_turtle.Y > _bestRowEver)
        {
            _score += (_turtle.Y - _bestRowEver);
            _bestRowEver = _turtle.Y;
            if (_score > _save.HighScore) _newHighScoreThisRun = true;

            // Beach milestone reached?
            int stageJustCleared = _bestRowEver / Generator.LanesPerStage;
            if (stageJustCleared > _stagesCleared && Generator.IsBeachLane(_bestRowEver))
            {
                _stagesCleared = stageJustCleared;
                int bonus = 25 * stageJustCleared;
                _score += bonus;
                if (_score > _save.HighScore) { _save.HighScore = _score; _newHighScoreThisRun = true; }
                if (stageJustCleared > _save.BestStage) _save.BestStage = stageJustCleared;
                SaveStore.Save(_save);
                Flash($"Stage {stageJustCleared} cleared!  +{bonus}");
            }
        }

        // Camera follows the turtle — pin to TurtlePinRow when scrolling forward;
        // never scroll backward (turtle just moves down within the visible window).
        int desiredCameraY = _turtle.Y - TurtlePinRow;
        if (desiredCameraY > _cameraY)
        {
            _cameraY = desiredCameraY;
            _world.TrimBelow(_cameraY - 12);
        }

        if (_turtle.HitFlashTicks > 0) _turtle.HitFlashTicks--;
        if (_shakeTicks > 0) _shakeTicks--;

        // Hazard pacing — speedy character slows the world.
        double slowdown = _turtle.Character.SpeedMultiplier;
        double hazardStepMs = 200.0 / Math.Max(0.1, slowdown);
        _hazardAccumMs += elapsedMs;
        while (_hazardAccumMs >= hazardStepMs)
        {
            _hazardAccumMs -= hazardStepMs;
            _world.StepHazards();
        }

        if (Collision.TurtleHitHazard(_turtle, _world))
        {
            if (_turtle.FreePassesLeft > 0)
            {
                _turtle.FreePassesLeft--;
                _turtle.HitFlashTicks = 4;
                _shakeTicks = 4;
                Flash("Phase! Free pass used.");
            }
            else
            {
                _turtle.Lives--;
                _turtle.HitFlashTicks = 8;
                _shakeTicks = 10;
                Flash("Hit!");
                if (_turtle.Lives <= 0) { EndRun("KIA. Mission failed."); return false; }
                _turtle.Y = Math.Max(_cameraY, _turtle.Y - 4);
                _turtle.X = _world.Width / 2;
            }
        }
        if (Collision.TurtleOnCoin(_turtle, _world))
        {
            _world.GetLane(_turtle.Y).CoinColumn = null;
            _turtle.Coins++;
        }

        return false;
    }

    private void EndRun(string reason)
    {
        if (_turtle is not null)
        {
            _save.Coins += _turtle.Coins;
            int reachedStage = StageNumber();
            if (reachedStage > _save.BestStage) _save.BestStage = reachedStage;
            if (_score > _save.HighScore) _save.HighScore = _score;
            SaveStore.Save(_save);
        }
        Flash(reason);
        _mode = Mode.GameOver;
    }

    private int StageNumber() => _turtle is null ? 1 : Generator.StageNumberFor(_bestRowEver);

    private bool HandlePaused(Intent intent)
    {
        if (intent == Intent.Pause || intent == Intent.Confirm) _mode = Mode.Play;
        if (intent == Intent.Quit) EndRun("Quit run.");
        return false;
    }

    private bool HandleGameOver(Intent intent)
    {
        if (intent == Intent.Confirm || intent == Intent.Pause || intent == Intent.Quit)
        {
            _world = null;
            _turtle = null;
            _mode = Mode.Title;
        }
        return false;
    }

    // ───── Drawing ─────

    private void DrawTitle()
    {
        _renderer.DrawCenteredLine(3,  Width, "==============================================", ConsoleColor.DarkYellow);
        _renderer.DrawCenteredLine(4,  Width, "|                                            |", ConsoleColor.DarkYellow);
        _renderer.DrawCenteredLine(5,  Width, "|              T U R D A Y                   |", ConsoleColor.Yellow);
        _renderer.DrawCenteredLine(6,  Width, "|       OPERATION  SHELLSTORM                |", ConsoleColor.Red);
        _renderer.DrawCenteredLine(7,  Width, "|     storming the beach against all odds    |", ConsoleColor.DarkYellow);
        _renderer.DrawCenteredLine(8,  Width, "==============================================", ConsoleColor.DarkYellow);

        _renderer.DrawCenteredLine(11, Width, $"HIGH SCORE   {_save.HighScore}", ConsoleColor.Yellow);
        _renderer.DrawCenteredLine(12, Width, $"coins  {_save.Coins}     best stage  {_save.BestStage}", ConsoleColor.DarkGray);

        for (int i = 0; i < TitleOptions.Length; i++)
        {
            string prefix = (_menuCursor == i) ? "> " : "  ";
            _renderer.DrawCenteredLine(15 + i, Width, prefix + TitleOptions[i] + (_menuCursor == i ? " <" : "  "),
                _menuCursor == i ? ConsoleColor.White : ConsoleColor.Gray);
        }

        _renderer.DrawCenteredLine(ScreenHeight - 3, Width,
            "[Up/Down] move    [Enter] select    [Esc/Q] quit", ConsoleColor.DarkGray);
    }

    private void DrawCharacterSelect()
    {
        _renderer.DrawCenteredLine(2, Width, "── CHARACTERS ──", ConsoleColor.White);
        _renderer.DrawCenteredLine(3, Width, $"Coins available: {_save.Coins}", ConsoleColor.Yellow);

        for (int i = 0; i < Roster.All.Count; i++)
        {
            var c = Roster.All[i];
            bool unlocked = _save.Unlocked.Contains(c.Id);
            bool active = _save.CurrentCharacter == c.Id;
            string marker = i == _charCursor ? "> " : "  ";
            string status = active ? "[EQUIPPED]" : unlocked ? "[unlocked]" : $"[{c.Cost} coins]";
            string line = $"{marker}{c.Name,-10}  {status,-12}  {c.PerkText}";
            var color = i == _charCursor ? ConsoleColor.White
                       : unlocked ? ConsoleColor.Green
                       : ConsoleColor.DarkGray;
            _renderer.DrawCenteredLine(7 + i * 2, Width, line, color);
        }

        _renderer.DrawCenteredLine(ScreenHeight - 3, Width,
            "[Up/Down] cycle    [Enter] equip / unlock    [Esc] back", ConsoleColor.DarkGray);
    }

    private void DrawPlay()
    {
        if (_world is null || _turtle is null) return;
        _renderer.DrawWorld(_world, _cameraY, VisibleLanes, _turtle);
        _renderer.DrawHud(HudRow, _turtle, StageNumber(), _score, _save.HighScore, Width);
    }

    private void DrawPausedOverlay()
    {
        int row = VisualPlayRows / 2;
        _renderer.DrawCenteredLine(row,     Width, "── PAUSED ──", ConsoleColor.White);
        _renderer.DrawCenteredLine(row + 2, Width, "[Esc/Enter] resume    [Q] end run", ConsoleColor.DarkGray);
    }

    private void DrawGameOverOverlay()
    {
        int row = VisualPlayRows / 2;
        _renderer.DrawCenteredLine(row - 2, Width, "── RUN OVER ──", ConsoleColor.Red);
        _renderer.DrawCenteredLine(row,     Width, $"SCORE  {_score}",
            _newHighScoreThisRun ? ConsoleColor.Yellow : ConsoleColor.White);
        if (_newHighScoreThisRun)
            _renderer.DrawCenteredLine(row + 2, Width, "*  NEW HIGH SCORE  *", ConsoleColor.Yellow);
        else
            _renderer.DrawCenteredLine(row + 2, Width, $"high score: {_save.HighScore}", ConsoleColor.DarkGray);
        _renderer.DrawCenteredLine(row + 3, Width, $"Reached stage {StageNumber()}   +{_turtle?.Coins ?? 0} coins", ConsoleColor.Gray);
        _renderer.DrawCenteredLine(row + 5, Width, "[Enter] back to title", ConsoleColor.DarkGray);
    }
}
