using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("CenterBox/VBoxContainer/StartButton").Pressed += OnStartPressed;
    }

    private void OnStartPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/GameManager.tscn");
    }
}
