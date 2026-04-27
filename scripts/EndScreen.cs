using Godot;

public partial class EndScreen : Control
{
    [Signal]
    public delegate void RestartRequestedEventHandler();

    public override void _Ready()
    {
        GetNode<Button>("CenterContainer/VBoxContainer/RestartButton").Pressed += OnRestartPressed;
    }

    public void Setup(bool victory, int playerHp, int bossHp, int bossesCleared)
    {
        GetNode<Label>("CenterContainer/VBoxContainer/ResultLabel").Text =
            victory ? "You reeled in the win" : "The boss got away";

        GetNode<Label>("CenterContainer/VBoxContainer/SummaryLabel").Text =
            $"Bosses Cleared: {bossesCleared}  |  Player HP: {playerHp}  |  Boss HP: {bossHp}";
    }

    private void OnRestartPressed()
    {
        EmitSignal(SignalName.RestartRequested);
    }
}
