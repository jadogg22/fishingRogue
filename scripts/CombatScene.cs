using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class CombatScene : Control
{
    [Signal]
    public delegate void RoundFinishedEventHandler(Dictionary result);

    private const int BossDamage = 5;
    private const int MaxHandSize = 3;

    private readonly List<CardData> _currentHand = new();
    private PlayerStateManager? _playerState;
    private EnemyStateManager? _enemyState;
    private Dictionary _pendingResult = new()
    {
        { "player_damage", 0 },
        { "boss_damage", 0 },
        { "player_hp", 0 },
        { "boss_hp", 0 },
        { "player_statuses", new Array<Dictionary>() },
        { "boss_statuses", new Array<Dictionary>() },
    };
    private bool _roundResolved;

    public override void _Ready()
    {
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_1").Pressed += () => OnCardPressed(0);
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_2").Pressed += () => OnCardPressed(1);
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_3").Pressed += () => OnCardPressed(2);
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Pressed += OnContinuePressed;
    }

    public void SetupRound(int currentPlayerHp, int maxPlayerHp, int currentBossHp, int maxBossHp, Array<Dictionary> cardData, Array<Dictionary> playerStatuses, Array<Dictionary> bossStatuses, int battleNumber)
    {
        _currentHand.Clear();
        foreach (var cardDictionary in cardData)
        {
            _currentHand.Add(CardData.FromDictionary(cardDictionary));
        }

        _playerState = new PlayerStateManager(currentPlayerHp, maxPlayerHp, playerStatuses);
        _enemyState = new EnemyStateManager(currentBossHp, maxBossHp, BossDamage, battleNumber, bossStatuses);

        GetNode<Label>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/Label_BossName").Text = $"LEVEL 1 BOSS  |  Battle {battleNumber}";
        UpdateEnemyUi();
        UpdatePlayerUi();
        RefreshHandButtons();

        GetNode<Label>("VBox_MainLayout/Row2_PlayerArea/Margin_Player/VBox_PlayerStats/Label_Intent").Text =
            _enemyState == null ? "Boss Intent: -" : $"Boss Intent: {_enemyState.CurrentIntent.Description}";
        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            "Fish three cards, then spend them here.";
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Disabled = true;

        _roundResolved = false;
        _pendingResult = new Dictionary
        {
            { "player_damage", 0 },
            { "boss_damage", 0 },
            { "player_hp", currentPlayerHp },
            { "boss_hp", currentBossHp },
            { "player_statuses", new Array<Dictionary>() },
            { "boss_statuses", new Array<Dictionary>() },
        };
    }

    private void OnCardPressed(int cardIndex)
    {
        if (_roundResolved)
        {
            return;
        }

        if (_enemyState == null || _playerState == null)
        {
            return;
        }

        if (cardIndex < 0 || cardIndex >= _currentHand.Count)
        {
            return;
        }

        var card = _currentHand[cardIndex];
        var damageSummary = string.Empty;
        var bossDamage = 0;

        if (card.Damage > 0)
        {
            var playerTurn = _playerState.ModifyOutgoingDamage(card.Damage);
            damageSummary = playerTurn.Summary;
            bossDamage = playerTurn.ActualDamage;

            if (card.DamageTarget == CombatTarget.Enemy)
            {
                _enemyState.DealDamage(bossDamage);
                _pendingResult["boss_damage"] = (int)_pendingResult["boss_damage"] + bossDamage;
            }
            else
            {
                _playerState.Heal(bossDamage);
            }
        }

        _currentHand.RemoveAt(cardIndex);
        var statusAppliedMessage = ApplyCardStatus(card);

        UpdateEnemyUi();
        UpdatePlayerUi();
        RefreshHandButtons();

        if (_enemyState.CurrentHp <= 0)
        {
            _roundResolved = true;
            _pendingResult["player_damage"] = 0;
            _pendingResult["player_hp"] = _playerState.CurrentHp;
            _pendingResult["boss_hp"] = _enemyState.CurrentHp;
            _pendingResult["player_statuses"] = _playerState.SerializeStatuses();
            _pendingResult["boss_statuses"] = _enemyState.SerializeStatuses();
            GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Disabled = false;
            GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
                $"You finished the boss with {card.Name}.";
            return;
        }

        var playSummary = $"You used {card.Name} for {bossDamage} damage.";
        if (card.DamageTarget == CombatTarget.Player)
        {
            playSummary = $"You used {card.Name} to recover {bossDamage}.";
        }

        if (!string.IsNullOrEmpty(damageSummary))
        {
            playSummary += $" {damageSummary}.";
        }

        if (!string.IsNullOrEmpty(statusAppliedMessage))
        {
            playSummary += $" {statusAppliedMessage}.";
        }

        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            $"{playSummary} {_currentHand.Count} card(s) left.";

        if (_currentHand.Count > 0)
        {
            return;
        }

        var enemyTurn = _enemyState.ResolveTurn(_playerState);
        UpdateEnemyUi();
        UpdatePlayerUi();
        _pendingResult = new Dictionary
        {
            { "player_damage", enemyTurn.OutgoingDamage },
            { "boss_damage", _pendingResult["boss_damage"] },
            { "player_hp", _playerState.CurrentHp },
            { "boss_hp", _enemyState.CurrentHp },
            { "player_statuses", _playerState.SerializeStatuses() },
            { "boss_statuses", _enemyState.SerializeStatuses() },
        };
        _roundResolved = true;
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Disabled = false;
        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            enemyTurn.Summary;
    }

    private void OnContinuePressed()
    {
        if (!_roundResolved)
        {
            return;
        }

        EmitSignal(SignalName.RoundFinished, _pendingResult);
    }

    private void RefreshHandButtons()
    {
        for (var index = 0; index < MaxHandSize; index++)
        {
            var button = GetNode<Button>($"VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_{index + 1}");
            if (index < _currentHand.Count)
            {
                var card = _currentHand[index];
                button.Text = card.BuildButtonText();
                button.Disabled = _roundResolved;
                button.Visible = true;
            }
            else
            {
                button.Text = "Empty";
                button.Disabled = true;
                button.Visible = true;
            }
        }
    }

    private void UpdatePlayerUi()
    {
        if (_playerState == null)
        {
            return;
        }

        var playerBar = GetNode<ProgressBar>("VBox_MainLayout/Row2_PlayerArea/Margin_Player/VBox_PlayerStats/ProgressBar_PlayerHP");
        playerBar.MaxValue = _playerState.MaxHp;
        playerBar.Value = _playerState.CurrentHp;
        GetNode<Label>("VBox_MainLayout/Row2_PlayerArea/Margin_Player/VBox_PlayerStats/Label_PlayerStatus").Text =
            _playerState.BuildStatusSummary();
    }

    private void UpdateEnemyUi()
    {
        if (_enemyState == null)
        {
            return;
        }

        var bossBar = GetNode<ProgressBar>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/ProgressBar_BossHP");
        bossBar.MaxValue = _enemyState.MaxHp;
        bossBar.Value = _enemyState.CurrentHp;
        GetNode<Label>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/Label_BossStatus").Text =
            _enemyState.BuildStatusSummary();
    }

    private string ApplyCardStatus(CardData card)
    {
        if (_enemyState == null || _playerState == null)
        {
            return string.Empty;
        }

        if (card.StatusEffect.Type == StatusEffectType.None)
        {
            return string.Empty;
        }

        if (card.StatusTarget == CombatTarget.Enemy)
        {
            _enemyState.ApplyStatus(card.StatusEffect);
            UpdateEnemyUi();
        }
        else
        {
            _playerState.ApplyStatus(card.StatusEffect);
            UpdatePlayerUi();
        }

        return $"Applied {card.StatusEffect.BuildSummary()}";
    }
}
