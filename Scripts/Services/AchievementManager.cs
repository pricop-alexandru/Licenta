using System;
using System.Collections.Generic;
using Deckrinth.Model;
using Deckrinth.Model.Resources;

namespace Deckrinth.Services;

public class AchievementManager
{
    private PlayerProfile _profile;
    private RunState _runState;
    private List<UnlockableItemResource> _allUnlockables;
    private int _currentSaveSlot;
    public List<UnlockableDisplayData> UnlocksThisRun { get; private set; } = new List<UnlockableDisplayData>();
    
    // Event that is triggered when an item is unlocked, allowing the UI to respond accordingly
    public event Action<UnlockableDisplayData> OnItemUnlocked;

    public AchievementManager(PlayerProfile profile, RunState runState, int saveSlot, List<UnlockableItemResource> allUnlockables)
    {
        _profile = profile;
        _runState = runState;
        _currentSaveSlot = saveSlot;
        _allUnlockables = allUnlockables;
        UnlocksThisRun.Clear();
    }

    // Universal method to record a stat, which can be used for both cumulative and record-breaking stats
    public void RecordStat(string statKey, int valueToAdd = 1, bool replaceIfHigher = false)
    {
        if (!_profile.LifetimeStats.ContainsKey(statKey))
        {
            _profile.LifetimeStats[statKey] = 0;
        }

        if (replaceIfHigher)
            _profile.LifetimeStats[statKey] = Math.Max(_profile.LifetimeStats[statKey], valueToAdd);
        else
            _profile.LifetimeStats[statKey] += valueToAdd;

        // We only record the stat for the current run if we have a valid run state and we're not replacing with a higher value
        if (!replaceIfHigher && _runState != null)
        {
            if (!_runState.CurrentRunStats.ContainsKey(statKey))
            {
                _runState.CurrentRunStats[statKey] = 0;
            }
            _runState.CurrentRunStats[statKey] += valueToAdd;
        }

        EvaluateUnlocks();
    }

    // We check if any unlockable items can be unlocked based on the current stats
    private void EvaluateUnlocks()
    {
        foreach (var resource in _allUnlockables)
        {
            // We ignore
            if (IsAlreadyUnlocked(resource)) continue;

            // If the resource doesn't have a required stat key, we skip it
            if (string.IsNullOrEmpty(resource.RequiredStatKey)) continue;

            // If the player has the respective stat
            if (_profile.LifetimeStats.TryGetValue(resource.RequiredStatKey, out int currentValue))
            {
                if (currentValue >= resource.RequiredStatValue)
                {
                    UnlockItem(resource);
                }
            }
        }
    }

    private bool IsAlreadyUnlocked(UnlockableItemResource resource)
    {
        if (resource.ItemType == UnlockableType.Card)
            return _profile.UnlockedCardIds.Contains(resource.Id);
        else
            return _profile.UnlockedEnemyIds.Contains(resource.Id);
    }

    private void UnlockItem(UnlockableItemResource resource)
    {
        if (resource.ItemType == UnlockableType.Card)
            _profile.UnlockedCardIds.Add(resource.Id);
        else
            _profile.UnlockedEnemyIds.Add(resource.Id);
        
        _profile.UnseenUnlockIds.Add(resource.Id);
        // We save the profile after unlocking an item to ensure persistence
        SaveManager.SaveProfile(_profile, _currentSaveSlot);

        // We notify the UI about the unlock so it can display a popup or some other feedback
        var displayData = new UnlockableDisplayData(resource, true);
        if (resource.ItemType == UnlockableType.Card)
        {
            UnlocksThisRun.Add(displayData);
        }
        
        OnItemUnlocked?.Invoke(displayData);
    }
}