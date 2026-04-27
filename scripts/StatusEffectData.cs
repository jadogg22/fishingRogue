using Godot;
using Godot.Collections;

[GlobalClass]
public partial class StatusEffectData : Resource
{
	[Export] public StatusEffectType Type { get; set; } = StatusEffectType.None;
	[Export] public int Potency { get; set; }
	[Export] public int Duration { get; set; }

	public StatusEffectData() { }

	public StatusEffectData(StatusEffectType type, int potency, int duration)
	{
		Type = type;
		Potency = potency;
		Duration = duration;
	}

	public StatusEffectData DuplicateStatus()
	{
		return new StatusEffectData
		{
			Type = Type,
			Potency = Potency,
			Duration = Duration,
		};
	}

	public Dictionary ToDictionary()
	{
		return new Dictionary
		{
			{ "type", (int)Type },
			{ "potency", Potency },
			{ "duration", Duration },
		};
	}

	public static StatusEffectData FromDictionary(Dictionary data)
	{
		return new StatusEffectData
		{
			Type = (StatusEffectType)(int)data["type"],
			Potency = (int)data["potency"],
			Duration = (int)data["duration"],
		};
	}

	public string BuildSummary()
	{
		return Type switch
		{
			StatusEffectType.Poison => $"Poison {Potency} ({Duration})",
			StatusEffectType.Stun => $"Stun ({Duration})",
			StatusEffectType.Weaken => $"Weaken {Potency} ({Duration})",
			StatusEffectType.Guard => $"Guard {Potency} ({Duration})",
			StatusEffectType.Regen => $"Regen {Potency} ({Duration})",
			StatusEffectType.Focus => $"Focus {Potency} ({Duration})",
			_ => string.Empty,
		};
	}
}
