using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	private static readonly AudioStream? HoverSound = GD.Load<AudioStream>("res://assets/kenney_ui-audio/Audio/rollover1.ogg");
	private static readonly AudioStream? ClickSound = GD.Load<AudioStream>("res://assets/kenney_ui-audio/Audio/click1.ogg");

	[Export] public Array<PackedScene> FishPool { get; set; } = new();

	private Node2D? _fishContainer;

	public override void _Ready()
	{
		_fishContainer = GetNodeOrNull<Node2D>("BackgroundFish");
		
		var startButton = GetNode<Button>("CenterBox/VBoxContainer/StartButton");
		var quitButton = GetNodeOrNull<Button>("CenterBox/VBoxContainer/QuitButton");

		startButton.Pressed += OnStartPressed;
		WireButtonFx(startButton);

		if (quitButton != null)
		{
			quitButton.Pressed += OnQuitPressed;
			WireButtonFx(quitButton);
		}

		SpawnBackgroundFish();
	}

	private void SpawnBackgroundFish()
	{
		if (_fishContainer == null || FishPool.Count == 0) return;

		// Spawn a handful of decorative fish
		for (int i = 0; i < 8; i++)
		{
			var scene = FishPool[(int)(GD.Randi() % (uint)FishPool.Count)];
			var fish = scene.Instantiate<Fish>();
			_fishContainer.AddChild(fish);
			
			// Disable collision so they don't try to look for a hook
			fish.SetDeferred(Area2D.PropertyName.Monitoring, false);
			fish.SetDeferred(Area2D.PropertyName.Monitorable, false);

			float direction = GD.Randf() > 0.5f ? 1.0f : -1.0f;
			Vector2 spawnPos = new Vector2(
				(float)GD.RandRange(50.0f, 1230.0f),
				(float)GD.RandRange(50.0f, 670.0f)
			);
			fish.Activate(spawnPos, direction);
		}
	}

	private void OnStartPressed()
	{
		FxHelper.PlayOneShot(this, ClickSound, "StartClick");
		GetTree().ChangeSceneToFile("res://scenes/GameManager.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	private void WireButtonFx(Button button)
	{
		button.MouseEntered += () =>
		{
			if (button.Disabled) return;
			FxHelper.PulseControl(button, 1.05f, 0.14f);
			FxHelper.PlayOneShot(this, HoverSound, $"{button.Name}_hover");
		};
	}
}
