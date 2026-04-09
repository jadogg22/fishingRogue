public partial class BossIntentData
{
    public BossIntentType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Damage { get; set; }
    public int HealAmount { get; set; }
    public StatusEffectData? AppliedStatus { get; set; }

    public BossIntentData Duplicate()
    {
        return new BossIntentData
        {
            Type = Type,
            Name = Name,
            Description = Description,
            Damage = Damage,
            HealAmount = HealAmount,
            AppliedStatus = AppliedStatus?.Duplicate(),
        };
    }
}
