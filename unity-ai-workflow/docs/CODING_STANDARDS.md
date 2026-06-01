# Coding Standards

> Refined collaboratively during Phase 1 (Pre-Production). These are starting defaults — customize to your team's preferences.

## Formatting

| Rule | Standard |
|------|----------|
| **Braces** | Allman style (opening brace on new line) |
| **Indentation** | 4 spaces (no tabs) |
| **Line Length** | Soft limit 120 characters |
| **Blank Lines** | One blank line between methods, two between sections |

## Class Structure Order

```csharp
public class ExampleBehaviour : MonoBehaviour
{
    // 1. Constants
    private const float DEFAULT_SPEED = 5f;

    // 2. Static fields
    private static int s_instanceCount;

    // 3. Serialized fields (Inspector)
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = DEFAULT_SPEED;
    [SerializeField] private float _jumpForce = 10f;

    // 4. Private fields
    private Rigidbody _rb;
    private bool _isGrounded;

    // 5. Properties
    public float MoveSpeed => _moveSpeed;

    // 6. Unity Lifecycle (in execution order)
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable() { }
    private void Start() { }
    private void Update() { }
    private void FixedUpdate() { }
    private void LateUpdate() { }
    private void OnDisable() { }
    private void OnDestroy() { }

    // 7. Public methods
    public void Jump() { }

    // 8. Private methods
    private void ApplyMovement() { }

    // 9. Event handlers
    private void OnDamageReceived(int amount) { }
}
```

## Serialization Rules

```csharp
// ✅ CORRECT
[SerializeField] private float _moveSpeed = 5f;
[SerializeField] private GameObject _projectilePrefab;

// ❌ WRONG — never public for Inspector
public float moveSpeed = 5f;

// ❌ WRONG — never [HideInInspector] on public
[HideInInspector] public float speed;
```

## Null Safety

```csharp
// ✅ CORRECT — TryGetComponent
if (TryGetComponent<Rigidbody>(out var rb))
{
    rb.AddForce(Vector3.up);
}

// ✅ CORRECT — null check before use
if (_targetTransform != null)
{
    transform.LookAt(_targetTransform);
}

// ❌ WRONG — crash-prone
GetComponent<Rigidbody>().AddForce(Vector3.up);
```

## String Usage

```csharp
// ✅ CORRECT — interpolation
Debug.Log($"Player {playerName} took {damage} damage");

// ❌ WRONG — concatenation (allocates)
Debug.Log("Player " + playerName + " took " + damage + " damage");
```

## Access Modifiers

| Modifier | When to Use |
|----------|-------------|
| `private` | Default for all fields and internal methods |
| `[SerializeField] private` | Inspector-exposed fields (never use `public` for this) |
| `public` | API methods intended for external callers |
| `internal` | APIs visible within an assembly but hidden outside it. Use for cross-class helpers within a feature asmdef that shouldn't be part of the public API. Follows same naming conventions as `public`. |
| `protected` | Only when designing for inheritance |

## Async Patterns

```csharp
// ✅ CORRECT — UniTask with CancellationToken
private async UniTask LoadLevelAsync(CancellationToken ct)
{
    try
    {
        await SceneManager.LoadSceneAsync("Level1").WithCancellation(ct);
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Level load cancelled");
    }
}

// ✅ CORRECT — MonoBehaviour-scoped cancellation token (UniTask)
var ct = this.GetCancellationTokenOnDestroy();
await LoadLevelAsync(ct);

// ✅ CORRECT — Create scoped token linked to MonoBehaviour lifetime
var cts = CancellationTokenSource.CreateLinkedTokenSource(
    this.GetCancellationTokenOnDestroy());

// ✅ CORRECT — Per-scope tokens (cancel when scope exits)
private CancellationTokenSource _levelCts;
private void OnLevelStart()
{
    _levelCts = CancellationTokenSource.CreateLinkedTokenSource(
        this.GetCancellationTokenOnDestroy());
}
private void OnLevelEnd()
{
    _levelCts?.Cancel();
    _levelCts?.Dispose();
}

// ✅ CORRECT — UniTask delay and wait utilities
await UniTask.Delay(1000, cancellationToken: ct);              // Wait 1 second
await UniTask.WaitUntil(() => _isReady, cancellationToken: ct); // Wait for condition
await UniTask.Yield();                                          // Yield one frame
await UniTask.WaitForFixedUpdate();                             // Wait for FixedUpdate

// ❌ WRONG — no cancellation, no error handling
private async UniTask LoadLevelAsync()
{
    await SceneManager.LoadSceneAsync("Level1");
}

// ⚠️ ACCEPTABLE — Coroutines for simple sequences (animations/tweens)
private IEnumerator PlayHitSequence()
{
    _spriteRenderer.color = Color.red;
    yield return new WaitForSeconds(0.1f);
    _spriteRenderer.color = Color.white;
}
```

**Async rules:**
- Always pass `CancellationToken` to async methods
- Use `this.GetCancellationTokenOnDestroy()` for MonoBehaviour-scoped tokens (UniTask)
- Always `try/catch` for `OperationCanceledException` around awaits
- Dispose `CancellationTokenSource` when scope ends
- Create per-level/per-scene tokens for granular cancellation
- Coroutines are acceptable for simple animation/tween sequences
