---
name: refactor-template
description: Structured template for refactoring requests. Ensures clear goals and no API breakage.
---

# Refactor Request Template

Use this structure for refactoring. Copy and fill in:

```markdown
## Refactor Target: [ClassName.cs or System Name]

### Reason
[Why this refactor is needed - performance, maintainability, etc.]

### Goal
[What the code should look like after refactoring]

### Constraints
- [ ] No public API changes (existing callers must still work)
- [ ] Must pass all existing tests
- [ ] Performance must not degrade
- [ ] [Any other constraints]

### Files Affected
- Primary: [Main file being refactored]
- Secondary: [Files that call into this code]
```

## Example: Inventory Optimization

```markdown
## Refactor Target: InventoryManager.cs

### Reason
Current implementation uses `List<GameObject>` causing GC spikes during sorting.
Hard to query items efficiently.

### Goal
- Convert storage from GameObjects to `List<ItemData>` where ItemData is a struct
- Create `Dictionary<string, ItemData>` for O(1) lookups by ID
- All public methods (`AddItem`, `RemoveItem`, `GetItem`) still work identically

### Constraints
- [x] No public API changes
- [x] Must pass all existing tests
- [x] Performance must improve (no GC during normal operations)

### Files Affected
- Primary: InventoryManager.cs
- Secondary: UIInventoryController.cs, SaveManager.cs, ShopController.cs
```

## Workflow After Template

1. **Claude reads** all affected files using Serena
2. **Claude writes** failing test for desired behavior
3. **Claude implements** refactor in small, testable steps
4. **Gemini reviews** for missed edge cases
5. **Claude runs** all tests, commits if green
