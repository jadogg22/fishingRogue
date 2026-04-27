using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class EnemyStateManager
{
    private readonly List<StatusEffectData> _statuses = new();

    public int CurrentHp { get; private set; }
    public int MaxHp { get; }
    public int Armor { get; }
    public BossIntentData CurrentIntent { get; private set; }

    public EnemyStateManager(int currentHp, BossDefinition bossDefinition, int combatRoundNumber, Array<Dictionary>? serializedStatuses = null)
    {
        CurrentHp = currentHp;
        MaxHp = bossDefinition.MaxHp;
        Armor = bossDefinition.Armor;
        CurrentIntent = bossDefinition.GetIntentForRound(combatRoundNumber);

        if (serializedStatuses == null)
        {
            return;
        }

        foreach (var statusData in serializedStatuses)
        {
            _statuses.Add(StatusEffectData.FromDictionary(statusData));
        }
    }

    public int DealDamage(int amount)
    {
        var actualDamage = Mathf.Max(0, amount - Armor);
        CurrentHp = Mathf.Max(0, CurrentHp - actualDamage);
        return actualDamage;
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
    }

    public void ApplyStatus(StatusEffectData status)
    {
        if (status.Type == StatusEffectType.None || status.Duration <= 0)
        {
            return;
        }

        if (status.Type == StatusEffectType.Stun)
        {
            _statuses.Add(status.DuplicateStatus());
            return;
        }

        foreach (var existingStatus in _statuses)
        {
            if (existingStatus.Type != status.Type)
            {
                continue;
            }

            existingStatus.Potency = Mathf.Max(existingStatus.Potency, status.Potency);
            existingStatus.Duration = Mathf.Max(existingStatus.Duration, status.Duration);
            return;
        }

        _statuses.Add(status.DuplicateStatus());
    }

    public EnemyTurnResult ResolveTurn(PlayerStateManager playerState)
    {
        var result = new EnemyTurnResult();
        var summaryParts = new List<string>();

        var statusSnapshot = new List<StatusEffectData>(_statuses);
        foreach (var status in statusSnapshot)
        {
            switch (status.Type)
            {
                case StatusEffectType.Poison:
                    CurrentHp = Mathf.Max(0, CurrentHp - status.Potency);
                    result.DamageTakenFromStatuses += status.Potency;
                    summaryParts.Add($"Poison deals {status.Potency}");
                    break;
                case StatusEffectType.Stun:
                    result.CanAct = false;
                    summaryParts.Add("Stun skips the boss turn");
                    break;
                case StatusEffectType.Weaken:
                    summaryParts.Add($"Weaken lowers damage by {status.Potency}");
                    break;
            }
        }

        if (CurrentHp > 0 && result.CanAct)
        {
            ExecuteIntent(playerState, result, summaryParts);
        }

        TickDownStatuses();

        var playerEndStep = playerState.ResolveEndOfEnemyTurn();
        if (!string.IsNullOrEmpty(playerEndStep))
        {
            summaryParts.Add(playerEndStep);
        }

        result.Summary = string.Join(". ", summaryParts);
        return result;
    }

    public Array<Dictionary> SerializeStatuses()
    {
        var payload = new Array<Dictionary>();
        foreach (var status in _statuses)
        {
            payload.Add(status.ToDictionary());
        }

        return payload;
    }

    public string BuildStatusSummary()
    {
        if (_statuses.Count == 0)
        {
            return "No status effects";
        }

        var summaries = new List<string>();
        if (Armor > 0)
        {
            summaries.Add($"Armor {Armor}");
        }

        foreach (var status in _statuses)
        {
            var summary = status.BuildSummary();
            if (!string.IsNullOrEmpty(summary))
            {
                summaries.Add(summary);
            }
        }

        return summaries.Count == 0 ? "No status effects" : string.Join(" | ", summaries);
    }

    private int GetWeakenAmount()
    {
        var amount = 0;
        foreach (var status in _statuses)
        {
            if (status.Type == StatusEffectType.Weaken)
            {
                amount += status.Potency;
            }
        }

        return amount;
    }

    private void TickDownStatuses()
    {
        for (var index = _statuses.Count - 1; index >= 0; index--)
        {
            _statuses[index].Duration -= 1;
            if (_statuses[index].Duration <= 0)
            {
                _statuses.RemoveAt(index);
            }
        }
    }

    private void ExecuteIntent(PlayerStateManager playerState, EnemyTurnResult result, List<string> summaryParts)
    {
        switch (CurrentIntent.Type)
        {
            case BossIntentType.Attack:
            {
                var rawDamage = Mathf.Max(0, CurrentIntent.Damage - GetWeakenAmount());
                var actualDamage = playerState.MitigateIncomingDamage(rawDamage, out var mitigationSummary);
                playerState.DealDamage(actualDamage);
                result.OutgoingDamage = actualDamage;
                summaryParts.Add($"{CurrentIntent.Name} hits for {actualDamage}");
                if (!string.IsNullOrEmpty(mitigationSummary))
                {
                    summaryParts.Add(mitigationSummary);
                }
                break;
            }
            case BossIntentType.HeavyAttack:
            {
                var rawDamage = Mathf.Max(0, CurrentIntent.Damage - GetWeakenAmount());
                var actualDamage = playerState.MitigateIncomingDamage(rawDamage, out var mitigationSummary);
                playerState.DealDamage(actualDamage);
                result.OutgoingDamage = actualDamage;
                summaryParts.Add($"{CurrentIntent.Name} crashes for {actualDamage}");
                if (!string.IsNullOrEmpty(mitigationSummary))
                {
                    summaryParts.Add(mitigationSummary);
                }
                break;
            }
            case BossIntentType.VenomSpit:
            {
                var rawDamage = Mathf.Max(0, CurrentIntent.Damage - GetWeakenAmount());
                var actualDamage = playerState.MitigateIncomingDamage(rawDamage, out var mitigationSummary);
                playerState.DealDamage(actualDamage);
                result.OutgoingDamage = actualDamage;
                summaryParts.Add($"{CurrentIntent.Name} deals {actualDamage}");
                if (!string.IsNullOrEmpty(mitigationSummary))
                {
                    summaryParts.Add(mitigationSummary);
                }
                if (CurrentIntent.AppliedStatus != null)
                {
                    playerState.ApplyStatus(CurrentIntent.AppliedStatus);
                    summaryParts.Add($"Player gains {CurrentIntent.AppliedStatus.BuildSummary()}");
                }
                break;
            }
            case BossIntentType.CripplingRoar:
            {
                var rawDamage = Mathf.Max(0, CurrentIntent.Damage - GetWeakenAmount());
                var actualDamage = playerState.MitigateIncomingDamage(rawDamage, out var mitigationSummary);
                playerState.DealDamage(actualDamage);
                result.OutgoingDamage = actualDamage;
                summaryParts.Add($"{CurrentIntent.Name} shakes you for {actualDamage}");
                if (!string.IsNullOrEmpty(mitigationSummary))
                {
                    summaryParts.Add(mitigationSummary);
                }
                if (CurrentIntent.AppliedStatus != null)
                {
                    playerState.ApplyStatus(CurrentIntent.AppliedStatus);
                    summaryParts.Add($"Player gains {CurrentIntent.AppliedStatus.BuildSummary()}");
                }
                break;
            }
            case BossIntentType.Recover:
                Heal(CurrentIntent.HealAmount);
                result.HealAmount = CurrentIntent.HealAmount;
                summaryParts.Add($"{CurrentIntent.Name} heals {CurrentIntent.HealAmount}");
                break;
        }
    }

}
