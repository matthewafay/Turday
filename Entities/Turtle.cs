using TurDay.Characters;

namespace TurDay.Entities;

public enum Facing { Up, Down, Left, Right }

public sealed class Turtle
{
    public const int HalfWidth = 2; // sprite is 5 wide → footprint X-2..X+2

    public int X;
    public int Y; // logical row index, 0 = bottom
    public int Lives;
    public int Coins;
    public Character Character { get; }
    public int FreePassesLeft;
    public Facing Facing = Facing.Up;
    public int HitFlashTicks; // > 0 = render hit-flash sprite

    public Turtle(Character character, int startX, int startY, int baseLives = 3)
    {
        Character = character;
        X = startX;
        Y = startY;
        Lives = baseLives + character.BonusLives;
        FreePassesLeft = character.FreePassesPerStage;
    }

    public void ResetForNewStage(int startX, int startY)
    {
        X = startX;
        Y = startY;
        Facing = Facing.Up;
        FreePassesLeft = Character.FreePassesPerStage;
    }
}
