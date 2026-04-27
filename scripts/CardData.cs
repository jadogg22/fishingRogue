using Godot;
using Godot.Collections;
using System.Collections.Generic;

[GlobalClass]
public partial class CardData : Resource
{
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public string Fish { get; set; } = string.Empty;
	[Export] public int Damage { get; set; }
	[Export] public CombatTarget DamageTarget { get; set; } = CombatTarget.Enemy;
	[Export] public StatusEffectData StatusEffect { get; set; } = new();
	[Export] public CombatTarget StatusTarget { get; set; } = CombatTarget.Enemy;
	[Export] public bool Piercing { get; set; }

	public CardData() { }

	public virtual string PlayCard(PlayerStateManager player, EnemyStateManager boss, out int actualDamage, out int dealtDamage, out int blockedDamage, out string statusMessage)
	{
		actualDamage = 0;
		dealtDamage = 0;
		blockedDamage = 0;
		statusMessage = string.Empty;
		var summaryParts = new List<string>();

		// Handle Damage/Healing
		if (Damage > 0)
		{
			var playerTurn = player.ModifyOutgoingDamage(Damage);
			actualDamage = playerTurn.ActualDamage;
			if (!string.IsNullOrEmpty(playerTurn.Summary))
			{
				summaryParts.Add(playerTurn.Summary);
			}

			if (DamageTarget == CombatTarget.Enemy)
			{
				dealtDamage = boss.DealDamage(actualDamage, Piercing);
				blockedDamage = Mathf.Max(0, actualDamage - dealtDamage);
				
				string pierceText = Piercing ? " (Piercing)" : "";
				summaryParts.Add($"You used {Name} for {dealtDamage} damage{pierceText}");
			}
			else
			{
				player.Heal(actualDamage);
				summaryParts.Add($"You used {Name} to recover {actualDamage}");
			}
		}
		else
		{
			 summaryParts.Add($"You used {Name}");
		}

		// Handle Status Effects
		if (StatusEffect.Type != StatusEffectType.None && StatusEffect.Duration > 0)
		{
			statusMessage = StatusEffect.BuildSummary();
			if (StatusTarget == CombatTarget.Enemy)
			{
				boss.ApplyStatus(StatusEffect);
			}
			else
			{
				player.ApplyStatus(StatusEffect);
			}
			summaryParts.Add($"Applied {statusMessage}");
		}

		return string.Join(". ", summaryParts);
	}

	public string BuildButtonText()
	{
		var details = Damage > 0 ? $"{Damage} dmg" : "Support";
		if (Piercing) details += " | Pierce";
		var statusSummary = StatusEffect.BuildSummary();
		if (!string.IsNullOrEmpty(statusSummary))
		{
			details += $" | {statusSummary}";
		}

		return $"{Name}\n{Fish} | {details}";
	}

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
}
