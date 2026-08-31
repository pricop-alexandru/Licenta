using Godot;
using System.Collections.Generic;
using Deckrinth.Model;
using Deckrinth.Services;
using Deckrinth.View;
using Deckrinth.Model.Cards;
using Deckrinth.Model.Resources;

namespace Deckrinth.Core;

public partial class GameRoot : Node2D
{
    [Export] public GridManager GridView { get; set; }
    [Export] public CameraController MainCamera { get; set; }
    [Export] public PackedScene EntityPrefab { get; set; }
    [Export] public PackedScene PortalPrefab { get; set; }
    [Export] public ChestLootUI ChestUI { get; set; }
    [Export] public Godot.Collections.Dictionary<string, PackedScene> EnemyPrefabs { get; set; }
    [Export] public HandUI PlayerHandUI { get; set; }
    [Export] public CurrencyUI TopRightCurrencyUI { get; set; }
    [Export] public DeckOverlayUI DeckViewerUI { get; set; }
    [Export] public ShopUI ShopMenuUI { get; set; } // Reference to shop UI for handling shop interactions
    [Export] public MainMenuUI MainMenuView { get; set; } 
    [Export] public CanvasLayer GameplayUILayer { get; set; } // To hide the gameplay UI when in the main menu
    [Export] public Label DepthLabel { get; set; }
    [Export] public PreRunMenuUI PreRunMenuView { get; set; }
    [Export] public PauseMenuUI PauseMenuView { get; set; }
    [Export] public SettingsMenuUI SettingsMenuView { get; set; }
    [Export] public StatsMenuUI StatsMenuView { get; set; }
    [Export] public GameOverUI GameOverView { get; set; }
    [Export] public UnlocksMenuUI UnlocksMenuView { get; set; }
    [Export] public Godot.Collections.Array<UnlockableItemResource> AllUnlockablesData { get; set; }
    private Card _selectedCard = null; 
    private Dictionary<Entity, EntityView> _enemyVisuals = new Dictionary<Entity, EntityView>();
    private List<Node2D> _hoverHighlights = new List<Node2D>();
    private GameLoopManager _gameLoop;
    private EntityView _playerVisual;
    private Node2D _portalVisual;
    private bool _isVisualAnimating = false;
    private PlayerProfile _selectedProfile;
    private int _selectedSlot;
    private bool _openedUnlocksFromPause = false;
    

