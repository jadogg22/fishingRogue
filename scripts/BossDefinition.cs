using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BossDefinition : Resource
{
    [Export] public string Name { get; set; } = string.Empty;
    [Export] public string Subtitle { get; set; } = string.Empty;
    [Export] public int MaxHp { get; set; }
    [Export] public int Armor { get; set; }
    [Export] public int RewardGold { get; set; }
    [Export] public Color AccentColor { get; set; } = Colors.White;
    [Export] public Color BackgroundColor { get; set; } = Colors.Black;
    [Export] public Array<BossIntentData> IntentSequence { get; set; } = new();

    public BossDefinition() { }

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
        return IntentSequence[sequenceIndex].DuplicateStatus();
    }
}
