namespace Terrabellum.Core;

public class GameState
{
    public int CurrentTurn { get; private set; } = 1;

    public void NextTurn()
    {
        CurrentTurn++;
    }
}