    public override void _Ready()
    {
        CardRegistry.Initialize();
        EnemyRegistry.Initialize();
        SaveManager.Initialize(); // Makes sure the save system is ready before we start the game loop
        
        DeckViewerUI.Setup();
        ShopMenuUI.Setup();
        
        // We hide the gameplay UI layer when we are in the main menu, and show it again when we start a game
        if (GameplayUILayer != null) GameplayUILayer.Visible = false;

        // Prepare main menu view and connect its signals
        MainMenuView.Setup();
        MainMenuView.OnSlotSelected += HandleSlotSelected;

        if (StatsMenuView != null)
        {
            StatsMenuView.Setup();
            StatsMenuView.OnReturnRequested += () => {
                StatsMenuView.Visible = false;
                PreRunMenuView.Visible = true; // We return where we started from
            };
        }

        if (UnlocksMenuView != null)
        {
            UnlocksMenuView.Setup();
            UnlocksMenuView.OnReturnRequested += () => 
            {
                // When we return from the unlocks tab, we check where we came from
                if (_openedUnlocksFromPause)
                {
                    PauseMenuView.Visible = true;
                }
                else
                {
                    PreRunMenuView.Visible = true;
                    // We make sure the notif updates
                    PreRunMenuView.ShowMenu(_selectedProfile, _selectedSlot); 
                }
            };
        }

        PreRunMenuView.Setup();
        PreRunMenuView.OnNewRunRequested += HandleNewRun;
        PreRunMenuView.OnContinueRunRequested += HandleContinueRun;
        PreRunMenuView.OnReturnRequested += () => {
        PreRunMenuView.HideMenu();
        MainMenuView.Visible = true;
        };
        PreRunMenuView.OnUnlocksRequested += () => {
            _openedUnlocksFromPause = false;
            PreRunMenuView.HideMenu();
            UnlocksMenuView.ShowMenu(_selectedProfile, new List<UnlockableItemResource>(AllUnlockablesData));
        };
        PreRunMenuView.OnStatsRequested += () => {
            PreRunMenuView.Visible = false;
            StatsMenuView.ShowStats(_selectedProfile);
        };
        PauseMenuView.Setup();
        PauseMenuView.OnAbandonRequested += HandleAbandonRun;
        PauseMenuView.OnSaveAndQuitToMain += () => HandleSaveAndQuit(toDesktop: false);
        PauseMenuView.OnSaveAndQuitToDesktop += () => HandleSaveAndQuit(toDesktop: true);
        PauseMenuView.OnUnlocksRequested += () => {
            _openedUnlocksFromPause = true;
            PauseMenuView.Visible = false;
            UnlocksMenuView.ShowMenu(_selectedProfile, new List<UnlockableItemResource>(AllUnlockablesData));
        };
        SettingsMenuView.Setup();
        SettingsMenuView.Visible = false;
        PauseMenuView.OnSettingsRequested += () => SettingsMenuView.Visible = true;
        MainMenuView.OnSettingsRequested += () => SettingsMenuView.Visible = true;
        
        if (PlayerHandUI != null)
        {
            PlayerHandUI.OnEndTurnRequested += HandleManualEndTurn; // We connect the end turn button to the manual end turn handler
        }
        if (GameOverView != null)
        {
            GameOverView.Setup();
            GameOverView.OnReturnToMenuRequested += () => 
            {
                GetTree().ReloadCurrentScene(); // We reload the scene to reset everything and go back to the main menu
            }; // Side note: this is painful and for a player it is a hassle to return to main menu every time they finish a run instead of the pre-run menu
        } // But it is safer because it clears up everything memory-wise from any calculation and lingering effects clutter
    }

    // This method is called when the player starts a new game or loads an existing profile
    private void StartGameCore(PlayerProfile loadedProfile, int slotIndex, bool isContinue)
    {
        if (GameplayUILayer != null) GameplayUILayer.Visible = true; // Show back gameplay UI when we start a game
        if (PauseMenuView != null) PauseMenuView.SetProfile(loadedProfile);
        _gameLoop = new GameLoopManager();
        _gameLoop.OnLevelTransition += HandleLevelTransition;
        _gameLoop.OnChestLootingStarted += ChestUI.ShowLoot;
        ChestUI.OnLootResolved += HandleChestDecision;
        _gameLoop.OnEnemiesMoved += AnimateEnemyTurn;
        _gameLoop.OnGameOver += HandleGameOver;
        PlayerHandUI.OnCardSelectedAction += HandleCardSelected;
        _gameLoop.OnEnemyIntentsCalculated += HandleEnemyIntents;
        _gameLoop.OnSkullsChanged += TopRightCurrencyUI.UpdateSkullsDisplay;
        DeckViewerUI.OnOverlayOpenedRequested += PopulateDeckOverlay;

        _gameLoop.OnAutoWalkStarted += HandleAutoWalk;
        _gameLoop.OnFloatingTextRequested += SpawnFloatingText;
        _gameLoop.OnShopPhaseStarted += HandleShopPhaseStarted;
        ShopMenuUI.OnLeaveRequested += HandleLeaveShop;
        ShopMenuUI.OnRerollRequested += HandleRerollShop;
        ShopMenuUI.OnBuyRequested += HandleBuyCardShop;
        
        // Failsafe if AllUnlockablesData is empty, we just pass an empty array
        var safeArray = AllUnlockablesData ?? new Godot.Collections.Array<UnlockableItemResource>();
        List<Model.Resources.UnlockableItemResource> allUnlockables = new List<Model.Resources.UnlockableItemResource>(safeArray);
        if (isContinue)
        {
            RunState savedRun = SaveManager.LoadActiveRun(slotIndex);
            if (savedRun != null)
            {
                // Load Current run
                _gameLoop.ResumeRun(loadedProfile, savedRun, slotIndex, allUnlockables);
            }
            else
            {
                // Safety fallback
                _gameLoop.StartNewRun(loadedProfile, slotIndex, allUnlockables);
            }
        }
        else
        {
            // New run
            _gameLoop.StartNewRun(loadedProfile, slotIndex, allUnlockables);
        }
    }
    private void HandleManualEndTurn()
    {
        // We dont let it happen if the game is not in the right state or if we are currently animating something
        if (_gameLoop.CurrentState != GameState.WaitingForPlayerInput || _isVisualAnimating) return;

        _isVisualAnimating = true;
        GridView.ClearPlayerHighlights();

        // We tell the game loop to process the end of the player's turn, which will handle enemy actions and any other game logic
        _gameLoop.ManualEndTurn(); 

        // Instantly update the hand and deck count to reflect the changes after the player's turn ends
        PlayerHandUI.UpdateHand(_gameLoop.DeckLogic.Hand);
        DeckViewerUI.UpdateDeckCount(_gameLoop.RunData.ActiveDeck.Count);

        // 0.5s delay before we start the enemy turn to give the player a moment to see their end of turn effects
        GetTree().CreateTimer(0.5f).Timeout += () => 
        {
            _gameLoop.ProcessEnemyTurn(); 
        };
    }
    private void HandleShopPhaseStarted()
    {
        // When we enter the shop phase, we show the shop UI with the current offers and reroll cost
        ShopMenuUI.ShowShop(_gameLoop.ShopLogic.CurrentOffers, _gameLoop.ShopLogic.RerollCost);
        if (PlayerHandUI.EndTurnButton != null) PlayerHandUI.EndTurnButton.Visible = false;
    }

