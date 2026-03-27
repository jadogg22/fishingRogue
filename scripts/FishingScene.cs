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
    private static readonly Vector2 RodOrigin = new(160.0f, 640.0f);

    private int _playerHp = 30;
    private int _roundNumber = 1;
    private float _timeLeft = RoundDuration;
    private bool _castingOut;
    private bool _manualReelReady;
    private bool _hasCaughtFish;
    private bool _castPressedLastFrame;
    private Vector2 _castTarget = RodOrigin;

    private readonly List<Area2D> _fishNodes = new();
    private readonly System.Collections.Generic.Dictionary<string, float> _fishBaseY = new();
    private readonly System.Collections.Generic.Dictionary<string, Dictionary> _fishData = new();

    public override void _Ready()
    {
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishRed"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishBlue"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishGray"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishGold"));
        _fishNodes.Add(GetNode<Area2D>("FishContainer/FishGreen"));

        _fishData["FishRed"] = new Dictionary
        {
            { "name", "Fire Strike" },
            { "type", "Attack" },
            { "damage", 8 },
            { "fish", "Red Fish" },
            { "speed", 230.0f },
            { "phase", 0.0f },
            { "direction", 1.0f },
        };
        _fishData["FishBlue"] = new Dictionary
        {
            { "name", "Tide Crash" },
            { "type", "Attack" },
            { "damage", 6 },
            { "fish", "Blue Fish" },
            { "speed", 290.0f },
            { "phase", 1.4f },
            { "direction", -1.0f },
        };
        _fishData["FishGray"] = new Dictionary
        {
            { "name", "Minnow Jab" },
            { "type", "Attack" },
            { "damage", 5 },
            { "fish", "Gray Fish" },
            { "speed", 180.0f },
            { "phase", 2.2f },
            { "direction", 1.0f },
        };
        _fishData["FishGold"] = new Dictionary
        {
            { "name", "Sunscale Slam" },
            { "type", "Attack" },
            { "damage", 10 },
            { "fish", "Gold Fish" },
            { "speed", 250.0f },
            { "phase", 0.7f },
            { "direction", -1.0f },
        };
        _fishData["FishGreen"] = new Dictionary
        {
            { "name", "Reed Ripper" },
            { "type", "Attack" },
            { "damage", 7 },
            { "fish", "Green Fish" },
            { "speed", 210.0f },
            { "phase", 2.8f },
            { "direction", 1.0f },
        };

        foreach (var fish in _fishNodes)
        {
            _fishBaseY[fish.Name.ToString()] = fish.Position.Y;
        }

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
        _castTarget = RodOrigin;

        var hook = GetNode<Area2D>("Hook_Mechanism");
        hook.Position = RodOrigin;
        GetNode<Line2D>("Hook_Rope").Points = new Vector2[]
        {
            RodOrigin,
            RodOrigin,
        };

        UpdateLabels();
    }

    public override void _Process(double delta)
    {
        HandleCastInput();
        UpdateTimer((float)delta);
        UpdateHook((float)delta);
        UpdateFishMotion((float)delta);
        UpdateLabels();
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
        var castJustPressed = castPressedNow && !_castPressedLastFrame;

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
            RodOrigin,
            hook.Position,
        };
    }

    private void UpdateFishMotion(float delta)
    {
        var elapsed = Time.GetTicksMsec() / 1000.0f;

        foreach (var fish in _fishNodes)
        {
            if (!IsInstanceValid(fish))
            {
                continue;
            }

            var fishName = fish.Name.ToString();
            var data = _fishData[fishName];
            var speed = (float)data["speed"];
            var phase = (float)data["phase"];
            var direction = (float)data["direction"];

            fish.Position = new Vector2(
                WrapFishX(fish.Position.X + speed * direction * delta, direction),
                _fishBaseY[fishName] + Mathf.Sin(elapsed * 2.0f + phase) * 18.0f
            );
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
                EmitSignal(SignalName.FishCaught, _fishData[fish.Name.ToString()].Duplicate(true));
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
        return new Dictionary
        {
            { "name", "Bare Hook" },
            { "type", "Miss" },
            { "damage", 2 },
            { "fish", "No Catch" },
        };
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
                EmitSignal(SignalName.FishCaught, _fishData[fish.Name.ToString()].Duplicate(true));
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

        return target;
    }
}
