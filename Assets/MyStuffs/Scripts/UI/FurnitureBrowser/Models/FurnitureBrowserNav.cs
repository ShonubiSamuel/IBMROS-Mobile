using System;
using System.Collections.Generic;

// ============================================
// FURNITURE BROWSER NAV
// Owns the navigation stack for the browser
// panel. Knows nothing about UI — it only
// tracks state and fires events that the
// FurnitureBrowserController listens to.
//
// One push per level tap, one pop per back
// button tap. The stack never grows beyond
// the total number of BrowserLevel values.
// ============================================
public class FurnitureBrowserNav
{
    // ============================================
    // EVENTS
    // FurnitureBrowserController subscribes to
    // these and swaps the visible view in response.
    // ============================================

    // Fired when the user drills into a new level
    // BrowserNavEntry carries the level, selected
    // id, and header title for the new view
    public event Action<BrowserNavEntry> OnNavigatedForward;

    // Fired when the user taps the back button
    // BrowserNavEntry carries the level and id
    // that should now be visible again
    public event Action<BrowserNavEntry> OnNavigatedBack;

    // Fired when the stack is fully reset
    // e.g. panel closed and reopened
    public event Action OnStackCleared;

    // ============================================
    // STATE
    // ============================================
    private readonly Stack<BrowserNavEntry> _stack
        = new Stack<BrowserNavEntry>();

    // Maximum depth equals the number of levels
    // in the BrowserLevel enum so the stack can
    // never grow past a full navigation path
    private static readonly int MaxDepth
        = Enum.GetValues(typeof(BrowserLevel)).Length;

    // ============================================
    // READ ONLY PROPERTIES
    // ============================================

    // True when the back button should be visible
    // Root level has nothing to go back to
    public bool CanGoBack => _stack.Count > 1;

    // How many levels deep the user currently is
    public int Depth => _stack.Count;

    // The entry that is currently on top of the
    // stack, i.e. what the user is looking at now
    // Returns null if the stack is empty
    public BrowserNavEntry Current =>
        _stack.Count > 0 ? _stack.Peek() : null;

    // The level the user is currently on
    // Returns RoomType when the stack is empty
    public BrowserLevel CurrentLevel =>
        Current?.Level ?? BrowserLevel.RoomType;

    // The header title for the current view
    // Returns empty string when stack is empty
    public string CurrentTitle =>
        Current?.HeaderTitle ?? string.Empty;

    // The id selected at the current level
    // Returns null when at the root
    public string CurrentSelectedId =>
        Current?.SelectedId;

    // ============================================
    // NAVIGATION METHODS
    // ============================================

    // Called when the user taps a room type card,
    // a category card, a subcategory card, or
    // any other item that drills to a deeper level
    //
    // level       — the level being entered
    // selectedId  — the id of the item tapped
    // headerTitle — the title shown in the panel
    //               header for this new view
    public void Push(BrowserLevel level, string selectedId, string headerTitle)
    {
        if (string.IsNullOrEmpty(selectedId))
            return;

        if (_stack.Count >= MaxDepth)
            return;

        var entry = new BrowserNavEntry(level, selectedId, headerTitle);
        _stack.Push(entry);

        OnNavigatedForward?.Invoke(entry);
    }

    // Called when the user taps the back button
    // Removes the current level and restores the
    // previous one. Does nothing at the root.
    public void Pop()
    {
        if (!CanGoBack)
            return;

        // Remove the current level
        _stack.Pop();

        // The entry now on top is where we return to
        OnNavigatedBack?.Invoke(_stack.Peek());
    }

    // Resets the stack back to the initial root
    // state without firing forward or back events.
    // Called when the browser panel is closed so
    // the next open always starts at room type level.
    public void Clear()
    {
        _stack.Clear();
        OnStackCleared?.Invoke();
    }

    // ============================================
    // STACK INSPECTION
    // Used by FurnitureBrowserController to restore
    // the correct view after an external event
    // e.g. the panel being hidden and reshown
    // ============================================

    // Returns a snapshot of the full stack ordered
    // from bottom (root) to top (current)
    // so the controller can rebuild the view chain
    public List<BrowserNavEntry> GetStackSnapshot()
    {
        var snapshot = new List<BrowserNavEntry>(_stack);

        // Stack enumerates top-to-bottom so reverse
        // to get chronological order root to current
        snapshot.Reverse();
        return snapshot;
    }

    // Returns the entry at a specific depth level
    // 0 is the root, Depth-1 is the current entry
    // Returns null if index is out of range
    public BrowserNavEntry GetEntryAtDepth(int depth)
    {
        var snapshot = GetStackSnapshot();

        if (depth < 0 || depth >= snapshot.Count)
            return null;

        return snapshot[depth];
    }

    // Returns the selected id at a given BrowserLevel
    // Returns null if that level is not in the stack
    public string GetSelectedIdAtLevel(BrowserLevel level)
    {
        foreach (var entry in _stack)
        {
            if (entry.Level == level)
                return entry.SelectedId;
        }

        return null;
    }

    // Returns true if a specific level is currently
    // anywhere in the navigation stack
    public bool IsLevelInStack(BrowserLevel level)
    {
        foreach (var entry in _stack)
        {
            if (entry.Level == level)
                return true;
        }

        return false;
    }

    // ============================================
    // CONVENIENCE PUSH HELPERS
    // One typed method per level so call sites
    // read clearly and cannot pass the wrong level
    // ============================================

    // Push from the room type grid into categories
    public void PushCategory(string roomTypeId, string roomTypeName)
    {
        Push(BrowserLevel.Category, roomTypeId, roomTypeName);
    }

    // Push from the category grid into subcategories
    public void PushSubcategory(string categoryId, string categoryName)
    {
        Push(BrowserLevel.Subcategory, categoryId, categoryName);
    }

    // Push from the subcategory grid into the product list
    public void PushProductList(string subcategoryId, string subcategoryName)
    {
        Push(BrowserLevel.ProductList, subcategoryId, subcategoryName);
    }
}