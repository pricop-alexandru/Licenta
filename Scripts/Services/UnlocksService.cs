using System.Collections.Generic;
using Deckrinth.Model;
using Deckrinth.Model.Resources;

namespace Deckrinth.Services;

public class UnlocksService
{
    private PlayerProfile _currentProfile;
    
    // HashSets provide O(1) lookup time compared to List O(N)
    private HashSet<string> _unlockedIdsCache;

    public UnlocksService(PlayerProfile profile)
    {
        _currentProfile = profile;
        _unlockedIdsCache = new HashSet<string>();
        RefreshCache();
    }

    // Called when the profile changes or a new item is unlocked mid-session
    public void RefreshCache()
    {
        _unlockedIdsCache.Clear();
        
        if (_currentProfile == null) return;

        foreach (string id in _currentProfile.UnlockedCardIds)
        {
            _unlockedIdsCache.Add(id);
        }
        
        foreach (string id in _currentProfile.UnlockedEnemyIds)
        {
            _unlockedIdsCache.Add(id);
        }
    }

    // Returns a masked or unmasked DTO ready to be drawn by the UI
    public UnlockableDisplayData GetDisplayData(UnlockableItemResource resource)
    {
        bool isUnlocked = _unlockedIdsCache.Contains(resource.Id);
        return new UnlockableDisplayData(resource, isUnlocked);
    }

    // Filters a full list of resources into separate categories for the UI
    public void CategorizeResources(List<UnlockableItemResource> allResources, 
        out List<UnlockableDisplayData> cards, 
        out List<UnlockableDisplayData> enemies)
    {
        cards = new List<UnlockableDisplayData>();
        enemies = new List<UnlockableDisplayData>();

        foreach (var res in allResources)
        {
            var displayData = GetDisplayData(res);
            
            if (res.ItemType == UnlockableType.Card)
                cards.Add(displayData);
            else if (res.ItemType == UnlockableType.Enemy)
                enemies.Add(displayData);
        }
    }
}