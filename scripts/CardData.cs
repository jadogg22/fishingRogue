using Godot.Collections;

public partial class CardData
{
    public string Name { get; set; } = string.Empty;
    public string Fish { get; set; } = string.Empty;
    public int Damage { get; set; }
    public CombatTarget DamageTarget { get; set; } = CombatTarget.Enemy;
    public StatusEffectData StatusEffect { get; set; } = new();
    public CombatTarget StatusTarget { get; set; } = CombatTarget.Enemy;

    public Dictionary ToDictionary()
    {
        return new Dictionary
        {
            { "name", Name },
            { "fish", Fish },
            { "damage", Damage },
            { "damage_target", (int)DamageTarget },
            { "status_target", (int)StatusTarget },
            { "status_type", (int)StatusEffect.Type },
            { "status_potency", StatusEffect.Potency },
            { "status_duration", StatusEffect.Duration },
        };
    }

    public static CardData FromDictionary(Dictionary data)
    {
        return new CardData
        {
            Name = (string)data["name"],
            Fish = (string)data["fish"],
            Damage = (int)data["damage"],
            DamageTarget = data.ContainsKey("damage_target") ? (CombatTarget)(int)data["damage_target"] : CombatTarget.Enemy,
            StatusTarget = data.ContainsKey("status_target") ? (CombatTarget)(int)data["status_target"] : CombatTarget.Enemy,
            StatusEffect = new StatusEffectData
            {
                Type = data.ContainsKey("status_type") ? (StatusEffectType)(int)data["status_type"] : StatusEffectType.None,
                Potency = data.ContainsKey("status_potency") ? (int)data["status_potency"] : 0,
                Duration = data.ContainsKey("status_duration") ? (int)data["status_duration"] : 0,
            }
        };
    }

    public string BuildButtonText()
    {
        var details = Damage > 0 ? $"{Damage} dmg" : "Support";
        var statusSummary = StatusEffect.BuildSummary();
        if (!string.IsNullOrEmpty(statusSummary))
        {
            details += $" | {statusSummary}";
        }

        return $"{Name}\n{Fish} | {details}";
    }
}
