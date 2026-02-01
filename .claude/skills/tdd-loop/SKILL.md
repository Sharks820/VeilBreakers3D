---
name: tdd-loop
description: Automated Test-Driven Development loop. Claude writes failing test, implements code, runs tests, debugs with Gemini if needed.
---

# TDD Loop Skill

Automated test-driven development with Claude+Gemini collaboration.

## The Loop

```
┌─────────────────────────────────────────────────────────────┐
│  1. WRITE FAILING TEST                                       │
│     Claude writes a test that defines expected behavior      │
│     Test MUST fail initially (proves test is valid)          │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  2. RUN TEST (Confirm Failure)                               │
│     Use mcp-unity to run the test                            │
│     If test passes → test was wrong, rewrite                 │
│     If test fails → proceed to implementation                │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  3. WRITE IMPLEMENTATION                                     │
│     Claude writes minimal code to make test pass             │
│     Don't over-engineer, just pass the test                  │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  4. RUN TEST (Check Pass)                                    │
│     If PASS → Feature complete, move to next test            │
│     If FAIL → Go to step 5                                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  5. DEBUG WITH GEMINI                                        │
│     Send error + code to Gemini for analysis                 │
│     Gemini suggests fix                                      │
│     Claude applies fix → Return to step 4                    │
└─────────────────────────────────────────────────────────────┘
```

## Unity Test Template

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VeilBreakers.[Namespace];

public class [Feature]Tests
{
    [Test]
    public void [Feature]_[Scenario]_[ExpectedResult]()
    {
        // Arrange
        var sut = new [SystemUnderTest]();

        // Act
        var result = sut.[Method]([inputs]);

        // Assert
        Assert.AreEqual([expected], result);
    }
}
```

## Example: Poison Effect TDD

### Step 1: Write Failing Test
```csharp
[Test]
public void PoisonEffect_ApplyToTarget_DealsDamageOverTime()
{
    // Arrange
    var target = CreateTestCombatant(maxHealth: 100);
    var poison = new PoisonEffect(damagePerSecond: 5, duration: 3);

    // Act
    poison.Apply(target);
    SimulateTime(3f); // Advance 3 seconds

    // Assert
    Assert.AreEqual(85, target.CurrentHealth); // 100 - (5 * 3)
}
```

### Step 2: Run Test → Fails (PoisonEffect doesn't exist)

### Step 3: Write Implementation
```csharp
public class PoisonEffect : StatusEffect
{
    private float _damagePerSecond;
    private float _elapsed;

    public PoisonEffect(float damagePerSecond, float duration)
    {
        _damagePerSecond = damagePerSecond;
        Duration = duration;
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed >= 1f)
        {
            Target.TakeDamage(_damagePerSecond);
            _elapsed -= 1f;
        }
    }
}
```

### Step 4: Run Test → Pass

### Step 5: (Only if fail) Send to Gemini:
```bash
gemini -p "Test failed with error: [error]. Code: [code]. What's wrong?"
```

## When to Use This Skill

- Implementing new game mechanics
- Adding features with clear expected behavior
- Bug fixes (write test that reproduces bug first)
- Refactoring (tests ensure nothing breaks)
