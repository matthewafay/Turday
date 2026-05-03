using System.Diagnostics;
using System.Windows.Forms;
using TurDay.Render;

namespace TurDay.Game;

public sealed class GameForm : Form
{
    private const int TickMs = 33;            // ~30Hz update
    private const int MaxIntentsPerTick = 8;

    private readonly GameApp _app;
    private readonly Renderer _renderer;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Queue<Intent> _pending = new();          // for one-shot keys (Enter/Esc/Q)
    private readonly HashSet<Keys> _heldKeys = new();          // continuous-fire keys (arrows/WASD)
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _lastTickTicks;

    public GameForm(int? seed = null, int debugStartY = 0)
    {
        Text = "TurDay";
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = System.Drawing.Color.Black;
        KeyPreview = true;

        _renderer = new Renderer();
        _app = new GameApp(_renderer, seed, debugStartY);

        ClientSize = new System.Drawing.Size(
            GameApp.Width * Renderer.CellWidth,
            GameApp.ScreenHeight * Renderer.CellHeight);

        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        _timer = new System.Windows.Forms.Timer { Interval = TickMs };
        _timer.Tick += OnTick;
        _timer.Start();

        _lastTickTicks = _stopwatch.ElapsedTicks;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // .Add returns true if the key was newly added (i.e., this is a real first press,
        // not an OS auto-repeat). All keys flow through _heldKeys for repeat suppression.
        bool firstPress = _heldKeys.Add(e.KeyCode);

        var intent = InputReader.FromKey(e.KeyCode);
        if (intent != Intent.None && firstPress)
        {
            // Fire once on first press for everything (so taps feel instant).
            // Continuous keys also get re-fired by OnTick while still held.
            _pending.Enqueue(intent);
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _heldKeys.Remove(e.KeyCode);
        base.OnKeyUp(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _heldKeys.Clear();
        base.OnLostFocus(e);
    }

    // Lateral keys (L/R) auto-repeat while held — that's what gives the snappy strafing.
    // Forward/back (U/D) intentionally do NOT auto-repeat: pressing UP once = one lane forward,
    // so you can't accidentally leap into a wall of hazards.
    private static bool IsLateralRepeating(Intent i) => i == Intent.Left || i == Intent.Right;

    private void OnTick(object? sender, EventArgs e)
    {
        var now = _stopwatch.ElapsedTicks;
        double elapsedMs = (now - _lastTickTicks) * 1000.0 / Stopwatch.Frequency;
        _lastTickTicks = now;

        // Each tick: held LATERAL keys fire one extra intent. Bypasses OS initial-repeat delay.
        foreach (var key in _heldKeys)
        {
            var intent = InputReader.FromKey(key);
            if (intent != Intent.None && IsLateralRepeating(intent))
                _pending.Enqueue(intent);
        }

        if (_pending.Count == 0)
        {
            if (_app.Tick(elapsedMs, Intent.None)) { Close(); return; }
        }
        else
        {
            bool first = true;
            int processed = 0;
            while (_pending.Count > 0 && processed < MaxIntentsPerTick)
            {
                var intent = _pending.Dequeue();
                double dt = first ? elapsedMs : 0;
                first = false;
                if (_app.Tick(dt, intent)) { Close(); return; }
                processed++;
            }
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _app.Render(e.Graphics);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // skip default background paint — renderer fills it
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        base.OnFormClosed(e);
    }
}
