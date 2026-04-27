using Godot;

public partial class Fish : Area2D
{
    [Export] public CardData Card { get; set; } = new();
    [Export] public Texture2D? SwimTexture { get; set; }
    [Export] public int FrameWidth { get; set; } = 48;
    [Export] public float VisualScale { get; set; } = 1.0f;
    [Export] public Vector2 VisualOffset { get; set; } = Vector2.Zero;
    [Export] public float AnimationSpeed { get; set; } = 5.0f;
    [Export] public float Speed { get; set; } = 200.0f;
    [Export] public float SineFrequency { get; set; } = 2.0f;
    [Export] public float SineAmplitude { get; set; } = 14.0f;
    [Export] public float SinePhase { get; set; }
    [Export] public float Direction { get; set; } = 1.0f;
    [Export] public bool IsMainFish { get; set; } = true;
    [Export] public float AggroDistance { get; set; } = 210.0f;
    [Export] public float AggroSpeed { get; set; } = 150.0f;
    [Export] public bool HasAggro { get; set; }

    private float _baseY;
    private Node2D? _hook;
    private bool _active = true;
    private AnimatedSprite2D? _animatedSprite;

    public override void _Ready()
    {
        _baseY = Position.Y;
        _hook = GetTree().Root.FindChild("Hook_Mechanism", true, false) as Node2D;
        SetupVisual();
        EnsureDefaultCardData();
        UpdateFacing(Direction);
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

        float fDelta = (float)delta;
        float elapsed = Time.GetTicksMsec() / 1000.0f;

        if (HasAggro && _hook != null)
        {
            UpdateAggroMovement(fDelta);
        }
        else
        {
            UpdateNormalMovement(elapsed, fDelta);
        }

        // Handle wrapping or deactivation based on main fish status
        if (Position.X < -110.0f || Position.X > 1390.0f)
        {
            if (IsMainFish)
            {
                // Main fish deactivate and wait for FishingScene to respawn them
                Deactivate();
            }
            else
            {
                // Minnows and obstacles wrap
                WrapX();
            }
        }
    }

    private void UpdateNormalMovement(float elapsed, float delta)
    {
        Position += new Vector2(Speed * Direction * delta, 0.0f);
        Position = new Vector2(
            Position.X,
            _baseY + Mathf.Sin(elapsed * SineFrequency + SinePhase) * SineAmplitude
        );
        UpdateFacing(Direction);
    }

    private void UpdateAggroMovement(float delta)
    {
        if (_hook == null) return;

        Vector2 directionToHook = _hook.GlobalPosition - GlobalPosition;
        if (directionToHook.Length() <= AggroDistance)
        {
            Vector2 chaseDirection = directionToHook.Normalized();
            Position += chaseDirection * AggroSpeed * delta;
            UpdateFacing(chaseDirection.X);
            
            // Constrain to water area (rough estimates from FishingScene)
            Position = new Vector2(
                Mathf.Clamp(Position.X, -100.0f, 1380.0f),
                Mathf.Clamp(Position.Y, 110.0f, 630.0f)
            );
        }
        else
        {
            // If out of range, just continue normal X movement but keep current Y for a moment?
            // For now, let's just do normal movement if not aggro'd
            Position += new Vector2(Speed * Direction * delta, 0.0f);
            UpdateFacing(Direction);
        }
    }

    private void WrapX()
    {
        if (Direction > 0.0f && Position.X > 1280.0f + 110.0f)
        {
            Position = new Vector2(-110.0f, Position.Y);
        }
        else if (Direction < 0.0f && Position.X < -110.0f)
        {
            Position = new Vector2(1280.0f + 110.0f, Position.Y);
        }
    }

    public void Deactivate()
    {
        _active = false;
        Visible = false;
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") is CollisionShape2D col)
        {
            col.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        }
    }

    public void Activate(Vector2 startPos, float direction)
    {
        Position = startPos;
        _baseY = startPos.Y;
        Direction = direction;
        _active = true;
        Visible = true;
        UpdateFacing(direction);
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") is CollisionShape2D col)
        {
            col.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        }
    }

