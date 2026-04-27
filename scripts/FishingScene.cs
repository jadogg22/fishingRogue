using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class FishingScene : Node2D
{
    [Signal]
    public delegate void FishCaughtEventHandler(CardData cardData);

    private const float RoundDuration = 30.0f;
    private const float CastSpeed = 900.0f;
    private const float ReelStepDistance = 42.0f;
    private const float WaterTop = 90.0f;
    private const float WaterBottom = 640.0f;
    private const float CatchDistance = 58.0f;
    private const float HorizontalMargin = 110.0f;
    private const int MaxActiveMainFish = 2;
    private static readonly Vector2 RodOrigin = new(640.0f, 680.0f);
    private static readonly AudioStream? CastSound = GD.Load<AudioStream>("res://assets/kenney_ui-pack/Sounds/tap-a.ogg");
    private static readonly AudioStream? CatchSound = GD.Load<AudioStream>("res://assets/kenney_ui-audio/Audio/click4.ogg");
    private static readonly AudioStream? MissSound = GD.Load<AudioStream>("res://assets/kenney_ui-audio/Audio/switch37.ogg");

    [Export] public PackedScene[] MainFishPool { get; set; } = System.Array.Empty<PackedScene>();
    [Export] public PackedScene[] MinnowPool { get; set; } = System.Array.Empty<PackedScene>();
    [Export] public PackedScene[] ObstaclePool { get; set; } = System.Array.Empty<PackedScene>();
    [Export] public PackedScene[] SpecialFishPool { get; set; } = System.Array.Empty<PackedScene>();
    [Export(PropertyHint.Range, "0,1,0.01")] public float SpecialFishSpawnChance { get; set; } = 0.18f;

    private int _playerHp = 30;
    private int _roundNumber = 1;
    private int _gold;
    private float _timeLeft = RoundDuration;
    private bool _castingOut;
    private bool _manualReelReady;
    private bool _hasCaughtFish;
    private bool _roundEnding;
    private bool _castPressedLastFrame;
    private bool _pendingClick;
    private Vector2 _castTarget = RodOrigin;
    private string _targetBossName = string.Empty;

    private Node2D? _fishContainer;
    private readonly List<Fish> _activeFish = new();

    public override void _Ready()
    {
        _fishContainer = GetNode<Node2D>("FishContainer");
        foreach (Node child in _fishContainer.GetChildren())
        {
            child.QueueFree();
        }

        UpdateLabels();
    }

    public void StartRound(int currentPlayerHp, int currentRound, string bossName, int gold)
    {
        _playerHp = currentPlayerHp;
        _roundNumber = currentRound;
        _targetBossName = bossName;
        _gold = gold;
        _timeLeft = RoundDuration;
        _castingOut = false;
        _manualReelReady = false;
        _hasCaughtFish = false;
        _roundEnding = false;
        _castPressedLastFrame = false;
        _pendingClick = false;
        _castTarget = RodOrigin;

        ResetFish();

        var hook = GetNode<Area2D>("Hook_Mechanism");
        hook.Position = RodOrigin;
        GetNode<Line2D>("Hook_Rope").Points = new Vector2[]
        {
            GetRodTipGlobalPosition(),
            RodOrigin,
        };
        UpdateCastPreview();

        UpdateLabels();
    }

    private void ResetFish()
    {
        foreach (var fish in _activeFish)
        {
            if (IsInstanceValid(fish)) fish.QueueFree();
        }
        _activeFish.Clear();

        for (int i = 0; i < MaxActiveMainFish; i++)
        {
            SpawnRandomFish(GD.Randf() < SpecialFishSpawnChance ? SpecialFishPool : MainFishPool);
        }
        
        foreach (var p in MinnowPool) SpawnRandomFish(new[] { p });
        foreach (var p in ObstaclePool) SpawnRandomFish(new[] { p });
    }

    private void SpawnRandomFish(PackedScene[] pool)
    {
        if (pool == null || pool.Length == 0 || _fishContainer == null) return;

        var scene = pool[GD.Randi() % pool.Length];
        var fish = scene.Instantiate<Fish>();
        _fishContainer.AddChild(fish);
        _activeFish.Add(fish);

        float direction = GD.Randf() > 0.5f ? 1.0f : -1.0f;
        Vector2 spawnPos = new Vector2(
            direction > 0 ? -110.0f : 1390.0f,
            (float)GD.RandRange(150.0f, 560.0f)
        );
        fish.Activate(spawnPos, direction);
    }

    public override void _Process(double delta)
    {
        HandleCastInput();
        UpdateTimer((float)delta);
        UpdateHook((float)delta);
        UpdateCastPreview();
        UpdateRodVisual();
        UpdateLabels();
        UpdateMainFishRespawns((float)delta);
    }

    private void UpdateMainFishRespawns(float delta)
    {
        int activeMainCount = 0;
        foreach (var fish in _activeFish)
        {
            if (IsInstanceValid(fish) && fish.IsMainFish && fish.Visible)
                activeMainCount++;
        }

        if (activeMainCount < MaxActiveMainFish)
        {
            SpawnRandomFish(GD.Randf() < SpecialFishSpawnChance ? SpecialFishPool : MainFishPool);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _pendingClick = true;
        }
    }

    private void UpdateTimer(float delta)
    {
        if (_hasCaughtFish) return;

        _timeLeft = Mathf.Max(0.0f, _timeLeft - delta);
        if (Mathf.IsZeroApprox(_timeLeft))
        {
            FinishRound(BuildMissCard(), GetNode<Area2D>("Hook_Mechanism").Position, "Bare Hook", new Color("#ffd47a"), true);
        }
    }

    private void HandleCastInput()
    {
        var castPressedNow = Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsActionPressed("ui_accept");
        var castJustPressed = _pendingClick || (castPressedNow && !_castPressedLastFrame);

        if (castJustPressed && !_hasCaughtFish)
        {
            if (!_castingOut && !_manualReelReady && hook_is_at_rod())
            {
                _castTarget = BuildCastTarget();
                _castingOut = true;
                FxHelper.PlayOneShot(this, CastSound, "CastSound");
            }
            else if (_manualReelReady)
            {
                ReelStep();
                FxHelper.PlayOneShot(this, CastSound, "ReelSound");
            }
        }

        _castPressedLastFrame = castPressedNow;
        _pendingClick = false;
    }

    private void UpdateHook(float delta)
    {
        var hook = GetNode<Area2D>("Hook_Mechanism");

        if (_castingOut)
        {
            hook.Position = hook.Position.MoveToward(_castTarget, CastSpeed * delta);
            if (hook.Position.DistanceTo(_castTarget) <= 6.0f)
            {
                hook.Position = _castTarget;
                _castingOut = false;
                _manualReelReady = true;
            }
        }

        GetNode<Line2D>("Hook_Rope").Points = new Vector2[]
        {
            GetRodTipGlobalPosition(),
            hook.Position,
        };
    }

    private void UpdateLabels()
    {
        GetNode<Label>("CanvasLayer/UI_Container/VBox_Stats/Label_PlayerHP").Text = $"Round {_roundNumber}  |  Player HP: {_playerHp}/30  |  Gold {_gold}";
        GetNode<Label>("CanvasLayer/UI_Container/VBox_Stats/Label_Timer").Text = $"Boss: {_targetBossName}  |  Time: {_timeLeft:0.0}";
    }

    private static CardData BuildMissCard()
    {
        return new CardData
        {
            Name = "Bare Hook",
            Fish = "No Catch",
            Damage = 2,
            DamageTarget = CombatTarget.Enemy,
        };
    }

    private void ReelStep()
    {
        var hook = GetNode<Area2D>("Hook_Mechanism");
        var previousPosition = hook.Position;
        var nextPosition = hook.Position.MoveToward(RodOrigin, ReelStepDistance);

        hook.Position = nextPosition;
        ApplyLureSteer(hook, 0.12f);
        CheckForCatchAlongSegment(previousPosition, nextPosition);

        if (_hasCaughtFish) return;

        if (hook.Position.DistanceTo(RodOrigin) <= 6.0f)
        {
            hook.Position = RodOrigin;
            _manualReelReady = false;
        }
    }

    private void CheckForCatchAlongSegment(Vector2 startPoint, Vector2 endPoint)
    {
        if (_hasCaughtFish) return;

        foreach (var fish in _activeFish)
        {
            if (!IsInstanceValid(fish) || !fish.Visible) continue;

            var closestPoint = Geometry2D.GetClosestPointToSegment(fish.GlobalPosition, startPoint, endPoint);
            if (closestPoint.DistanceTo(fish.GlobalPosition) <= CatchDistance)
            {
                FinishRound(fish.Card, fish.Position, fish.Card.Name, new Color("#86ebff"));
                return;
            }
        }
    }

    private bool hook_is_at_rod()
    {
        return GetNode<Area2D>("Hook_Mechanism").Position.DistanceTo(RodOrigin) <= 6.0f;
    }

    private Vector2 BuildCastTarget()
    {
        var mousePosition = GetViewport().GetMousePosition();
        var castX = Mathf.Clamp(mousePosition.X, 120.0f, 1180.0f);
        var castY = Mathf.Clamp(mousePosition.Y, WaterTop + 20.0f, WaterBottom - 30.0f);
        var target = new Vector2(castX, castY);

        if (target.DistanceTo(RodOrigin) < 80.0f)
        {
            var direction = (target - RodOrigin).Normalized();
            if (direction == Vector2.Zero) direction = new Vector2(1.0f, -0.3f).Normalized();
            target = RodOrigin + direction * 80.0f;
        }

        target += new Vector2((float)GD.RandRange(-42.0f, 42.0f), (float)GD.RandRange(-42.0f, 42.0f));
        target.X = Mathf.Clamp(target.X, 120.0f, 1180.0f);
        target.Y = Mathf.Clamp(target.Y, WaterTop + 20.0f, WaterBottom - 30.0f);

        return target;
    }

    private async void FinishRound(CardData cardData, Vector2 effectPosition, string effectText, Color effectColor, bool missed = false)
    {
        if (_roundEnding) return;

        _roundEnding = true;
        _hasCaughtFish = true;
        _castingOut = false;
        _manualReelReady = false;

        FxHelper.PlayOneShot(this, missed ? MissSound : CatchSound, missed ? "MissSound" : "CatchSound");
        FxHelper.SpawnRing(this, effectPosition, effectColor);
        ShowFloatingCatchText(effectPosition, effectText, effectColor);
        FxHelper.FlashCanvasItem(GetNode<CanvasItem>("Hook_Mechanism/Hook_Visual"), Colors.White);

        await ToSignal(GetTree().CreateTimer(0.45f), SceneTreeTimer.SignalName.Timeout);
        EmitSignal(SignalName.FishCaught, cardData);
    }

    private void ShowFloatingCatchText(Vector2 worldPosition, string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            Position = worldPosition + new Vector2(-72.0f, -34.0f),
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 20);
        AddChild(label);

        var tween = label.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(label, "position", label.Position + new Vector2(0.0f, -36.0f), 0.45f);
        tween.TweenProperty(label, "modulate:a", 0.0f, 0.45f);
        tween.Finished += label.QueueFree;
    }

    private void UpdateCastPreview()
    {
        var previewTarget = BuildPreviewTarget();
        var castPreview = GetNode<Node2D>("CastPreview");
        castPreview.Position = previewTarget;
        castPreview.Visible = !_castingOut && !_hasCaughtFish;
    }

    private Vector2 BuildPreviewTarget()
    {
        var mousePosition = GetViewport().GetMousePosition();
        var previewX = Mathf.Clamp(mousePosition.X, 120.0f, 1180.0f);
        var previewY = Mathf.Clamp(mousePosition.Y, WaterTop + 20.0f, WaterBottom - 30.0f);
        var target = new Vector2(previewX, previewY);

        if (target.DistanceTo(RodOrigin) < 80.0f)
        {
            var direction = (target - RodOrigin).Normalized();
            if (direction == Vector2.Zero) direction = new Vector2(1.0f, -0.3f).Normalized();
            target = RodOrigin + direction * 80.0f;
        }

        return target;
    }

    private void ApplyLureSteer(Area2D hook, float delta)
    {
        var mouseX = Mathf.Clamp(GetViewport().GetMousePosition().X, 100.0f, 1180.0f);
        var centerBias = (mouseX - 640.0f) / 540.0f;
        var desiredX = hook.Position.X + centerBias * 18.0f;
        var steeredX = Mathf.MoveToward(hook.Position.X, desiredX, 90.0f * delta);
        hook.Position = new Vector2(Mathf.Clamp(steeredX, 80.0f, 1200.0f), hook.Position.Y);
    }

    private void UpdateRodVisual()
    {
        var mouseX = Mathf.Clamp(GetViewport().GetMousePosition().X, 100.0f, 1180.0f);
        var rodVisual = GetNode<Node2D>("RodVisual");
        rodVisual.RotationDegrees = Mathf.Lerp(-16.0f, 16.0f, (mouseX - 100.0f) / 1080.0f);
    }

    private Vector2 GetRodTipGlobalPosition()
    {
        var rodVisual = GetNode<Node2D>("RodVisual");
        var rodTip = rodVisual.GetNode<Line2D>("RodTip");
        var tipPoint = rodTip.Points[rodTip.Points.Length - 1];
        return rodVisual.ToGlobal(tipPoint);
    }
}
