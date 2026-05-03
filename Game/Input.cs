using System.Windows.Forms;

namespace TurDay.Game;

public enum Intent
{
    None,
    Up, Down, Left, Right,
    Confirm,
    Pause,
    Quit,
}

public static class InputReader
{
    public static Intent FromKey(Keys key) => key switch
    {
        Keys.Up    or Keys.W => Intent.Up,
        Keys.Down  or Keys.S => Intent.Down,
        Keys.Left  or Keys.A => Intent.Left,
        Keys.Right or Keys.D => Intent.Right,
        Keys.Enter or Keys.Space => Intent.Confirm,
        Keys.Escape => Intent.Pause,
        Keys.Q      => Intent.Quit,
        _           => Intent.None,
    };
}
