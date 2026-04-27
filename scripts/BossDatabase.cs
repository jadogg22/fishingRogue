using Godot;
using System.Collections.Generic;

public static class BossDatabase
{
    public static List<BossDefinition> BuildActOneBosses()
    {
        return new List<BossDefinition>
        {
            new BossDefinition
            {
                Name = "Tide Warden",
                Subtitle = "Pier Sentinel",
                MaxHp = 44,
                Armor = 1,
                RewardGold = 12,
                AccentColor = new Color("a14f47"),
                BackgroundColor = new Color("1a1f2d"),
                IntentSequence = new List<BossIntentData>
                {
                    new BossIntentData
                    {
                        Type = BossIntentType.Attack,
                        Name = "Fin Slam",
                        Description = "Attack for 6",
                        Damage = 6,
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.HeavyAttack,
                        Name = "Undertow Crush",
                        Description = "Heavy attack for 8",
                        Damage = 8,
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.CripplingRoar,
                        Name = "Crippling Roar",
                        Description = "Attack for 4 and apply Weaken 2",
                        Damage = 4,
                        AppliedStatus = new StatusEffectData
                        {
                            Type = StatusEffectType.Weaken,
                            Potency = 2,
                            Duration = 2,
                        }
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.Recover,
                        Name = "Saltbound Breath",
                        Description = "Recover 5 HP",
                        HealAmount = 5,
                    },
                }
            },
            new BossDefinition
            {
                Name = "Mire Queen",
                Subtitle = "Bog Matriarch",
                MaxHp = 56,
                Armor = 1,
                RewardGold = 18,
                AccentColor = new Color("4b8d56"),
                BackgroundColor = new Color("16271c"),
                IntentSequence = new List<BossIntentData>
                {
                    new BossIntentData
                    {
                        Type = BossIntentType.VenomSpit,
                        Name = "Bog Spray",
                        Description = "Attack for 5 and apply Poison 2",
                        Damage = 5,
                        AppliedStatus = new StatusEffectData
                        {
                            Type = StatusEffectType.Poison,
                            Potency = 2,
                            Duration = 2,
                        }
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.Attack,
                        Name = "Lash Tail",
                        Description = "Attack for 7",
                        Damage = 7,
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.CripplingRoar,
                        Name = "Silt Hex",
                        Description = "Attack for 5 and apply Weaken 1",
                        Damage = 5,
                        AppliedStatus = new StatusEffectData
                        {
                            Type = StatusEffectType.Weaken,
                            Potency = 1,
                            Duration = 3,
                        }
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.HeavyAttack,
                        Name = "Marsh Drop",
                        Description = "Heavy attack for 9",
                        Damage = 9,
                    },
                }
            },
            new BossDefinition
            {
                Name = "Abyss Monarch",
                Subtitle = "The Lake Below",
                MaxHp = 68,
                Armor = 2,
                RewardGold = 30,
                AccentColor = new Color("d0a13f"),
                BackgroundColor = new Color("14131f"),
                IntentSequence = new List<BossIntentData>
                {
                    new BossIntentData
                    {
                        Type = BossIntentType.Attack,
                        Name = "Royal Bite",
                        Description = "Attack for 8",
                        Damage = 8,
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.HeavyAttack,
                        Name = "Depth Charge",
                        Description = "Heavy attack for 11",
                        Damage = 11,
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.VenomSpit,
                        Name = "Blackwater Spit",
                        Description = "Attack for 6 and apply Poison 2",
                        Damage = 6,
                        AppliedStatus = new StatusEffectData
                        {
                            Type = StatusEffectType.Poison,
                            Potency = 2,
                            Duration = 3,
                        }
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.Recover,
                        Name = "Deep Feast",
                        Description = "Recover 7 HP",
                        HealAmount = 7,
                    },
                    new BossIntentData
                    {
                        Type = BossIntentType.CripplingRoar,
                        Name = "Pressure Wave",
                        Description = "Attack for 6 and apply Weaken 2",
                        Damage = 6,
                        AppliedStatus = new StatusEffectData
                        {
                            Type = StatusEffectType.Weaken,
                            Potency = 2,
                            Duration = 2,
                        }
                    },
                }
            }
        };
    }
}