    private void HandleLeaveShop()
    {
        ShopMenuUI.HideShop();
        _gameLoop.LeaveShop(); // We tell the game loop that we are leaving the shop and want to continue to the next level
    }

    private void HandleRerollShop()
    {
        // Run logic to reroll the shop offers, spending the player's skulls if they have enough
        if (_gameLoop.ShopLogic.TryReroll(_gameLoop.TrySpendSkulls))
        {
            ShopMenuUI.RefreshGrid(_gameLoop.ShopLogic.CurrentOffers);
            
            // Update the reroll cost display in the shop UI
            ShopMenuUI.UpdateRerollCost(_gameLoop.ShopLogic.RerollCost);
            
            // Refresh the skulls display in the top-right currency UI
            TopRightCurrencyUI.UpdateSkullsDisplay(_gameLoop.CurrentSkulls);
        }
    }

    private void HandleBuyCardShop(int slotIndex)
    {
        // We attempt to buy the card at the given slot index, spending the player's skulls if they have enough and acquiring the card if successful
        if (_gameLoop.ShopLogic.TryBuyCard(slotIndex, _gameLoop.TrySpendSkulls, _gameLoop.AcquireCard))
        {
            ShopMenuUI.RefreshGrid(_gameLoop.ShopLogic.CurrentOffers);
            // We update the number on the button to reflect the new number of copies of that card in the player's deck
            DeckViewerUI.UpdateDeckCount(_gameLoop.RunData.ActiveDeck.Count); 
        }
    }

    private void AnimateEnemyTurn(Dictionary<Entity, Vector2I> enemyPositions)
    {
        _isVisualAnimating = true;
        GridView.ClearEnemyHighlights();

        foreach (var kvp in enemyPositions)
        {
            Entity enemyLogic = kvp.Key;
            Vector2I newMathPos = kvp.Value;

            if (_enemyVisuals.TryGetValue(enemyLogic, out EntityView enemyView))
            {
                if (IsInstanceValid(enemyView))
                {
                    enemyView.MoveTo(newMathPos);
                }
            }
        }
        _playerVisual.MoveTo(_gameLoop.Player.Position);

        GetTree().CreateTimer(0.35f).Timeout += () => 
        {
            _isVisualAnimating = false;
            _gameLoop.EndEnemyTurn();
            
            if (_gameLoop.CurrentState == GameState.WaitingForPlayerInput)
            {
                StartPlayerTurnVisuals(); 
            }
        };
    }

