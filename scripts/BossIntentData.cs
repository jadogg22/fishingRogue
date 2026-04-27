using Godot;

[GlobalClass]
public partial class BossIntentData : Resource
{
	[Export] public BossIntentType Type { get; set; }
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public string Description { get; set; } = string.Empty;
	[Export] public int Damage { get; set; }
	[Export] public int HealAmount { get; set; }
	[Export] public StatusEffectData? AppliedStatus { get; set; }

	public BossIntentData() { }

	public BossIntentData DuplicateStatus()
	{
		return new BossIntentData
		{
			Type = Type,
			Name = Name,
			Description = Description,
			Damage = Damage,
			HealAmount = HealAmount,
			AppliedStatus = AppliedStatus?.DuplicateStatus(),
		};
	}
}
