using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class GameManager : Control
{
    private const int StartingPlayerHp = 30;
    private const int FishingRoundsPerCombat = 3;

    [Export] public Array<BossDefinition> BossPool { get; set; } = new();

    private int _playerHp = StartingPlayerHp;
    private int _bossHp;
    private int _combatRoundNumber = 1;
    private int _fishingRoundNumber = 1;
    private int _currentBossIndex;
    private int _gold;
    private readonly List<CardData> _currentHand = new();
    private Array<Dictionary> _bossStatuses = new();
    private Array<Dictionary> _playerStatuses = new();
    private Node? _activePhase;

    public override void _Ready()
    {
        StartRun();
    }

    private void StartRun()
    {
        _playerHp = StartingPlayerHp;
        _currentBossIndex = 0;
        _bossHp = CurrentBoss?.MaxHp ?? 100;
        _combatRoundNumber = 1;
        _fishingRoundNumber = 1;
        _gold = 0;
        _currentHand.Clear();
        _bossStatuses = new Array<Dictionary>();
        _playerStatuses = new Array<Dictionary>();
        ShowFishingPhase();
    }

    private void ClearPhase()
    {
        if (IsInstanceValid(_activePhase))
        {
            if (_activePhase.GetParent() != null)
            {
                _activePhase.GetParent().RemoveChild(_activePhase);
            }
            _activePhase.QueueFree();
            _activePhase = null;
        }
    }

    private void ShowFishingPhase()
    {
        ClearPhase();
        var fishingScene = GD.Load<PackedScene>("res://scenes/FishingScene.tscn").Instantiate<FishingScene>();
        _activePhase = fishingScene;
        GetNode<Control>("PhaseContainer").AddChild(fishingScene);
        fishingScene.StartRound(_playerHp, _fishingRoundNumber, CurrentBoss?.Name ?? "Unknown", _gold);
        fishingScene.FishCaught += OnFishCaught;
    }

    private void ShowCombatPhase()
    {
        if (CurrentBoss == null) return;
        ClearPhase();
        var combatScene = GD.Load<PackedScene>("res://scenes/CombatScene.tscn").Instantiate<CombatScene>();
        _activePhase = combatScene;
        GetNode<Control>("PhaseContainer").AddChild(combatScene);
        combatScene.SetupRound(_playerHp, StartingPlayerHp, _bossHp, _currentHand, _playerStatuses, _bossStatuses, CurrentBoss, _combatRoundNumber, _gold);
        combatScene.RoundFinished += OnRoundFinished;
    }

    private void ShowEndScreen(bool victory)
    {
        ClearPhase();
        var endScene = GD.Load<PackedScene>("res://scenes/EndScreen.tscn").Instantiate<EndScreen>();
        _activePhase = endScene;
        GetNode<Control>("PhaseContainer").AddChild(endScene);
        var clearedBosses = Mathf.Max(1, Mathf.Min(_currentBossIndex + 1, BossPool.Count));
        endScene.Setup(victory, _playerHp, _bossHp, clearedBosses);
        endScene.RestartRequested += OnRestartRequested;
    }

    private void OnFishCaught(CardData cardData)
    {
        _currentHand.Add(cardData);

        if (_currentHand.Count >= FishingRoundsPerCombat)
        {
            ShowCombatPhase();
            return;
        }

        _fishingRoundNumber += 1;
        ShowFishingPhase();
    }

    private void OnRoundFinished(Dictionary result)
    {
        _playerHp = (int)result["player_hp"];
        _bossHp = (int)result["boss_hp"];
        _bossStatuses = (Array<Dictionary>)result["boss_statuses"];
        _playerStatuses = (Array<Dictionary>)result["player_statuses"];

        if (_bossHp <= 0)
        {
            if (CurrentBoss != null) _gold += CurrentBoss.RewardGold;
            _currentBossIndex += 1;

            if (_currentBossIndex >= BossPool.Count)
            {
                ShowEndScreen(true);
                return;
            }

            _bossHp = CurrentBoss?.MaxHp ?? 100;
            _bossStatuses = new Array<Dictionary>();
            _combatRoundNumber = 1;
            _fishingRoundNumber = 1;
            _currentHand.Clear();
            ShowFishingPhase();
            return;
        }

        if (_playerHp <= 0)
        {
            ShowEndScreen(false);
            return;
        }

        _combatRoundNumber += 1;
        _fishingRoundNumber = 1;
        _currentHand.Clear();
        ShowFishingPhase();
    }

    private void OnRestartRequested()
    {
        StartRun();
    }

    private BossDefinition? CurrentBoss => _currentBossIndex < BossPool.Count ? BossPool[_currentBossIndex] : null;
}
