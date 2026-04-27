using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class CombatScene : Control
{
    [Signal]
    public delegate void RoundFinishedEventHandler(Dictionary result);

    private const int MaxHandSize = 3;
    private static readonly AudioStream? HoverSound = GD.Load<AudioStream>("res://assets/kenney_ui-pack/Sounds/switch-a.ogg");
    private static readonly AudioStream? ClickSound = GD.Load<AudioStream>("res://assets/kenney_ui-pack/Sounds/click-a.ogg");
    private static readonly AudioStream? HitSound = GD.Load<AudioStream>("res://assets/kenney_ui-audio/Audio/click3.ogg");
    private static readonly AudioStream? HeavyHitSound = GD.Load<AudioStream>("res://assets/kenney_ui-audio/Audio/click5.ogg");

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
        var cardOne = GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_1");
        var cardTwo = GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_2");
        var cardThree = GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_3");
        var continueButton = GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton");

        cardOne.Pressed += () => OnCardPressed(0);
        cardTwo.Pressed += () => OnCardPressed(1);
        cardThree.Pressed += () => OnCardPressed(2);
        continueButton.Pressed += OnContinuePressed;

        WireButtonFx(cardOne);
        WireButtonFx(cardTwo);
        WireButtonFx(cardThree);
        WireButtonFx(continueButton);
    }

    public void SetupRound(int currentPlayerHp, int maxPlayerHp, int currentBossHp, List<CardData> cardData, Array<Dictionary> playerStatuses, Array<Dictionary> bossStatuses, BossDefinition bossDefinition, int combatRoundNumber, int gold)
    {
        _currentHand.Clear();
        _currentHand.AddRange(cardData);

        _playerState = new PlayerStateManager(currentPlayerHp, maxPlayerHp, playerStatuses);
        _enemyState = new EnemyStateManager(currentBossHp, bossDefinition, combatRoundNumber, bossStatuses);

        GetNode<Label>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/Label_BossName").Text =
            $"{bossDefinition.BuildDisplayName()}  |  Combat {combatRoundNumber}";
        GetNode<ColorRect>("Background_Dungeon").Color = bossDefinition.BackgroundColor;
        GetNode<ColorRect>("VBox_MainLayout/Row1_BossArea/BossSpriteAnchor/Sprite_Boss").Color = bossDefinition.AccentColor;
        UpdateEnemyUi();
        UpdatePlayerUi();
        RefreshHandButtons();

        GetNode<Label>("VBox_MainLayout/Row2_PlayerArea/Margin_Player/VBox_PlayerStats/Label_Intent").Text =
            _enemyState == null ? "Boss Intent: -" : $"Boss Intent: {_enemyState.CurrentIntent.Description}";
        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            $"Fish three cards, then break through {bossDefinition.Name}'s pattern.";
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Text =
            $"Continue  |  Gold {gold}";
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
        if (_roundResolved) return;
        if (_enemyState == null || _playerState == null) return;
        if (cardIndex < 0 || cardIndex >= _currentHand.Count) return;

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
                var dealtDamage = _enemyState.DealDamage(bossDamage);
                var blockedDamage = Mathf.Max(0, bossDamage - dealtDamage);
                _pendingResult["boss_damage"] = (int)_pendingResult["boss_damage"] + dealtDamage;
                bossDamage = dealtDamage;
                ShowEnemyImpact(dealtDamage, blockedDamage);
            }
            else
            {
                _playerState.Heal(bossDamage);
                ShowPlayerSupport($"+{bossDamage}", new Color("#74d99f"));
            }
        }

        _currentHand.RemoveAt(cardIndex);
        FxHelper.PulseControl(GetNode<Button>($"VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_{cardIndex + 1}"), 0.96f, 0.12f);
        FxHelper.PlayOneShot(this, ClickSound, "ClickSound");
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
        if (card.DamageTarget == CombatTarget.Player) playSummary = $"You used {card.Name} to recover {bossDamage}.";

        if (!string.IsNullOrEmpty(damageSummary)) playSummary += $" {damageSummary}.";
        if (!string.IsNullOrEmpty(statusAppliedMessage)) playSummary += $" {statusAppliedMessage}.";

        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            $"{playSummary} {_currentHand.Count} card(s) left.";

        if (_currentHand.Count > 0) return;

        var enemyTurn = _enemyState.ResolveTurn(_playerState);
        UpdateEnemyUi();
        UpdatePlayerUi();
        if (enemyTurn.OutgoingDamage > 0) ShowPlayerDamage(enemyTurn.OutgoingDamage, _enemyState.CurrentIntent.Type == BossIntentType.HeavyAttack);

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
        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text = enemyTurn.Summary;
    }

    private void OnContinuePressed()
    {
        if (!_roundResolved) return;
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
        if (_playerState == null) return;
        var playerBar = GetNode<ProgressBar>("VBox_MainLayout/Row2_PlayerArea/Margin_Player/VBox_PlayerStats/ProgressBar_PlayerHP");
        playerBar.MaxValue = _playerState.MaxHp;
        playerBar.Value = _playerState.CurrentHp;
        GetNode<Label>("VBox_MainLayout/Row2_PlayerArea/Margin_Player/VBox_PlayerStats/Label_PlayerStatus").Text = _playerState.BuildStatusSummary();
    }

    private void UpdateEnemyUi()
    {
        if (_enemyState == null) return;
        var bossBar = GetNode<ProgressBar>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/ProgressBar_BossHP");
        bossBar.MaxValue = _enemyState.MaxHp;
        bossBar.Value = _enemyState.CurrentHp;
        GetNode<Label>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/Label_BossStatus").Text = _enemyState.BuildStatusSummary();
    }

    private string ApplyCardStatus(CardData card)
    {
        if (_enemyState == null || _playerState == null) return string.Empty;
        if (card.StatusEffect.Type == StatusEffectType.None) return string.Empty;

        if (card.StatusTarget == CombatTarget.Enemy)
        {
            _enemyState.ApplyStatus(card.StatusEffect);
            UpdateEnemyUi();
            ShowEnemyStatus(card.StatusEffect.BuildSummary());
        }
        else
        {
            _playerState.ApplyStatus(card.StatusEffect);
            UpdatePlayerUi();
            ShowPlayerSupport(card.StatusEffect.BuildSummary(), new Color("#ffe08a"));
        }

        return $"Applied {card.StatusEffect.BuildSummary()}";
    }

    private void WireButtonFx(Button button)
    {
        button.MouseEntered += () =>
        {
            if (button.Disabled) return;
            FxHelper.PulseControl(button, 1.03f, 0.14f);
            FxHelper.PlayOneShot(this, HoverSound, $"{button.Name}_hover");
        };
    }

    private void ShowEnemyImpact(int damage, int blockedDamage)
    {
        var bossPosition = GetNode<Control>("VBox_MainLayout/Row1_BossArea/BossSpriteAnchor").GlobalPosition;

        if (damage > 0)
        {
            FxHelper.PlayOneShot(this, HitSound, "EnemyHitSound");
            FxHelper.FlashCanvasItem(GetNode<ColorRect>("VBox_MainLayout/Row1_BossArea/BossSpriteAnchor/Sprite_Boss"), Colors.White);
            FxHelper.SpawnFloatingLabel(this, $"-{damage}", bossPosition + new Vector2(96.0f, 80.0f), new Color("#ff8b7d"), 24);
        }

        if (blockedDamage > 0)
        {
            FxHelper.SpawnFloatingLabel(this, $"Armor {blockedDamage}", bossPosition + new Vector2(86.0f, 120.0f), new Color("#9ec0ff"), 16);
        }
    }

    private void ShowEnemyStatus(string text)
    {
        var bossPosition = GetNode<Control>("VBox_MainLayout/Row1_BossArea/BossSpriteAnchor").GlobalPosition;
        FxHelper.SpawnFloatingLabel(this, text, bossPosition + new Vector2(50.0f, 42.0f), new Color("#ffe08a"), 16);
    }

    private void ShowPlayerDamage(int damage, bool heavy)
    {
        var playerPosition = GetNode<Control>("VBox_MainLayout/Row2_PlayerArea/PlayerSpriteAnchor").GlobalPosition;
        FxHelper.PlayOneShot(this, heavy ? HeavyHitSound : HitSound, heavy ? "HeavyHitSound" : "PlayerHitSound");
        FxHelper.FlashCanvasItem(GetNode<ColorRect>("VBox_MainLayout/Row2_PlayerArea/PlayerSpriteAnchor/Sprite_Player"), new Color(1.0f, 0.5f, 0.5f));
        FxHelper.SpawnFloatingLabel(this, $"-{damage}", playerPosition + new Vector2(52.0f, 64.0f), new Color("#ff8b7d"), 24);
    }

    private void ShowPlayerSupport(string text, Color color)
    {
        var playerPosition = GetNode<Control>("VBox_MainLayout/Row2_PlayerArea/PlayerSpriteAnchor").GlobalPosition;
        FxHelper.SpawnFloatingLabel(this, text, playerPosition + new Vector2(42.0f, 36.0f), color, 18);
    }
}
