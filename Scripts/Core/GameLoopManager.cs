using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Deckrinth.Model;
using Deckrinth.Services;
using Deckrinth.Model.Cards;
using Deckrinth.Model.Resources;

namespace Deckrinth.Core;

public enum GameState
{
    InitializingRun,
    GeneratingLevel,
    PlayerTurnStart,
    WaitingForPlayerInput,
    ChestLooting,
    ResolvingPlayerAction,
    EnemyTurn,
    CheckWinCondition,
    ShopPhase,
    LevelComplete,
    GameOver,
    AutoWalking
}

public class GameLoopManager
{
    public GameState CurrentState { get; private set; }
    private int _currentSaveSlot;
    
    // Core Data
    public RunState RunData { get; private set; }
    public GridModel CurrentGrid { get; private set; }
    public Entity Player { get; private set; }
    public List<Entity> Enemies { get; private set; }
    public Vector2I CurrentExitPos => _currentExitPos;
    public int CurrentSkulls { get; private set; } = 0;
    
    // Services
    private LevelGeneratorService _levelGenerator;
    private PathfindingService _pathfinder;
    private EnemyAIService _enemyAI;
    private Dictionary<Entity, EnemyIntent> _lockedIntents = new Dictionary<Entity, EnemyIntent>();
    private CombatService _combatService;
    private AchievementManager _achievementManager;
    private PlayerProfile _activeProfile;
    public DeckManager DeckLogic { get; private set; }
    public ShopManager ShopLogic { get; private set; } = new ShopManager();

    // Events for Godot (View Layer) to listen to
    public event Action<GameState> OnStateChanged;
    public event Action<Dictionary<Entity, EnemyIntent>> OnEnemyIntentsCalculated;
    public event Action<Dictionary<Entity, Vector2I>> OnEnemiesMoved;
    public event Action OnExitUnlocked;
    public event Action OnPlayerDeath;
    public event Action OnLevelTransition;
    public event Action<Card> OnChestLootingStarted;
    public event Action<RunState, List<UnlockableDisplayData>, PlayerProfile> OnGameOver;
    public event Action<int> OnSkullsChanged;
    public event Action OnShopPhaseStarted;
    public event Action<List<Vector2I>> OnAutoWalkStarted;
    public event Action<string, Color> OnFloatingTextRequested;

    private bool _isExitUnlocked;
    private Vector2I _currentExitPos;

    public GameLoopManager()
    {
        _levelGenerator = new LevelGeneratorService();
        CurrentState = GameState.InitializingRun;
    }

    public void StartNewRun(PlayerProfile profile, int slotIndex, List<Model.Resources.UnlockableItemResource> allUnlockables)
    {
        _activeProfile = profile;
        _currentSaveSlot = slotIndex;
        RunData = new RunState();
        _achievementManager = new AchievementManager(_activeProfile, RunData, _currentSaveSlot, allUnlockables);
        
        Player = new Entity 
        { 
            Id = "player", 
            Faction = EntityFaction.Player, 
            Tier = EnemyTier.None,
            HasShield = false,
            HasSecondWind = false, 
        };
        
        Enemies = new List<Entity>();
        _combatService = new CombatService(CurrentGrid, Enemies, Player);
        _enemyAI = new EnemyAIService(CurrentGrid, new PathfindingService(CurrentGrid), Enemies, Player);
        DeckLogic = new DeckManager();
        
        // One of each starting card
        List<Card> startingDeck = new List<Card> 
        {
            new CoreMovementCard(),
            new DiagonalStrikeCard()
        };

        // We register the starting deck in the run data and initialize the deck logic with it
        RunData.ActiveDeck.AddRange(startingDeck);

        DeckLogic.InitializeDeck(startingDeck, initialDraw: 2);
        ChangeState(GameState.GeneratingLevel);
    }
    public void ResumeRun(PlayerProfile profile, RunState loadedRunState, int slotIndex, List<Model.Resources.UnlockableItemResource> allUnlockables)
    {
        _activeProfile = profile;
        _currentSaveSlot = slotIndex;
        RunData = loadedRunState;
        
        RunData.RehydrateDecks();
        
        _achievementManager = new AchievementManager(_activeProfile, RunData, _currentSaveSlot, allUnlockables);
        
        Player = new Entity 
        { 
            Id = "player", 
            Faction = EntityFaction.Player, 
            Tier = EnemyTier.None,
            HasShield = false,
            HasSecondWind = false, 
        };
        
        Enemies = new List<Entity>();
        _combatService = new CombatService(CurrentGrid, Enemies, Player);
        _enemyAI = new EnemyAIService(CurrentGrid, new PathfindingService(CurrentGrid), Enemies, Player);
        DeckLogic = new DeckManager();
        
        // We load the pack using the saved deck
        DeckLogic.InitializeDeck(RunData.ActiveDeck, initialDraw: 2);
        
        // Generate current floor
        ChangeState(GameState.GeneratingLevel);
    }