    private void HandleGameOver(RunState finalRunState, List<UnlockableDisplayData> newUnlocks, PlayerProfile profile)
    {
        if (IsInstanceValid(_playerVisual))
        {
            Tween deathTween = _playerVisual.CreateTween();
            deathTween.TweenProperty(_playerVisual, "scale", Vector2.Zero, 0.4f)
                      .SetTrans(Tween.TransitionType.Back)
                      .SetEase(Tween.EaseType.In);
        }

        // We wait for the death animation to finish (even if its a simple one right now)
        GetTree().CreateTimer(1.5f).Timeout += () => 
        {
            // We hide the gameplay UI for less screen clutter
            if (GameplayUILayer != null) GameplayUILayer.Visible = false;
            if (MainCamera != null) MainCamera.ResetCamera();
            // We show the game over screen
            if (GameOverView != null) 
                GameOverView.ShowGameOver(finalRunState, newUnlocks, profile);
        };
    }

    private void HandleLevelTransition()
    {
        GridView.RenderGridWithAnimation(_gameLoop.CurrentGrid);
        if (PlayerHandUI.EndTurnButton != null) PlayerHandUI.EndTurnButton.Visible = true;
        float centerX = (_gameLoop.CurrentGrid.Width - 1) / 2f;
        float centerY = (_gameLoop.CurrentGrid.Height - 1) / 2f;
        MainCamera.FocusOnPosition(GridView.LogicalToIsometric(new Vector2(centerX, centerY)));
        if (DepthLabel != null) DepthLabel.Text = $"Depth: {_gameLoop.RunData.CurrentDepth}";
        SpawnPortal();
        SpawnPlayer();
        SpawnEnemiesVisuals();
    }

