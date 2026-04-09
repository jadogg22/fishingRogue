using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class FishingScene : Node2D
{
    [Signal]
    public delegate void FishCaughtEventHandler(Dictionary cardData);

    private const float RoundDuration = 30.0f;
    private const float CastSpeed = 900.0f;
    private const float ReelStepDistance = 42.0f;
    private const float WaterTop = 90.0f;
    private const float WaterBottom = 640.0f;
    private const float CatchDistance = 58.0f;
    private const float HorizontalMargin = 110.0f;
    private const float CastScatterRadius = 42.0f;
    private const float MinnowAggroDistance = 210.0f;
    private const float ObstacleAggroDistance = 260.0f;
    private const float LureSteerSpeed = 90.0f;
    private const int MaxActiveMainFish = 2;
    private const float MaxSteerOffset = 18.0f;
    private static readonly Vector2 RodOrigin = new(640.0f, 680.0f);

    private int _playerHp = 30;
    private int _roundNumber = 1;
    private float _timeLeft = RoundDuration;
    private bool _castingOut;
    private bool _manualReelReady;
    private bool _hasCaughtFish;
    private bool _castPressedLastFrame;
    private bool _pendingClick;
    private Vector2 _castTarget = RodOrigin;

    private readonly List<Area2D> _fishNodes = new();
    private readonly List<Area2D> _minnowNodes = new();
    private readonly List<Area2D> _obstacleNodes = new();
    private readonly List<Area2D> _mainFishNodes = new();
    private readonly System.Collections.Generic.Dictionary<string, float> _fishBaseY = new();
    private readonly System.Collections.Generic.Dictionary<string, CardData> _fishCards = new();
    private readonly System.Collections.Generic.Dictionary<string, Dictionary> _fishMotionData = new();
    private readonly System.Collections.Generic.Dictionary<string, float> _fishRespawnTimers = new();
    private readonly System.Collections.Generic.Dictionary<string, bool> _fishActiveStates = new();

    public override void _Ready()
    {
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishRed"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishBlue"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishGray"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishGold"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishGreen"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/MinnowA"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/MinnowB"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/ObstacleA"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/ObstacleB"));
        _minnowNodes.Add(GetNode<Area2D>("FishContainer/MinnowA"));
        _minnowNodes.Add(GetNode<Area2D>("FishContainer/MinnowB"));
        _obstacleNodes.Add(GetNode<Area2D>("FishContainer/ObstacleA"));
        _obstacleNodes.Add(GetNode<Area2D>("FishContainer/ObstacleB"));
        _mainFishNodes.Add(GetNode<Area2D>("FishContainer/FishRed"));
        _mainFishNodes.Add(GetNode<Area2D>("FishContainer/FishBlue"));
        _mainFishNodes.Add(GetNode<Area2D>("FishContainer/FishGray"));
        _mainFishNodes.Add(GetNode<Area2D>("FishContainer/FishGold"));
        _mainFishNodes.Add(GetNode<Area2D>("FishContainer/FishGreen"));

        _fishCards["FishRed"] = new CardData
        {
            Name = "Fire Strike",
            Fish = "Red Fish",
            Damage = 8,
            DamageTarget = CombatTarget.Enemy,
        };
        _fishMotionData["FishRed"] = new Dictionary
        {
            { "speed", 230.0f },
            { "phase", 0.0f },
            { "direction", 1.0f },
        };

        _fishCards["FishBlue"] = new CardData
        {
            Name = "Tide Crash",
            Fish = "Blue Fish",
            Damage = 5,
            DamageTarget = CombatTarget.Enemy,
            StatusTarget = CombatTarget.Enemy,
            StatusEffect = new StatusEffectData
            {
                Type = StatusEffectType.Stun,
                Potency = 0,
                Duration = 1,
            }
        };
        _fishMotionData["FishBlue"] = new Dictionary
        {
            { "speed", 290.0f },
            { "phase", 1.4f },
            { "direction", -1.0f },
        };

        _fishCards["FishGray"] = new CardData
        {
            Name = "River Guard",
            Fish = "Gray Fish",
            Damage = 3,
            DamageTarget = CombatTarget.Enemy,
            StatusTarget = CombatTarget.Player,
            StatusEffect = new StatusEffectData
            {
                Type = StatusEffectType.Guard,
                Potency = 2,
                Duration = 1,
            }
        };
        _fishMotionData["FishGray"] = new Dictionary
        {
            { "speed", 180.0f },
            { "phase", 2.2f },
            { "direction", 1.0f },
        };

        _fishCards["FishGold"] = new CardData
        {
            Name = "Sunscale Focus",
            Fish = "Gold Fish",
            Damage = 4,
            DamageTarget = CombatTarget.Enemy,
            StatusTarget = CombatTarget.Player,
            StatusEffect = new StatusEffectData
            {
                Type = StatusEffectType.Focus,
                Potency = 2,
                Duration = 2,
            }
        };
        _fishMotionData["FishGold"] = new Dictionary
        {
            { "speed", 250.0f },
            { "phase", 0.7f },
            { "direction", -1.0f },
        };

        _fishCards["FishGreen"] = new CardData
        {
            Name = "Reed Ripper",
            Fish = "Green Fish",
            Damage = 6,
            DamageTarget = CombatTarget.Enemy,
            StatusTarget = CombatTarget.Enemy,
            StatusEffect = new StatusEffectData
            {
                Type = StatusEffectType.Poison,
                Potency = 2,
                Duration = 3,
            }
        };
        _fishMotionData["FishGreen"] = new Dictionary
        {
            { "speed", 210.0f },
            { "phase", 2.8f },
            { "direction", 1.0f },
        };

        _fishCards["MinnowA"] = new CardData
        {
            Name = "Snagged Minnow",
            Fish = "Minnow",
            Damage = 1,
            DamageTarget = CombatTarget.Enemy,
        };
        _fishMotionData["MinnowA"] = new Dictionary
        {
            { "speed", 88.0f },
            { "phase", 0.8f },
            { "direction", -1.0f },
            { "aggro_speed", 150.0f },
        };

        _fishCards["MinnowB"] = new CardData
        {
            Name = "Snagged Minnow",
            Fish = "Minnow",
            Damage = 1,
            DamageTarget = CombatTarget.Enemy,
        };
        _fishMotionData["MinnowB"] = new Dictionary
        {
            { "speed", 96.0f },
            { "phase", 2.6f },
            { "direction", 1.0f },
            { "aggro_speed", 160.0f },
        };

        _fishCards["ObstacleA"] = new CardData
        {
            Name = "Snagged Weed Pike",
            Fish = "Obstacle Fish",
            Damage = 3,
            DamageTarget = CombatTarget.Enemy,
        };
        _fishMotionData["ObstacleA"] = new Dictionary
        {
            { "speed", 108.0f },
            { "phase", 0.5f },
            { "direction", -1.0f },
            { "aggro_speed", 118.0f },
        };

        _fishCards["ObstacleB"] = new CardData
        {
            Name = "Snagged Weed Pike",
            Fish = "Obstacle Fish",
            Damage = 3,
            DamageTarget = CombatTarget.Enemy,
        };
        _fishMotionData["ObstacleB"] = new Dictionary
        {
            { "speed", 116.0f },
            { "phase", 2.1f },
            { "direction", 1.0f },
            { "aggro_speed", 126.0f },
        };

        foreach (var fish in _fishNodes)
        {
            _fishBaseY[fish.Name.ToString()] = fish.Position.Y;
            _fishRespawnTimers[fish.Name.ToString()] = 0.0f;
            _fishActiveStates[fish.Name.ToString()] = true;
        }

        InitializeMainFishSpawns();
        UpdateLabels();
    }

    public void StartRound(int currentPlayerHp, int currentRound)
    {
        _playerHp = currentPlayerHp;
        _roundNumber = currentRound;
        _timeLeft = RoundDuration;
        _castingOut = false;
        _manualReelReady = false;
        _hasCaughtFish = false;
        _castPressedLastFrame = false;
        _pendingClick = false;
        _castTarget = RodOrigin;
        ResetMainFishSpawns();

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

    public override void _Process(double delta)
    {
        HandleCastInput();
        UpdateTimer((float)delta);
        UpdateHook((float)delta);
        UpdateFishMotion((float)delta);
        UpdateCastPreview();
        UpdateRodVisual();
        UpdateLabels();
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
        if (_hasCaughtFish)
        {
            return;
        }

        _timeLeft = Mathf.Max(0.0f, _timeLeft - delta);
        if (Mathf.IsZeroApprox(_timeLeft))
        {
            _hasCaughtFish = true;
            EmitSignal(SignalName.FishCaught, BuildMissCard());
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
            }
            else if (_manualReelReady)
            {
                ReelStep();
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

    private void UpdateFishMotion(float delta)
    {
        var elapsed = Time.GetTicksMsec() / 1000.0f;
        UpdateMainFishRespawns(delta);

        foreach (var fish in _fishNodes)
        {
            if (!IsInstanceValid(fish))
            {
                continue;
            }

            var fishName = fish.Name.ToString();
            var data = _fishMotionData[fishName];

            if (_minnowNodes.Contains(fish))
            {
                UpdateMinnowMotion(fish, data, elapsed, delta);
                continue;
            }

            if (_obstacleNodes.Contains(fish))
            {
                UpdateObstacleMotion(fish, data, elapsed, delta);
                continue;
            }

            if (!_fishActiveStates[fishName])
            {
                continue;
            }

            var speed = (float)data["speed"];
            var phase = (float)data["phase"];
            var direction = (float)data["direction"];

            fish.Position += new Vector2(speed * direction * delta, 0.0f);
            fish.Position = new Vector2(
                fish.Position.X,
                _fishBaseY[fishName] + Mathf.Sin(elapsed * 2.0f + phase) * 14.0f
            );

            if (fish.Position.X < -90.0f || fish.Position.X > 1370.0f)
            {
                DeactivateFish(fishName, (float)GD.RandRange(1.3f, 3.0f));
            }
        }
    }

    private void CheckForCatch()
    {
        if (_hasCaughtFish)
        {
            return;
        }

        var hook = GetNode<Area2D>("Hook_Mechanism");
        foreach (var fish in _fishNodes)
        {
            if (!IsInstanceValid(fish))
            {
                continue;
            }

            if (hook.GlobalPosition.DistanceTo(fish.GlobalPosition) <= CatchDistance)
            {
                _hasCaughtFish = true;
                _castingOut = false;
                _manualReelReady = false;
                if (_mainFishNodes.Contains(fish))
                {
                    DeactivateFish(fish.Name.ToString(), (float)GD.RandRange(1.8f, 3.2f));
                }
                EmitSignal(SignalName.FishCaught, _fishCards[fish.Name.ToString()].ToDictionary());
                return;
            }
        }
    }

    private void UpdateLabels()
    {
        GetNode<Label>("CanvasLayer/UI_Container/VBox_Stats/Label_PlayerHP").Text = $"Round {_roundNumber}  |  Player HP: {_playerHp}/30";
        GetNode<Label>("CanvasLayer/UI_Container/VBox_Stats/Label_Timer").Text = $"Time: {_timeLeft:0.0}";
    }

    private static Dictionary BuildMissCard()
    {
        return new CardData
        {
            Name = "Bare Hook",
            Fish = "No Catch",
            Damage = 2,
            DamageTarget = CombatTarget.Enemy,
        }.ToDictionary();
    }

    private static float WrapFishX(float x, float direction)
    {
        if (direction > 0.0f && x > 1280.0f + HorizontalMargin)
        {
            return -HorizontalMargin;
        }

        if (direction < 0.0f && x < -HorizontalMargin)
        {
            return 1280.0f + HorizontalMargin;
        }

        return x;
    }

    private void ReelStep()
    {
        var hook = GetNode<Area2D>("Hook_Mechanism");
        var previousPosition = hook.Position;
        var nextPosition = hook.Position.MoveToward(RodOrigin, ReelStepDistance);

        hook.Position = nextPosition;
        ApplyLureSteer(hook, 0.12f);
        CheckForCatchAlongSegment(previousPosition, nextPosition);

        if (_hasCaughtFish)
        {
            return;
        }

        if (hook.Position.DistanceTo(RodOrigin) <= 6.0f)
        {
            hook.Position = RodOrigin;
            _manualReelReady = false;
        }
    }

    private void CheckForCatchAlongSegment(Vector2 startPoint, Vector2 endPoint)
    {
        if (_hasCaughtFish)
        {
            return;
        }

        foreach (var fish in _fishNodes)
        {
            if (!IsInstanceValid(fish))
            {
                continue;
            }

            var closestPoint = Geometry2D.GetClosestPointToSegment(fish.GlobalPosition, startPoint, endPoint);
            if (closestPoint.DistanceTo(fish.GlobalPosition) <= CatchDistance)
            {
                _hasCaughtFish = true;
                _castingOut = false;
                _manualReelReady = false;
                if (_mainFishNodes.Contains(fish))
                {
                    DeactivateFish(fish.Name.ToString(), (float)GD.RandRange(1.8f, 3.2f));
                }
                EmitSignal(SignalName.FishCaught, _fishCards[fish.Name.ToString()].ToDictionary());
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
            if (direction == Vector2.Zero)
            {
                direction = new Vector2(1.0f, -0.3f).Normalized();
            }
            target = RodOrigin + direction * 80.0f;
        }

        var scatterOffset = new Vector2(
            (float)GD.RandRange(-CastScatterRadius, CastScatterRadius),
            (float)GD.RandRange(-CastScatterRadius, CastScatterRadius)
        );
        target += scatterOffset;
        target.X = Mathf.Clamp(target.X, 120.0f, 1180.0f);
        target.Y = Mathf.Clamp(target.Y, WaterTop + 20.0f, WaterBottom - 30.0f);

        return target;
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
            if (direction == Vector2.Zero)
            {
                direction = new Vector2(1.0f, -0.3f).Normalized();
            }
            target = RodOrigin + direction * 80.0f;
        }

        return target;
    }

    private void UpdateMinnowMotion(Area2D minnow, Dictionary data, float elapsed, float delta)
    {
        var hook = GetNode<Area2D>("Hook_Mechanism");
        var directionToHook = hook.Position - minnow.Position;
        var chasingHook = !hook_is_at_rod() && directionToHook.Length() <= MinnowAggroDistance;

        if (chasingHook)
        {
            var chaseDirection = directionToHook.Normalized();
            var aggroSpeed = (float)data["aggro_speed"];
            minnow.Position += chaseDirection * aggroSpeed * delta;
            minnow.Position = new Vector2(
                Mathf.Clamp(minnow.Position.X, 80.0f, 1200.0f),
                Mathf.Clamp(minnow.Position.Y, WaterTop + 20.0f, WaterBottom - 10.0f)
            );
            return;
        }

        var speed = (float)data["speed"];
        var phase = (float)data["phase"];
        var direction = (float)data["direction"];
        minnow.Position = new Vector2(
            WrapFishX(minnow.Position.X + speed * direction * delta, direction),
            _fishBaseY[minnow.Name.ToString()] + Mathf.Sin(elapsed * 3.3f + phase) * 12.0f
        );
    }

    private void UpdateObstacleMotion(Area2D obstacleFish, Dictionary data, float elapsed, float delta)
    {
        var hook = GetNode<Area2D>("Hook_Mechanism");
        var directionToHook = hook.Position - obstacleFish.Position;
        var chasingHook = !hook_is_at_rod() && directionToHook.Length() <= ObstacleAggroDistance;

        if (chasingHook)
        {
            var chaseDirection = directionToHook.Normalized();
            var aggroSpeed = (float)data["aggro_speed"];
            obstacleFish.Position += chaseDirection * aggroSpeed * delta;
            obstacleFish.Position = new Vector2(
                Mathf.Clamp(obstacleFish.Position.X, 80.0f, 1200.0f),
                Mathf.Clamp(obstacleFish.Position.Y, WaterTop + 20.0f, WaterBottom - 10.0f)
            );
            return;
        }

        var speed = (float)data["speed"];
        var phase = (float)data["phase"];
        var direction = (float)data["direction"];
        obstacleFish.Position = new Vector2(
            WrapFishX(obstacleFish.Position.X + speed * direction * delta, direction),
            _fishBaseY[obstacleFish.Name.ToString()] + Mathf.Sin(elapsed * 2.6f + phase) * 18.0f
        );
    }

    private void InitializeMainFishSpawns()
    {
        var activeCount = 0;
        foreach (var fish in _mainFishNodes)
        {
            var fishName = fish.Name.ToString();
            if (activeCount < MaxActiveMainFish)
            {
                SpawnFishNow(fish);
                activeCount += 1;
            }
            else
            {
                DeactivateFish(fishName, GetRespawnDelayForFish(fishName));
            }
        }
    }

    private void ResetMainFishSpawns()
    {
        InitializeMainFishSpawns();
    }

    private void UpdateMainFishRespawns(float delta)
    {
        foreach (var fish in _mainFishNodes)
        {
            var fishName = fish.Name.ToString();
            if (_fishActiveStates[fishName])
            {
                continue;
            }

            _fishRespawnTimers[fishName] -= delta;
            if (_fishRespawnTimers[fishName] > 0.0f)
            {
                continue;
            }

            if (CountActiveMainFish() >= MaxActiveMainFish)
            {
                _fishRespawnTimers[fishName] = 0.6f;
                continue;
            }

            SpawnFishNow(fish);
        }
    }

    private void SpawnFishNow(Area2D fish)
    {
        var fishName = fish.Name.ToString();
        var data = _fishMotionData[fishName];
        var direction = (float)data["direction"];
        var spawnFromLeft = direction > 0.0f;
        fish.Position = new Vector2(
            spawnFromLeft ? -70.0f : 1350.0f,
            (float)GD.RandRange(150.0f, 560.0f)
        );
        _fishBaseY[fishName] = fish.Position.Y;
        _fishRespawnTimers[fishName] = 0.0f;
        _fishActiveStates[fishName] = true;
        SetFishVisible(fish, true);
    }

    private void DeactivateFish(string fishName, float respawnDelay)
    {
        var fish = GetNode<Area2D>($"FishContainer/{fishName}");
        _fishActiveStates[fishName] = false;
        _fishRespawnTimers[fishName] = respawnDelay;
        SetFishVisible(fish, false);
    }

    private static void SetFishVisible(Area2D fish, bool visible)
    {
        fish.Visible = visible;
        fish.GetNode<CollisionShape2D>("CollisionShape2D").Disabled = !visible;
    }

    private int CountActiveMainFish()
    {
        var count = 0;
        foreach (var fish in _mainFishNodes)
        {
            if (_fishActiveStates[fish.Name.ToString()])
            {
                count += 1;
            }
        }

        return count;
    }

    private float GetRespawnDelayForFish(string fishName)
    {
        return fishName switch
        {
            "FishGold" => (float)GD.RandRange(5.0f, 8.0f),
            "FishGreen" => (float)GD.RandRange(4.0f, 6.5f),
            "FishBlue" => (float)GD.RandRange(2.1f, 3.8f),
            "FishGray" => (float)GD.RandRange(1.8f, 3.3f),
            _ => (float)GD.RandRange(1.0f, 2.3f),
        };
    }

    private void ApplyLureSteer(Area2D hook, float delta)
    {
        var mouseX = Mathf.Clamp(GetViewport().GetMousePosition().X, 100.0f, 1180.0f);
        var centerBias = (mouseX - 640.0f) / 540.0f;
        var desiredX = hook.Position.X + centerBias * MaxSteerOffset;
        var maxStep = LureSteerSpeed * delta;
        var steeredX = Mathf.MoveToward(hook.Position.X, desiredX, maxStep);
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