    public void ProcessEnemyTurn()
    {
        if (CurrentState != GameState.EnemyTurn) return;

        Dictionary<Entity, Vector2I> newPositions = new Dictionary<Entity, Vector2I>();

        try
        {
            // We interpret the locked intents and move the enemies accordingly, while also handling combat interactions
            foreach (var enemy in Enemies)
            {
                if (enemy.IsDead || enemy.IsRooted) continue;

                if (_lockedIntents.TryGetValue(enemy, out EnemyIntent intent))
                {
                    if (intent.Type == IntentType.Move || intent.Type == IntentType.AttackPlayer)
                    {
                        // Enemy attempts to move or attack based on the locked intent
                        Vector2I direction = intent.TargetPosition - enemy.Position;
                        if (direction != Vector2I.Zero)
                        {
                            _combatService.AttemptMoveOrAttack(enemy, direction, out _, out _);
                        }
                    }
                }
                newPositions[enemy] = enemy.Position;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("CRASH IN AI DETECTED: " + e.Message + "\n" + e.StackTrace);
        }

        OnEnemiesMoved?.Invoke(newPositions);
    }

    public void EndEnemyTurn()
    {
        ChangeState(GameState.CheckWinCondition);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
        ExecuteState();
    }

    private void ExecuteState()
    {
        switch (CurrentState)
        {
            case GameState.GeneratingLevel:
                GenerateNewFloor();
                break;
                
            case GameState.PlayerTurnStart:
                DeckLogic.ProcessTurnStart();
                _lockedIntents = _enemyAI.ComputeEnemyIntents();
                OnEnemyIntentsCalculated?.Invoke(_lockedIntents);
                ChangeState(GameState.WaitingForPlayerInput);
                break;
            
            case GameState.EnemyTurn:
                DeckLogic.ProcessTurnEnd();
                break;

            case GameState.ShopPhase:
                // When entering the shop phase, we initialize the shop with the player's unlocked cards and the current run data
                ShopLogic.InitializeShopPhase(_activeProfile.UnlockedCardIds, RunData);
                OnShopPhaseStarted?.Invoke();
                break;
                
            case GameState.WaitingForPlayerInput:
            case GameState.GameOver:
                if (CurrentState == GameState.GameOver) OnPlayerDeath?.Invoke();
                break;

            case GameState.CheckWinCondition:
                EvaluateWinCondition();
                break;
        }
    }

    private void GenerateNewFloor()
    {
        _isExitUnlocked = false;
        
        _levelGenerator.SetupNextLevel(RunData.CurrentDepth, _activeProfile.UnlockedEnemyIds, out GridModel newGrid, out List<Entity> newEnemies, out Vector2I startPos, out Vector2I exitPos);
        _achievementManager.RecordStat("max_depth", RunData.CurrentDepth, replaceIfHigher: true);
        CurrentGrid = newGrid;
        Enemies = newEnemies;
        _currentExitPos = exitPos;
        foreach (var enemy in Enemies)
        {
            _achievementManager.RecordStat($"seen_{enemy.Id}", 1);
        }
        SaveManager.SaveProfile(_activeProfile, _currentSaveSlot);
        Player.Position = startPos;
        _pathfinder = new PathfindingService(CurrentGrid);
        _enemyAI = new EnemyAIService(CurrentGrid, _pathfinder, Enemies, Player);
        
        List<Entity> allEntities = new List<Entity> { Player };
        allEntities.AddRange(Enemies);
        _combatService = new CombatService(CurrentGrid, allEntities, Player);
        
        OnLevelTransition?.Invoke();
        ChangeState(GameState.PlayerTurnStart);
    }

    public Card CurrentChestLoot { get; private set; }
    private GameState _pendingStateAfterChest;

    public void TryPlayCard(Card card, Vector2I targetPosition)
    {
        if (CurrentState != GameState.WaitingForPlayerInput) return;
        if (Player.IsRooted && card.Category == CardCategory.Movement) return;

        ChangeState(GameState.ResolvingPlayerAction);
        bool success = card.Play(Player, CurrentGrid, targetPosition);

        if (success)
        {
            bool freeAction = false;
            bool chestOpened = false;
            DeckLogic.OnCardPlayed(card);

            if (card.Category == CardCategory.Movement || card.Category == CardCategory.Transformation)
            {
                Vector2I direction = targetPosition - Player.Position;
                _combatService.AttemptMoveOrAttack(Player, direction, out freeAction, out chestOpened);
                Player.IsRooted = false; 
            }
            else if (card.Category == CardCategory.Attack || card.Category == CardCategory.Spell)
            {
                List<Vector2I> affectedTiles = card.GetAoETiles(targetPosition, CurrentGrid);
                foreach(Vector2I tile in affectedTiles)
                {
                    _combatService.ExecuteRangedAttack(tile);
                }
            }

            GameState nextState = card.EndsTurn ? GameState.EnemyTurn : GameState.PlayerTurnStart;
            if (freeAction) nextState = GameState.PlayerTurnStart;

            if (chestOpened)
            {
                CurrentChestLoot = GenerateRandomLoot();
                _pendingStateAfterChest = nextState; 
                ChangeState(GameState.ChestLooting);
                OnChestLootingStarted?.Invoke(CurrentChestLoot);
            }
            else
            {
                ChangeState(nextState);
            }
        }
        else
        {
            ChangeState(GameState.WaitingForPlayerInput);
        }
    }

    public void ResolveChestLoot(bool takeCard)
    {
        if (CurrentState != GameState.ChestLooting) return;

        if (takeCard && CurrentChestLoot != null)
        {
            if (CurrentChestLoot.Category == CardCategory.Passive)
            {
                RunData.PassiveBuild.Add(CurrentChestLoot);
                CurrentChestLoot.OnEquipPassive(this);
            }
            else
            {
                RunData.ActiveDeck.Add(CurrentChestLoot);
                RunData.CurrentHand.Add(CurrentChestLoot);
            }
        }

        CurrentChestLoot = null;
        ChangeState(_pendingStateAfterChest);
    }

    private Card GenerateRandomLoot()
    {
        List<Card> validPool = CardRegistry.GetAllCardsMasterList()
            .Where(c => _activeProfile.UnlockedCardIds.Contains(c.Id))
            .Where(c => !RunData.ExhaustedCardIds.Contains(c.Id))
            .Where(c => c.Category != CardCategory.Transformation)
            .Where(c => RunData.GetCardCount(c.Id) < c.MaxCopiesInDeck) 
            .ToList();

        if (validPool.Count == 0) return null;

        Random rng = new Random();
        int totalWeight = validPool.Sum(c => c.DropWeight);
        int randomValue = rng.Next(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (Card card in validPool)
        {
            cumulativeWeight += card.DropWeight;
            if (randomValue < cumulativeWeight)
            {
                return CardRegistry.CreateCardInstance(card.Id);
            }
        }
        
        return CardRegistry.CreateCardInstance(validPool[0].Id);
    }

    private void EvaluateWinCondition()
    {
        if (Player.IsDead)
        {
            SaveManager.SaveProfile(_activeProfile, _currentSaveSlot); // Save Meta-Progression (Lifetime Stats, Unlocked Cards/Enemies)
            SaveManager.DeleteActiveRun(_currentSaveSlot);
            ChangeState(GameState.GameOver);
            OnGameOver?.Invoke(RunData, _achievementManager.UnlocksThisRun, _activeProfile);
            return;
        }

        var deadEnemies = Enemies.Where(e => e.IsDead).ToList();
        if (deadEnemies.Count > 0)
        {
            _achievementManager.RecordStat("total_kills", deadEnemies.Count);
            foreach (var cadaver in deadEnemies)
            {
                _achievementManager.RecordStat($"kill_{cadaver.Id}", 1); // Will result in 'kill_enemyId' stat keys for each enemy type
                AddSkullsFromKill(cadaver.SkullsDrop);
            }
            Enemies.RemoveAll(e => e.IsDead);
        }

        if (Enemies.Count == 0 && !_isExitUnlocked)
        {
            _isExitUnlocked = true;
            OnExitUnlocked?.Invoke();
            int[,] flowField = _pathfinder.GenerateFlowField(_currentExitPos);
            
            List<Vector2I> path = new List<Vector2I>();
            Vector2I currentPos = Player.Position;
            path.Add(currentPos);

            Vector2I[] movePattern = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
            int safetyCounter = 0; // We prevent infinite loops by limiting the number of iterations to 1000, which should be more than enough for any reasonable grid size.

            // We generate a path from the lowest flow field value to the exit, which will be used for auto-walking
            while (currentPos != _currentExitPos && safetyCounter < 1000)
            {
                safetyCounter++;
                int bestScore = flowField[currentPos.X, currentPos.Y];
                Vector2I nextStep = currentPos;

                foreach (Vector2I dir in movePattern)
                {
                    Vector2I neighbor = currentPos + dir;
                    if (CurrentGrid.IsInBounds(neighbor) && CurrentGrid.IsCellWalkable(neighbor))
                    {
                        if (flowField[neighbor.X, neighbor.Y] < bestScore)
                        {
                            bestScore = flowField[neighbor.X, neighbor.Y];
                            nextStep = neighbor;
                        }
                    }
                }

                // If the it is blocked (no better move found)
                if (nextStep == currentPos) break; 
                
                currentPos = nextStep;
                path.Add(currentPos);
            }
            
            if (path.Count > 1 && currentPos == _currentExitPos)
            {
                ChangeState(GameState.AutoWalking);
                OnAutoWalkStarted?.Invoke(path);
                return; // We stop here and wait for the auto-walk to complete before transitioning to the next state
            }
            else
            {
                // If no path is found, we can either log an error or handle it gracefully. For now, we'll just transition to the next state.
                ReachPortal();
                return;
            }
        }
        ChangeState(GameState.PlayerTurnStart);
    }
    public void ReachPortal()
    {
        if (RunData.CurrentDepth % 3 == 0) ChangeState(GameState.ShopPhase);
        else 
        {
            RunData.CurrentDepth++;
            ChangeState(GameState.GeneratingLevel);
        }
    }
    public void ManualEndTurn()
    {
        if (CurrentState != GameState.WaitingForPlayerInput) return;

        bool reducedCooldowns = DeckLogic.ReduceAllCooldowns(1);
        
        if (reducedCooldowns)
        {
            // Blue text
            OnFloatingTextRequested?.Invoke("-1 Cooldowns", new Color(0.2f, 0.8f, 1f));
        }
        else
        {
            // Calculate 10% of current skulls, rounded up, with a minimum of 1
            int bonus = Mathf.Max(1, Mathf.RoundToInt(CurrentSkulls * 0.1f));
            AddSkulls(bonus);
            // Gold text
            OnFloatingTextRequested?.Invoke($"+{bonus} Skulls", new Color(1f, 0.8f, 0.2f));
        }

        ChangeState(GameState.EnemyTurn);
    }
    public void LeaveShop()
    {
        if (CurrentState != GameState.ShopPhase) return;

        // We replenish the player's deck from the shop's offerings before moving to the next level
        DeckLogic.ReplenishFromShop();

        // We advance the depth and transition to generating the next level
        RunData.CurrentDepth++;
        ChangeState(GameState.GeneratingLevel);
    }

    public void AddSkulls(int amount)
    {
       int finalAmount = Godot.Mathf.RoundToInt(amount * RunData.SkullBonusMultiplier);
        RunData.AddSkulls(finalAmount);
        // We also update the lifetime earned skulls in the profile for meta-progression tracking
        _activeProfile.TotalLifetimeSkulls += finalAmount;
        OnSkullsChanged?.Invoke(RunData.Skulls);
    }
    public void AddSkullsFromKill(int baseAmount)
    {
        // We add the skulls to the current run's economy first, which will also update the lifetime stats in the profile
        int finalYield = RunData.AddSkullsFromKill(baseAmount);
        
        // We add it to the profile's lifetime skulls for meta-progression tracking
        _activeProfile.TotalLifetimeSkulls += finalYield;
        
        // Announcing the UI update for the current skulls after the kill
        OnSkullsChanged?.Invoke(RunData.Skulls);
    }

    public bool TrySpendSkulls(int amount)
    {
        if (RunData.TrySpendSkulls(amount))
        {
            // We also update the lifetime spent skulls in the profile for meta-progression tracking
            _activeProfile.LifetimeSkullsSpent += amount; 
            OnSkullsChanged?.Invoke(RunData.Skulls);
            return true;
        }
        return false;
    }

    public void AcquireCard(Card card)
    {
        _achievementManager.RecordStat("total_cards_bought", 1);
        if (card.Category == CardCategory.Passive)
        {
            RunData.PassiveBuild.Add(card);
            card.OnEquipPassive(this); 
        }
        else
        {
            // Replacement logic for Transformation cards
            if (card.Category == CardCategory.Transformation)
            {
                RunData.ActiveDeck.RemoveAll(c => c.Category == CardCategory.Transformation);
                
                // Removing it from everywhere in the deck logic to ensure it doesn't appear in the draw pile, hand, or discard pile
                DeckLogic.DrawPile.RemoveAll(c => c.Category == CardCategory.Transformation);
                DeckLogic.Hand.RemoveAll(c => c.Category == CardCategory.Transformation);
                DeckLogic.DiscardPile.RemoveAll(c => c.Category == CardCategory.Transformation);
            }

            // After we handle any necessary removals, we add the new card to the active deck and the discard pile
            RunData.ActiveDeck.Add(card);
            DeckLogic.DiscardPile.Add(card); 
        }
    }
}