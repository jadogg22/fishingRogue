using Godot;
using System.Collections.Generic;

public partial class BossDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int MaxHp { get; set; }
    public int Armor { get; set; }
    public int RewardGold { get; set; }
    public Color AccentColor { get; set; } = Colors.White;
    public Color BackgroundColor { get; set; } = Colors.Black;
    public List<BossIntentData> IntentSequence { get; set; } = new();

    public string BuildDisplayName()
    {
        return string.IsNullOrWhiteSpace(Subtitle) ? Name : $"{Name} | {Subtitle}";
    }

    public BossIntentData GetIntentForRound(int combatRoundNumber)
    {
        if (IntentSequence.Count == 0)
        {
            return new BossIntentData
            {
                Type = BossIntentType.Attack,
                Name = "Wild Flail",
                Description = "Attack for 4",
                Damage = 4,
            };
        }

        var sequenceIndex = Mathf.PosMod(combatRoundNumber - 1, IntentSequence.Count);
        return IntentSequence[sequenceIndex].Duplicate();
    }
}
