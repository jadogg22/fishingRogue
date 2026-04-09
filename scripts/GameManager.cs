using Godot;
using Godot.Collections;

public partial class GameManager : Control
{
    private const int StartingPlayerHp = 30;
    private const int StartingBossHp = 40;
    private const int FishingRoundsPerCombat = 3;

    private int _playerHp = StartingPlayerHp;
    private int _bossHp = StartingBossHp;
    private int _battleNumber = 1;
    private int _fishingRoundNumber = 1;
    private Array<Dictionary> _currentHand = new();
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
        _bossHp = StartingBossHp;
        _battleNumber = 1;
        _fishingRoundNumber = 1;
        _currentHand = new Array<Dictionary>();
        _bossStatuses = new Array<Dictionary>();
        _playerStatuses = new Array<Dictionary>();
        ShowFishingPhase();
    }

    private void ClearPhase()
    {
        if (IsInstanceValid(_activePhase))
        {
            _activePhase.QueueFree();
        }
    }

    private void ShowFishingPhase()
    {
        ClearPhase();
        var fishingScene = GD.Load<PackedScene>("res://scenes/FishingScene.tscn").Instantiate<FishingScene>();
        _activePhase = fishingScene;
        GetNode<Control>("PhaseContainer").AddChild(fishingScene);
        fishingScene.StartRound(_playerHp, _fishingRoundNumber);
        fishingScene.FishCaught += OnFishCaught;
    }

    private void ShowCombatPhase()
    {
        ClearPhase();
        var combatScene = GD.Load<PackedScene>("res://scenes/CombatScene.tscn").Instantiate<CombatScene>();
        _activePhase = combatScene;
        GetNode<Control>("PhaseContainer").AddChild(combatScene);
        combatScene.SetupRound(_playerHp, StartingPlayerHp, _bossHp, StartingBossHp, _currentHand, _playerStatuses, _bossStatuses, _battleNumber);
        combatScene.RoundFinished += OnRoundFinished;
    }

    private void ShowEndScreen(bool victory)
    {
        ClearPhase();
        var endScene = GD.Load<PackedScene>("res://scenes/EndScreen.tscn").Instantiate<EndScreen>();
        _activePhase = endScene;
        GetNode<Control>("PhaseContainer").AddChild(endScene);
        endScene.Setup(victory, _playerHp, _bossHp, _battleNumber);
        endScene.RestartRequested += OnRestartRequested;
    }

    private void OnFishCaught(Dictionary cardData)
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
            ShowEndScreen(true);
            return;
        }

        if (_playerHp <= 0)
        {
            ShowEndScreen(false);
            return;
        }

        _battleNumber += 1;
        _fishingRoundNumber = 1;
        _currentHand.Clear();
        ShowFishingPhase();
    }

    private void OnRestartRequested()
    {
        StartRun();
    }
}
