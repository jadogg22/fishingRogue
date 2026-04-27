using Godot;

public static class FxHelper
{
	public static void FlashCanvasItem(CanvasItem item, Color flashColor, float duration = 0.18f)
	{
		if (!GodotObject.IsInstanceValid(item))
		{
			return;
		}

		var originalModulate = item.Modulate;
		var tween = item.CreateTween();
		tween.TweenProperty(item, "modulate", flashColor, duration * 0.4f);
		tween.TweenProperty(item, "modulate", originalModulate, duration * 0.6f);
	}

	public static void PulseControl(Control control, float scale = 1.05f, float duration = 0.16f)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return;
		}

		control.PivotOffset = control.Size * 0.5f;
		var tween = control.CreateTween();
		tween.TweenProperty(control, "scale", new Vector2(scale, scale), duration * 0.45f);
		tween.TweenProperty(control, "scale", Vector2.One, duration * 0.55f);
	}

	public static void TweenProgressBar(Range bar, float targetValue, float duration = 0.35f)
	{
		if (!GodotObject.IsInstanceValid(bar))
		{
			return;
		}

		var tween = bar.CreateTween();
		tween.TweenProperty(bar, "value", targetValue, duration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}

	public static void ShakeControl(Control control, float intensity = 8.0f, float duration = 0.25f)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return;
		}

		var originalPosition = control.Position;
		var tween = control.CreateTween();
		
		for (var i = 0; i < 4; i++)
		{
			var offset = new Vector2((float)GD.RandRange(-intensity, intensity), (float)GD.RandRange(-intensity, intensity));
			tween.TweenProperty(control, "position", originalPosition + offset, duration / 5.0f);
		}
		
		tween.TweenProperty(control, "position", originalPosition, duration / 5.0f);
	}

	public static Label SpawnFloatingLabel(Control parent, string text, Vector2 position, Color color, int fontSize = 20)
	{
		var label = new Label
		{
			Text = text,
			Position = position,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		parent.AddChild(label);

		var tween = label.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(label, "position", position + new Vector2(0.0f, -48.0f), 0.55f);
		tween.TweenProperty(label, "modulate:a", 0.0f, 0.55f);
		tween.Finished += label.QueueFree;
		return label;
	}

	public static Line2D SpawnRing(Node parent, Vector2 position, Color color, float startRadius = 18.0f, float endRadius = 62.0f)
	{
		var ring = new Line2D
		{
			Width = 4.0f,
			DefaultColor = color,
			Position = position,
			Closed = true,
		};
		ring.Points = BuildCircle(startRadius, 24);
		parent.AddChild(ring);

		var tween = ring.CreateTween();
		tween.TweenMethod(Callable.From<float>(radius => ring.Points = BuildCircle(radius, 24)), startRadius, endRadius, 0.35f);
		tween.SetParallel(true);
		tween.TweenProperty(ring, "modulate:a", 0.0f, 0.35f);
		tween.Finished += ring.QueueFree;
		return ring;
	}

	public static void PlayOneShot(Node parent, AudioStream? stream, string name)
	{
		if (stream == null)
		{
			return;
		}

		var player = new AudioStreamPlayer
		{
			Name = name,
			Stream = stream,
		};
		parent.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	private static Vector2[] BuildCircle(float radius, int pointCount)
	{
		var points = new Vector2[pointCount];
		for (var index = 0; index < pointCount; index++)
		{
			var angle = Mathf.Tau * index / pointCount;
			points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}

		return points;
	}
}
