using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class PlayerStateManager
{
    private readonly List<StatusEffectData> _statuses = new();

    public int CurrentHp { get; private set; }
    public int MaxHp { get; }

    public PlayerStateManager(int currentHp, int maxHp, Array<Dictionary>? serializedStatuses = null)
    {
        CurrentHp = currentHp;
        MaxHp = maxHp;

        if (serializedStatuses == null)
        {
            return;
        }

        foreach (var statusData in serializedStatuses)
        {
            _statuses.Add(StatusEffectData.FromDictionary(statusData));
        }
    }

    public void DealDamage(int amount)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
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
            _statuses.Add(status.Duplicate());
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

        _statuses.Add(status.Duplicate());
    }

    public PlayerTurnResult ModifyOutgoingDamage(int baseDamage)
    {
        var bonus = 0;
        var penalty = 0;
        var summaryParts = new List<string>();

        foreach (var status in _statuses)
        {
            switch (status.Type)
            {
                case StatusEffectType.Focus:
                    bonus += status.Potency;
                    break;
                case StatusEffectType.Weaken:
                    penalty += status.Potency;
                    break;
            }
        }

        if (bonus > 0)
        {
            summaryParts.Add($"Focus adds {bonus}");
        }

        if (penalty > 0)
        {
            summaryParts.Add($"Weaken cuts {penalty}");
        }

        return new PlayerTurnResult
        {
            ActualDamage = Mathf.Max(0, baseDamage + bonus - penalty),
            Summary = string.Join(". ", summaryParts),
        };
    }

    public int MitigateIncomingDamage(int damage, out string summary)
    {
        var guardAmount = 0;
        foreach (var status in _statuses)
        {
            if (status.Type == StatusEffectType.Guard)
            {
                guardAmount += status.Potency;
            }
        }

        var actualDamage = Mathf.Max(0, damage - guardAmount);
        summary = guardAmount > 0 ? $"Guard blocks {guardAmount}" : string.Empty;
        return actualDamage;
    }

    public string ResolveEndOfEnemyTurn()
    {
        var summaryParts = new List<string>();

        foreach (var status in new List<StatusEffectData>(_statuses))
        {
            switch (status.Type)
            {
                case StatusEffectType.Poison:
                    DealDamage(status.Potency);
                    summaryParts.Add($"Player poison deals {status.Potency}");
                    break;
                case StatusEffectType.Regen:
                    Heal(status.Potency);
                    summaryParts.Add($"Regen heals {status.Potency}");
                    break;
            }
        }

        TickDownStatuses();
        return string.Join(". ", summaryParts);
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
}
