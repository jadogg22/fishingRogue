using Godot;

public partial class Fish : Area2D
{
    [Export] public CardData Card { get; set; } = new();
    [Export] public float Speed { get; set; } = 200.0f;
    [Export] public float SpeedVariation { get; set; } = 0.25f;
    [Export] public float SineFrequency { get; set; } = 2.0f;
    [Export] public float SineAmplitude { get; set; } = 14.0f;
    [Export] public float SinePhase { get; set; }
    [Export] public float Direction { get; set; } = 1.0f;
    [Export] public bool IsMainFish { get; set; } = true;
    [Export] public float AggroDistance { get; set; } = 210.0f;
    [Export] public float AggroSpeed { get; set; } = 150.0f;
    [Export] public bool HasAggro { get; set; }

    private float _baseY;
    private float _currentSpeed;
    private Node2D? _hook;
    private bool _active = true;

    public override void _Ready()
    {
        _baseY = Position.Y;
        _currentSpeed = Speed;
        _hook = GetTree().Root.FindChild("Hook_Mechanism", true, false) as Node2D;
        EnsureDefaultCardData();
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

        if (Position.X < -150.0f || Position.X > 1430.0f)
        {
            if (IsMainFish)
            {
                Deactivate();
            }
            else
            {
                WrapX();
            }
        }
    }

    private void UpdateNormalMovement(float elapsed, float delta)
    {
        Position += new Vector2(_currentSpeed * Direction * delta, 0.0f);
        Position = new Vector2(
            Position.X,
            _baseY + Mathf.Sin(elapsed * SineFrequency + SinePhase) * SineAmplitude
        );
    }

    private void UpdateAggroMovement(float delta)
    {
        if (_hook == null) return;

        // Only aggro if the hook is actually in the water (not at the rod/hands)
        if (_hook.Position.Y > 640.0f) 
        {
            UpdateNormalMovement((float)(Time.GetTicksMsec() / 1000.0f), delta);
            return;
        }

        Vector2 directionToHook = _hook.GlobalPosition - GlobalPosition;
        if (directionToHook.Length() <= AggroDistance)
        {
            Vector2 chaseDirection = directionToHook.Normalized();
            Position += chaseDirection * AggroSpeed * delta;
            
            Position = new Vector2(
                Mathf.Clamp(Position.X, -140.0f, 1420.0f),
                Mathf.Clamp(Position.Y, 120.0f, 580.0f)
            );
        }
        else
        {
            Position += new Vector2(_currentSpeed * Direction * delta, 0.0f);
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
        
        // Randomize speed and phase on each activation
        _currentSpeed = Speed * (1.0f + (float)GD.RandRange(-SpeedVariation, SpeedVariation));
        SinePhase = (float)GD.RandRange(0.0f, Mathf.Tau);
        
        _active = true;
        Visible = true;
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") is CollisionShape2D col)
        {
            col.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        }

        if (GetNodeOrNull<Sprite2D>("Sprite2D") is Sprite2D sprite)
        {
            sprite.FlipH = direction < 0.0f;
        }
    }

    private void EnsureDefaultCardData()
    {
        if (!string.IsNullOrWhiteSpace(Card.Name)) return;

        Card = ((string)Name) switch
        {
            "Fish1" => new CardData { Name = "Pesky Minnow", Fish = "Minnow", Damage = 1 },
            "Fish2" => new CardData { Name = "Large Minnow", Fish = "Minnow", Damage = 2 },
            "Fish3" => new CardData { Name = "Common Perch", Fish = "Perch", Damage = 3 },
            "Fish4" => new CardData { Name = "Swift Trout", Fish = "Trout", Damage = 4 },
            "Fish5" => new CardData { Name = "Sturdy Bass", Fish = "Bass", Damage = 5 },
            "Fish6" => new CardData { Name = "Gilded Carp", Fish = "Carp", Damage = 6 },
            "Fish7" => new CardData { Name = "Deep Grouper", Fish = "Grouper", Damage = 8 },
            "Fish8" => new CardData { Name = "King Salmon", Fish = "Salmon", Damage = 10 },
            "AnglerFish" => new CardData
            {
                Name = "Luring Light",
                Fish = "Angler",
                Damage = 7,
                StatusEffect = new StatusEffectData(StatusEffectType.Weaken, 2, 2),
            },
            "EelFish" => new CardData
            {
                Name = "Static Shock",
                Fish = "Eel",
                Damage = 4,
                StatusEffect = new StatusEffectData(StatusEffectType.Stun, 0, 1),
            },
            "JellyfishFish" => new CardData
            {
                Name = "Paralyzing Veil",
                Fish = "Jellyfish",
                Damage = 5,
                StatusEffect = new StatusEffectData(StatusEffectType.Stun, 0, 1),
            },
            "OctopusFish" => new CardData
            {
                Name = "Inky Escape",
                Fish = "Octopus",
                Damage = 6,
                StatusEffect = new StatusEffectData(StatusEffectType.Weaken, 3, 2),
            },
            "SwordfishFish" => new CardData
            {
                Name = "Piercing Rush",
                Fish = "Swordfish",
                Damage = 10,
                Piercing = true,
            },
            "TurtleFish" => new CardData
            {
                Name = "Shell Blessing",
                Fish = "Turtle",
                Damage = 2,
                StatusTarget = CombatTarget.Player,
                StatusEffect = new StatusEffectData(StatusEffectType.Guard, 4, 2),
            },
            _ => Card,
        };
    }
}