    private void SpawnPortal()
    {
        if (_portalVisual != null) _portalVisual.QueueFree();

        _portalVisual = PortalPrefab.Instantiate<Node2D>();
        AddChild(_portalVisual);
        if (_portalVisual.HasNode("AnimationPlayer"))
        {
            var animPlayer = _portalVisual.GetNode<AnimationPlayer>("AnimationPlayer");
            animPlayer.Play("opening");
            animPlayer.Advance(0); // Applies the texture of the first frame
            animPlayer.Pause();
        }
        float totalTiles = _gameLoop.CurrentGrid.Width * _gameLoop.CurrentGrid.Height;
        float delayForPortal = (totalTiles * 0.05f) + 0.1f;
        Vector2 targetScreenPos = GridView.LogicalToIsometric(_gameLoop.CurrentExitPos);
        
        _portalVisual.Position = targetScreenPos + new Vector2(0, -500); 
        _portalVisual.Modulate = new Color(1, 1, 1, 0); 

        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(_portalVisual, "position", targetScreenPos, 0.4f)
             .SetDelay(delayForPortal).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_portalVisual, "modulate", new Color(1f, 1f, 1f, 0.3f), 0.2f).SetDelay(delayForPortal);
        // We play the "opening animation after portal lands on the ground, and then we switch to the idle animation
        tween.TweenCallback(Callable.From(() => {
            if (_portalVisual.HasNode("AnimationPlayer"))
            {
                var animPlayer = _portalVisual.GetNode<AnimationPlayer>("AnimationPlayer");
                animPlayer.Play("opening");
                // After opening, it goes into idle
                animPlayer.AnimationFinished += (animName) => {
                    if (animName == "opening") animPlayer.Play("idle");
                };
            }
        }));
    }
    private void HandleAutoWalk(List<Vector2I> path)
    {
        _isVisualAnimating = true;
        GridView.ClearPlayerHighlights();
        if (_portalVisual != null)
        {
            Tween portalGlowTween = CreateTween();
            portalGlowTween.TweenProperty(_portalVisual, "modulate:a", 1.0f, 0.5f);
        }
        _playerVisual.WalkPath(path, () => {
            Tween playerTween = CreateTween().SetParallel(true);
            
            // Jumps a bit up to give a sense of movement
            playerTween.TweenProperty(_playerVisual, "position:y", _playerVisual.Position.Y - 40f, 0.3f)
                       .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            
            // Gets smaller and fades out
            playerTween.TweenProperty(_playerVisual, "scale", Vector2.Zero, 0.3f);
            playerTween.TweenProperty(_playerVisual, "modulate:a", 0f, 0.3f);

            // After the player has finished the animation, we trigger the portal opening and the level transition
            playerTween.Chain().TweenCallback(Callable.From(() => {
                if (_portalVisual != null && _portalVisual.HasNode("AnimationPlayer")) 
                {
                    var animPlayer = _portalVisual.GetNode<AnimationPlayer>("AnimationPlayer");
                    animPlayer.Play("closing");
                    
                    // Timer
                    GetTree().CreateTimer(1.6f).Timeout += _gameLoop.ReachPortal;
                } 
                else 
                {
                    _gameLoop.ReachPortal(); // Failsafe
                }
            }));
        });
    }
    private void SpawnFloatingText(string text, Color color)
    {
        Label floatingLabel = new Label
        {
            Text = text,
            Modulate = color,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        floatingLabel.AddThemeFontSizeOverride("font_size", 28);
        floatingLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        floatingLabel.AddThemeConstantOverride("outline_size", 6);
        
        // Slightly above the player, with a small offset to make it look better
        floatingLabel.Position = _playerVisual.Position + new Vector2(-50, -80);
        AddChild(floatingLabel);

        // Moves up in a smooth manner and fades out over 1 second, then removes itself from the scene tree
        Tween tween = CreateTween().SetParallel(true);
        tween.TweenProperty(floatingLabel, "position:y", floatingLabel.Position.Y - 60f, 1.0f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(floatingLabel, "modulate:a", 0f, 1.0f).SetTrans(Tween.TransitionType.Sine).SetDelay(0.3f);
        tween.Chain().TweenCallback(Callable.From(floatingLabel.QueueFree)); // Delete from screen
    }
    private void SpawnPlayer()
    {
        if (_playerVisual != null) _playerVisual.QueueFree();

        _playerVisual = EntityPrefab.Instantiate<EntityView>();
        AddChild(_playerVisual);
        _playerVisual.GetNode<Sprite2D>("Sprite2D").Modulate = new Color(0.2f, 0.8f, 0.2f);

        float totalTiles = _gameLoop.CurrentGrid.Width * _gameLoop.CurrentGrid.Height;
        float delayForPlayer = (totalTiles * 0.05f) + 0.3f;

        _playerVisual.Initialize(GridView, _gameLoop.Player.Position, delayForPlayer, "You");
        GetTree().CreateTimer(delayForPlayer + 0.4f).Timeout += StartPlayerTurnVisuals;
    }

    private void StartPlayerTurnVisuals()
    {
        _isVisualAnimating = false;
        _selectedCard = null; 
        GridView.ClearPlayerHighlights();
        foreach (var hl in _hoverHighlights) if (IsInstanceValid(hl)) hl.QueueFree();
        _hoverHighlights.Clear();
        PlayerHandUI.UpdateHand(_gameLoop.DeckLogic.Hand); 
        DeckViewerUI.UpdateDeckCount(_gameLoop.RunData.ActiveDeck.Count);
    }

    private void HandleCardSelected(Card selectedCard)
    {
        if (_gameLoop.CurrentState != GameState.WaitingForPlayerInput || _isVisualAnimating) return;

        _selectedCard = selectedCard;
        GridView.ClearPlayerHighlights();

        _selectedCard.GetValidTargets(_gameLoop.Player, _gameLoop.CurrentGrid, _gameLoop.Enemies, 
            out List<Vector2I> moves, out List<Vector2I> attacks);

        if (_selectedCard.Category != CardCategory.Spell)
        {
            foreach (var move in moves) GridView.SpawnHighlight(move, GridManager.HighlightType.Movement);
            foreach (var attack in attacks) GridView.SpawnHighlight(attack, GridManager.HighlightType.Attack);
        }
    }

    private void SpawnEnemiesVisuals()
    {
        foreach (var kvp in _enemyVisuals)
        {
            if (IsInstanceValid(kvp.Value)) kvp.Value.QueueFree();
        }
        _enemyVisuals.Clear();

        float totalTiles = _gameLoop.CurrentGrid.Width * _gameLoop.CurrentGrid.Height;
        float delayForEnemies = (totalTiles * 0.05f) + 0.3f;

        foreach (Entity enemy in _gameLoop.Enemies)
        {
            if (EnemyPrefabs != null && EnemyPrefabs.TryGetValue(enemy.Id, out PackedScene prefab))
            {
                EntityView enemyView = prefab.Instantiate<EntityView>();
                AddChild(enemyView);
                
                // To disable once textures are ready
                enemyView.GetNode<Sprite2D>("Sprite2D").Modulate = new Color(0.8f, 0.3f, 0.3f);
                
                enemyView.Initialize(GridView, enemy.Position, delayForEnemies, enemy.DisplayName);
                _enemyVisuals.Add(enemy, enemyView);
            }
            else
            {
                GD.PrintErr($"Eroare Vizuala: Nu ai asignat un Prefab in dictionar pentru inamicul cu ID-ul: {enemy.Id}");
            }
        }
    }

    public void HandleGridClick(Vector2I tilePos)
    {
        if (_gameLoop == null) return;
        if (_gameLoop.CurrentState != GameState.WaitingForPlayerInput || _isVisualAnimating || _selectedCard == null) return;

        _selectedCard.GetValidTargets(_gameLoop.Player, _gameLoop.CurrentGrid, _gameLoop.Enemies, 
            out List<Vector2I> moves, out List<Vector2I> attacks);
        
        if (moves.Contains(tilePos) || attacks.Contains(tilePos))
        {
            _isVisualAnimating = true; 
            GridView.ClearPlayerHighlights(); 
            foreach (var hl in _hoverHighlights) if (IsInstanceValid(hl)) hl.QueueFree();
            _hoverHighlights.Clear();

            _gameLoop.TryPlayCard(_selectedCard, tilePos);
            PlayerHandUI.UpdateHand(_gameLoop.DeckLogic.Hand);
            DeckViewerUI.UpdateDeckCount(_gameLoop.RunData.ActiveDeck.Count);
            
            _playerVisual.MoveTo(_gameLoop.Player.Position);

            foreach (var enemyKvp in _enemyVisuals)
            {
                if (enemyKvp.Key.IsDead && IsInstanceValid(enemyKvp.Value))
                {
                    _gameLoop.AddSkulls(enemyKvp.Key.SkullsDrop);
                    Tween deathTween = enemyKvp.Value.CreateTween();
                    deathTween.TweenProperty(enemyKvp.Value, "scale", Vector2.Zero, 0.2f);
                    deathTween.TweenCallback(Callable.From(enemyKvp.Value.QueueFree));
                }
            }
            
            GetTree().CreateTimer(0.35f).Timeout += () => 
            {
                _isVisualAnimating = false; 
                
                if (_gameLoop.CurrentState == GameState.WaitingForPlayerInput)
                {
                    StartPlayerTurnVisuals();
                }
                else if (_gameLoop.CurrentState == GameState.EnemyTurn)
                {
                    _gameLoop.ProcessEnemyTurn();
                }
            };
        }
    }

    private void HandleChestDecision(bool takeCard)
    {
        _gameLoop.ResolveChestLoot(takeCard);

        GridView.SetChestLooted(_gameLoop.Player.Position);
        
        if (_gameLoop.CurrentState == GameState.WaitingForPlayerInput)
        {
            StartPlayerTurnVisuals();
        }
        else if (_gameLoop.CurrentState == GameState.EnemyTurn)
        {
            _gameLoop.ProcessEnemyTurn();
        }
    }

    private void PopulateDeckOverlay()
    {
        if (_gameLoop == null || _gameLoop.RunData == null || _gameLoop.DeckLogic == null) return; // Failsafe in case deck tries to load without gameloop
        DeckViewerUI.PopulateOverlay(_gameLoop.RunData, _gameLoop.DeckLogic);
    }

    private void HandleEnemyIntents(Dictionary<Entity, EnemyIntent> intents)
    {
        GridView.ClearEnemyHighlights();
        
        foreach (var kvp in intents)
        {
            if (kvp.Value.Type == IntentType.Move || kvp.Value.Type == IntentType.AttackPlayer)
            {
                // Enemy intent highlights are now spawned in the grid view based on the calculated target positions
                GridView.SpawnHighlight(kvp.Value.TargetPosition, GridManager.HighlightType.EnemyIntent);
            }
        }
    }
    private void HandleSlotSelected(PlayerProfile profile, int slotIndex)
    {
        _selectedProfile = profile;
        _selectedSlot = slotIndex;
        PreRunMenuView.ShowMenu(profile, slotIndex);
    }
    private void HandleNewRun()
    {
        PreRunMenuView.HideMenu();
        if (GameplayUILayer != null) GameplayUILayer.Visible = true;
        PauseMenuView.IsGameActive = true; // Activate escape
        
        // Starting a new run deletes the previous one
        SaveManager.DeleteActiveRun(_selectedSlot);

        StartGameCore(_selectedProfile, _selectedSlot, isContinue: false);
    }

    private void HandleContinueRun()
    {
        PreRunMenuView.HideMenu();
        if (GameplayUILayer != null) GameplayUILayer.Visible = true;
        PauseMenuView.IsGameActive = true;

        StartGameCore(_selectedProfile, _selectedSlot, isContinue: true);
    }

    private void HandleSaveAndQuit(bool toDesktop)
    {
        PauseMenuView.ForceClose();
        // We save the run
        SaveManager.SaveActiveRun(_gameLoop.RunData, _selectedSlot);
        SaveManager.SaveProfile(_selectedProfile, _selectedSlot);

        if (toDesktop)
        {
            GetTree().Quit();
        }
        else
        {
            // Reset scene
            GetTree().ReloadCurrentScene();
        }
    }

    private void HandleAbandonRun()
    {
        PauseMenuView.ForceClose();
        PauseMenuView.IsGameActive = false; // Block escape before we go back to main menu
        
        if (_gameLoop != null && _gameLoop.Player != null)
        {
            _gameLoop.Player.ForceKill();
            _gameLoop.ChangeState(GameState.CheckWinCondition); 
        }
        else
        {
            // Failsafe: if we somehow trigger abandon run but there's no active game, just return to main menu directly (you won't imagine how funny this bug was)
            GetTree().ReloadCurrentScene();
        }
    }
    public void HandleGridHover(Vector2I tilePos)
    {
        if (_gameLoop == null) return;
        // Clear old hovers at every mouse movement
        foreach (var hl in _hoverHighlights) if (IsInstanceValid(hl)) hl.QueueFree();
        _hoverHighlights.Clear();

        if (_gameLoop.CurrentState != GameState.WaitingForPlayerInput || _isVisualAnimating || _selectedCard == null) return;

        _selectedCard.GetValidTargets(_gameLoop.Player, _gameLoop.CurrentGrid, _gameLoop.Enemies, 
            out List<Vector2I> moves, out List<Vector2I> attacks);

        // If the mouse tile is part of the valid tiles
        if (moves.Contains(tilePos) || attacks.Contains(tilePos))
        {
            // We call the AoE positions that we will hit if used there
            List<Vector2I> aoeTiles = _selectedCard.GetAoETiles(tilePos, _gameLoop.CurrentGrid);
            
            foreach (var aoeTile in aoeTiles)
            {
                // We use an orange-d attack highlight for targetting
                Node2D hl = GridView.AttackHighlightPrefab.Instantiate<Node2D>();
                GridView.AddChild(hl);
                hl.Position = GridView.LogicalToIsometric(aoeTile);
                hl.Modulate = new Color(1f, 0.5f, 0f, 0.6f); 
                
                _hoverHighlights.Add(hl);
            }
        }
    }
}