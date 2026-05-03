using System.Windows.Forms;
using TurDay.Game;

int? seed = null;
int debugStartY = 0;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out var s)) seed = s;
    if (args[i] == "--start-y" && i + 1 < args.Length && int.TryParse(args[i + 1], out var y)) debugStartY = y;
}

if (args.Contains("--help") || args.Contains("-h"))
{
    System.Windows.Forms.MessageBox.Show(
        "TurDay - turtle to the beach\n\n" +
        "Controls:\n" +
        "  Arrow keys / WASD : move\n" +
        "  Enter / Space     : confirm\n" +
        "  Esc               : pause\n" +
        "  Q                 : quit run / app\n\n" +
        $"Save file: {TurDay.Save.SaveStore.FilePath}",
        "TurDay - help");
    return 0;
}

ApplicationConfiguration.Initialize();
Application.Run(new GameForm(seed));
return 0;