    private void SetupVisual()
    {
        if (SwimTexture == null || FrameWidth <= 0)
        {
            return;
        }

        if (GetNodeOrNull<CanvasItem>("Visual") is CanvasItem oldVisual)
        {
            oldVisual.Visible = false;
        }

        var existing = GetNodeOrNull<AnimatedSprite2D>("AnimatedVisual");
        if (existing != null)
        {
            existing.QueueFree();
        }

        var frames = new SpriteFrames();
        frames.AddAnimation("swim");
        frames.SetAnimationLoop("swim", true);
        frames.SetAnimationSpeed("swim", AnimationSpeed);

        var frameCount = Mathf.Max(1, SwimTexture.GetWidth() / FrameWidth);
        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            frames.AddFrame("swim", new AtlasTexture
            {
                Atlas = SwimTexture,
                Region = new Rect2(frameIndex * FrameWidth, 0, FrameWidth, SwimTexture.GetHeight()),
            });
        }

        _animatedSprite = new AnimatedSprite2D
        {
            Name = "AnimatedVisual",
            SpriteFrames = frames,
            Animation = "swim",
            Centered = true,
            Position = VisualOffset,
            Scale = new Vector2(VisualScale, VisualScale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        AddChild(_animatedSprite);
        _animatedSprite.Play();
    }

    private void UpdateFacing(float horizontalDirection)
    {
        if (_animatedSprite == null || Mathf.IsZeroApprox(horizontalDirection))
        {
            return;
        }

        // Source art faces right by default.
        _animatedSprite.FlipH = horizontalDirection < 0.0f;
    }

    private void EnsureDefaultCardData()
    {
        if (!string.IsNullOrWhiteSpace(Card.Name))
        {
            return;
        }

        Card = Name switch
        {
            "RedFish" => new CardData
            {
                Name = "Fire Strike",
                Fish = "Red Fish",
                Damage = 8,
                DamageTarget = CombatTarget.Enemy,
            },
            "BlueFish" => new CardData
            {
                Name = "Tide Crash",
                Fish = "Blue Fish",
                Damage = 5,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Enemy,
                StatusEffect = new StatusEffectData(StatusEffectType.Stun, 0, 1),
            },
            "GrayFish" => new CardData
            {
                Name = "River Guard",
                Fish = "Gray Fish",
                Damage = 3,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Player,
                StatusEffect = new StatusEffectData(StatusEffectType.Guard, 2, 1),
            },
            "GoldFish" => new CardData
            {
                Name = "Sunscale Focus",
                Fish = "Gold Fish",
                Damage = 4,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Player,
                StatusEffect = new StatusEffectData(StatusEffectType.Focus, 2, 2),
            },
            "GreenFish" => new CardData
            {
                Name = "Reed Ripper",
                Fish = "Green Fish",
                Damage = 6,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Enemy,
                StatusEffect = new StatusEffectData(StatusEffectType.Poison, 2, 3),
            },
            "Minnow" => new CardData
            {
                Name = "Snagged Minnow",
                Fish = "Minnow",
                Damage = 1,
                DamageTarget = CombatTarget.Enemy,
            },
            "AnglerFish" => new CardData
            {
                Name = "Lantern Lure",
                Fish = "Angler",
                Damage = 7,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Enemy,
                StatusEffect = new StatusEffectData(StatusEffectType.Weaken, 1, 2),
            },
            "EelFish" => new CardData
            {
                Name = "Shock Eel",
                Fish = "Eel",
                Damage = 6,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Enemy,
                StatusEffect = new StatusEffectData(StatusEffectType.Stun, 0, 1),
            },
            "JellyfishFish" => new CardData
            {
                Name = "Jelly Veil",
                Fish = "Jellyfish",
                Damage = 3,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Player,
                StatusEffect = new StatusEffectData(StatusEffectType.Guard, 3, 2),
            },
            "OctopusFish" => new CardData
            {
                Name = "Ink Burst",
                Fish = "Octopus",
                Damage = 8,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Enemy,
                StatusEffect = new StatusEffectData(StatusEffectType.Weaken, 2, 2),
            },
            "SwordfishFish" => new CardData
            {
                Name = "Piercing Rush",
                Fish = "Swordfish",
                Damage = 10,
                DamageTarget = CombatTarget.Enemy,
            },
            "TurtleFish" => new CardData
            {
                Name = "Shell Blessing",
                Fish = "Turtle",
                Damage = 2,
                DamageTarget = CombatTarget.Enemy,
                StatusTarget = CombatTarget.Player,
                StatusEffect = new StatusEffectData(StatusEffectType.Guard, 4, 2),
            },
            _ => Card,
        };
    }
}
