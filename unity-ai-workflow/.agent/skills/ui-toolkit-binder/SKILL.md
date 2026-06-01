---
name: UI Toolkit Binder
description: Generate Unity 2022 LTS UI Toolkit manual binding code using the Controller pattern with MonoBehaviour + UIDocument. Use when creating UI screens, HUDs, menus, or any UI elements with UI Toolkit. Triggers on requests like "create a health bar", "build the HUD", "bind data to UI", or any UI Toolkit work. Only activates when ProjectConfig.yaml → ui_system is "UIToolkit" or "Mixed". Compatible with Unity 2022 LTS (no Runtime Data Binding).
---

# UI Toolkit Binder

Generate UI code using manual binding for Unity 2022 LTS.

> ⚠️ **Unity 2022 LTS does NOT support Runtime Data Binding** (`[CreateProperty]`, `DataBinding`, `PropertyPath`). Those APIs were introduced in Unity 6. This skill uses **manual `Q<T>()` queries** and **`RegisterCallback<T>()`** instead.

## When to Use UI Toolkit vs uGUI
- **UI Toolkit** — Recommended for menus, settings screens, inventories, data-heavy UI. Better performance (batched rendering, no GameObject overhead), future-proof.
- **uGUI** — Valid for animated in-game HUDs, diegetic UI in world space, or when shipping fast with existing uGUI knowledge. Still maintained by Unity (not deprecated).
- **Hybrid** — Use both in the same project. Common: UI Toolkit for menus, uGUI for in-game HUD.

If `ProjectConfig.yaml → ui_system` is `UGUI`, this skill does not apply.

## Before You Start
1. Verify `docs/ProjectConfig.yaml → ui_system` includes UIToolkit or Mixed.
2. Check `docs/GFD_Template.md` for UI motion guidelines if applicable.
3. Check `docs/DESIGN.md` for "The Tangible Playground" design tokens.

## Architecture

```
Model (Data) → Controller (MonoBehaviour + UIDocument) → View (UXML + USS)
```

No ViewModel layer — the Controller queries elements directly and updates them in response to data changes.

## Key Pattern: Controller with Manual Binding

### 1. Query elements via `root.Q<T>("name")`

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHUDController : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private Label _healthLabel;
    private VisualElement _healthBar;
    private Button _pauseButton;

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;

        // Always null-check Q<T>() results
        _healthLabel = root.Q<Label>("health-label");
        _healthBar = root.Q<VisualElement>("health-bar-fill");
        _pauseButton = root.Q<Button>("pause-btn");

        if (_healthLabel == null)
        {
            Debug.LogError("[PlayerHUDController] health-label not found in UXML!");
            return;
        }

        // Register callbacks manually
        if (_pauseButton != null)
        {
            _pauseButton.RegisterCallback<ClickEvent>(OnPauseClicked);
        }
    }

    private void OnDisable()
    {
        // Always unregister to prevent leaks
        _pauseButton?.UnregisterCallback<ClickEvent>(OnPauseClicked);
    }

    private void OnPauseClicked(ClickEvent evt)
    {
        // Handle pause
    }
}
```

### 2. Update UI from data (manual push)

```csharp
// Call this whenever health changes — no automatic binding
public void UpdateHealth(int current, int max)
{
    if (_healthLabel != null)
    {
        _healthLabel.text = $"{current} / {max}";
    }

    if (_healthBar != null)
    {
        float pct = (float)current / max;
        _healthBar.style.width = Length.Percent(pct * 100f);
    }
}
```

### 3. List/ScrollView binding (manual)

```csharp
public void PopulateList(List<string> items)
{
    var listContainer = _uiDocument.rootVisualElement.Q<ScrollView>("item-list");
    if (listContainer == null) return;

    listContainer.Clear();

    foreach (var item in items)
    {
        var label = new Label(item);
        label.AddToClassList("list-item");
        listContainer.Add(label);
    }
}
```

### 4. Async operations with UniTask

```csharp
using Cysharp.Threading.Tasks;

public async UniTaskVoid LoadAndDisplayAsync()
{
    var ct = this.GetCancellationTokenOnDestroy();

    _healthLabel.text = "Loading...";
    var data = await SomeAsyncOperation().AttachExternalCancellation(ct);
    _healthLabel.text = data.ToString();
}
```

## Critical Notes
- **No `[CreateProperty]`** — that requires `Unity.Properties` which is Unity 6+ only.
- **No `DataBinding`** — use manual `Q<T>()` + update methods.
- **Always null-check** `Q<T>()` results — typos in element names cause silent `null` returns.
- **Always unregister callbacks** in `OnDisable()` to prevent memory leaks.
- **Use USS classes** for styling — avoid inline `style` properties in C# when possible.
- Always generate matching `.uxml` and `.uss` files alongside C# controller scripts.
- Reference `docs/DESIGN.md` for "The Tangible Playground" design tokens (colors, spacing, typography).
