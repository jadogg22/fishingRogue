using Godot;
using Godot.Collections;

public partial class CombatScene : Control
{
    [Signal]
    public delegate void RoundFinishedEventHandler(Dictionary result);

    private const int BossDamage = 5;
    private const int MaxHandSize = 3;

    private Array<Dictionary> _currentHand = new();
    private Dictionary _pendingResult = new()
    {
        { "player_damage", 0 },
        { "boss_damage", 0 },
    };
    private bool _roundResolved;
    private int _bossHpDuringRound;
    private int _playerHpDuringRound;
    private int _maxBossHp;
    private int _maxPlayerHp;

    public override void _Ready()
    {
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_1").Pressed += () => OnCardPressed(0);
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_2").Pressed += () => OnCardPressed(1);
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/HBox_Hand/Card_Slot_3").Pressed += () => OnCardPressed(2);
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Pressed += OnContinuePressed;
    }

    public void SetupRound(int currentPlayerHp, int maxPlayerHp, int currentBossHp, int maxBossHp, Array<Dictionary> cardData, int battleNumber)
    {
        _currentHand = new Array<Dictionary>(cardData);
        _bossHpDuringRound = currentBossHp;
        _playerHpDuringRound = currentPlayerHp;
        _maxBossHp = maxBossHp;
        _maxPlayerHp = maxPlayerHp;

        GetNode<Label>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/Label_BossName").Text = $"LEVEL 1 BOSS  |  Battle {battleNumber}";
        UpdateHealthBars();
        RefreshHandButtons();

        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            $"Boss intent: {BossDamage} damage. Fish three cards, then spend them here.";
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Disabled = true;

        _roundResolved = false;
        _pendingResult = new Dictionary
        {
            { "player_damage", 0 },
            { "boss_damage", 0 },
        };
    }

    private void OnCardPressed(int cardIndex)
    {
        if (_roundResolved)
        {
            return;
        }

        if (cardIndex < 0 || cardIndex >= _currentHand.Count)
        {
            return;
        }

        var card = _currentHand[cardIndex];
        var bossDamage = (int)card["damage"];
        _bossHpDuringRound = Mathf.Max(0, _bossHpDuringRound - bossDamage);
        _pendingResult["boss_damage"] = (int)_pendingResult["boss_damage"] + bossDamage;
        _currentHand.RemoveAt(cardIndex);

        UpdateHealthBars();
        RefreshHandButtons();

        if (_bossHpDuringRound <= 0)
        {
            _roundResolved = true;
            _pendingResult["player_damage"] = 0;
            GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Disabled = false;
            GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
                $"You finished the boss with {card["name"]}.";
            return;
        }

        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            $"You used {card["name"]} for {bossDamage} damage. {_currentHand.Count} card(s) left.";

        if (_currentHand.Count > 0)
        {
            return;
        }

        _playerHpDuringRound = Mathf.Max(0, _playerHpDuringRound - BossDamage);
        UpdateHealthBars();
        _pendingResult = new Dictionary
        {
            { "player_damage", BossDamage },
            { "boss_damage", _pendingResult["boss_damage"] },
        };
        _roundResolved = true;
        GetNode<Button>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/ContinueButton").Disabled = false;
        GetNode<Label>("VBox_MainLayout/Row3_CardHand/Panel_HandBackground/VBox_HandLayout/Label_Log").Text =
            $"Your hand is spent. The boss hits back for {BossDamage}.";
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
                button.Text = $"{card["name"]}\n{card["fish"]} | {card["damage"]} dmg";
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

    private void UpdateHealthBars()
    {
        var bossBar = GetNode<ProgressBar>("VBox_MainLayout/Row1_BossArea/Margin_Boss/VBox_BossStats/ProgressBar_BossHP");
        bossBar.MaxValue = _maxBossHp;
        bossBar.Value = _bossHpDuringRound;

        var playerBar = GetNode<ProgressBar>("VBox_MainLayout/Row2_PlayerArea/Margin_Player/VBox_PlayerStats/ProgressBar_PlayerHP");
        playerBar.MaxValue = _maxPlayerHp;
        playerBar.Value = _playerHpDuringRound;
    }
}
