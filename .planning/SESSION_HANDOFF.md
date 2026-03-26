# Session Handoff — 2026-03-22 Comprehensive Quality Pass (v5.1)

**Created:** 2026-03-22
**Last commit:** `e883184`
**Branches:** master ahead — develop/feature branches need sync

## Immediate Actions (Next Session)
```bash
git push origin master
git branch -f develop master
git branch -f feature/character-select-rebuild master
git push origin master develop
```

## Session Summary
Opus session: 42 bugs found, 28+ fixed, 7 commits. Version v5.1.

### Commits (newest first)
```
e883184 perf: Debug.Log → ErrorLogger in AudioManager + SaveFileHandler
a08ef20 docs: VEILBREAKERS.md v5.1
4dd1cba fix: EventBus leak, VERASystem alloc, code quality
f072686 perf: Debug.Log → ErrorLogger in SaveManager
cadc539 perf: hot-path allocations, Camera.main cache
25d5d94 fix: orange bar glitch on New Game transition
d77ec1b fix: Tier 0-1 security, gameplay, CharSelect (15 files)
```

## Key Fixes Applied
- **Security:** Save key file backup, non-blocking pause save, clean capture removal
- **Gameplay:** Corruption double-dip removed, damage buff stacking, BattleResumed event
- **CharSelect:** 12 fixes (isEmbarking finally, NavigationMove dedup, VolumeProfile leak, zone gating)
- **Orange bar:** VFX overlay fades out during transition
- **Performance:** 33 Debug.Log → ErrorLogger, HashSet party lookup, alloc fixes
- **Review:** EventBus 4 missing events, UIAnimationController dict clear, VERASystem static chars

## Remaining Work
1. **CharSelect UI functionality** — buttons not clickable, needs Unity visual testing
2. **VB-Toolkit upgrades** — blocked by permissions (now fixed via allowedTools)
3. **Remaining bugs below** — prioritized by severity

## Master Bug Tracker — All Scans Combined (~200+ unique issues)

### Source Reports
| Report | Total | Location |
|--------|-------|----------|
| BUG_SCAN_REPORT.md (2026-03-20) | 37 | `Docs/BUG_SCAN_REPORT.md` |
| CHARSELECT_DEEP_SCAN.md (2026-03-21) | 28 | `Docs/CHARSELECT_DEEP_SCAN.md` |
| FULL_CODEBASE_DEEP_SCAN.md (2026-03-21) | 154 | `Docs/FULL_CODEBASE_DEEP_SCAN.md` |
| FULL_CODEBASE_FIX_PROMPT.md (2026-03-21) | 39 fixes | `Docs/FULL_CODEBASE_FIX_PROMPT.md` |
| This session's agents (2026-03-22) | 42 | Documented below |

### Fix Prompt Tracker (39 fixes from FULL_CODEBASE_FIX_PROMPT.md)

#### TIER 0 — Data Loss / Security
| Fix | Description | Status |
|-----|-------------|--------|
| FIX 1 | Save encryption key file backup | DONE (d77ec1b) |
| FIX 2 | SaveManager deadlock on pause | DONE (d77ec1b) |
| FIX 3 | Captured monster triggers death events | DONE (d77ec1b) |

#### TIER 1 — Gameplay-Breaking
| Fix | Description | Status |
|-----|-------------|--------|
| FIX 4 | Corruption modifier inverted for defenders | DONE (d77ec1b) |
| FIX 5 | `_isEmbarking` not reset on timeout | DONE (d77ec1b) |
| FIX 6 | Dual NavigationMoveEvent handlers | DONE (d77ec1b) |
| FIX 7 | Berserk fires BattleStarted event | DONE (d77ec1b) |
| FIX 8 | Damage buff overwrites instead of stacking | DONE (d77ec1b) |

#### TIER 2 — Memory Leaks
| Fix | Description | Status |
|-----|-------------|--------|
| FIX 9 | EventBus 4 missing event clears | DONE (4dd1cba) |
| FIX 10 | VolumeProfile SO leak | DONE (d77ec1b) |
| FIX 11 | AIPersonality static SO cache leak | TODO |
| FIX 12 | SoulSwarmVFX double mouse callback | TODO |
| FIX 13-17 | 5x GeometryChangedEvent leaks | TODO |

#### TIER 3 — Robustness
| Fix | Description | Status |
|-----|-------------|--------|
| FIX 18 | SaveData validation gaps | TODO |
| FIX 19 | HeroData stat overflow (GetStatAtLevel clamp) | TODO |
| FIX 20 | HeroData.Validate clamp all 6 stats | DONE (d77ec1b) |
| FIX 21 | Unsafe enum casts in MonsterData | TODO |
| FIX 22 | Mutable Party list | TODO |
| FIX 23 | AddCurrency negative amount bypass | TODO |
| FIX 24 | PauseGame state machine inconsistency | TODO |
| FIX 25 | AI null target issues | TODO |
| FIX 26 | GambitController null personality check | TODO |
| FIX 27 | Gamepad hold bypasses embark focus check | DONE (d77ec1b) |
| FIX 28 | HoldToEmbark duplicate callback registration | DONE (d77ec1b) |
| FIX 29 | Combatant events not cleared | TODO |

#### TIER 4 — CharSelect Remaining
| Fix | Description | Status |
|-----|-------------|--------|
| FIX 30 | OnCinematicComplete not cleared | DONE (d77ec1b) |
| FIX 31 | Rim flicker tweens in CleanupStage | TODO |
| FIX 32 | Breathing animation stacking | TODO |
| FIX 33 | Entry sequence not tracked + wrong leftPanel | TODO |
| FIX 34 | OnTransitionComplete multi-fire | DONE (d77ec1b) |
| FIX 35 | Stat cascade not stopped on disable | DONE (d77ec1b) |
| FIX 36 | RenderSettings.ambientLight not restored | TODO |

#### TIER 5 — Repo Cleanup
| Fix | Description | Status |
|-----|-------------|--------|
| FIX 37 | Consolidate duplicate files | TODO |
| FIX 38 | Move archive data out of Resources | TODO |
| FIX 39 | Archive old scan reports | TODO |

**Score: 16/39 fixes done (41%). All Tier 0 + Tier 1 complete. Tier 2-5 partially done.**

---

## Unfixed Bugs Found By Code Review Agents (This Session)

### HIGH Priority (fix next)
| File | Issue |
|------|-------|
| `GameDataTypes.cs` | Duplicate `SkillData`/`ItemData` nested classes shadow `VeilBreakers.Data` types — confusing, could cause deserialization bugs |
| `BrandSystem.cs:5` | Cross-layer dependency: Systems imports `VeilBreakers.UI.Core` for ThemeManager — breaks if assemblies are separated |
| `DamageCalculator.cs:66-69` | Synergy defense bonus uses division (`/= 1.08`) instead of multiplication — mathematically inconsistent |
| `CaptureFormulaCalculator.cs:166-169` | Level penalty variable naming misleading (`penalty` is negative) — refactor risk |
| `CaptureBannerController.cs:177` | `GetNearestAlly()` always returns player, not actual ally — placeholder logic |
| `CaptureBannerController.cs:365` | `BattleManager.Instance?.CurrentTarget` not guarded with `HasInstance` |

### MEDIUM Priority
| File | Issue |
|------|-------|
| `TitleScreenVFX.cs` | 1,500+ VisualElements (no pooling), 11 Resources.Load calls, 96 new elements per click burst |
| `MoltenButtonVFX.cs:148-182` | 5 Resources.Load calls in Initialize() — should use SO refs |
| `CharSelectEnvironmentController.cs:222` | `string.GetHashCode()` non-deterministic across .NET versions for nebula offsets |
| `CarouselController.cs:167-173` | PrimeTween `Tween.Custom` uses closure-based overload (allocates) instead of target-based |
| `Combatant.cs:389-397` | `ApplyStatus` creates StatusEffectInstance directly, bypassing StatusEffectManager tracking |
| `UIAssets.cs:27` | Fallback `Resources.Load` in singleton getter with no "tried and failed" guard |
| `VERASystem.cs:95-104` | Manual singleton pattern instead of `SingletonMonoBehaviour<T>` |
| `GameBootstrap.cs:214-230` | LowHealthAudio/FPSCounter don't use standard singleton pattern |
| `EventBus.cs:92-94` | Obsolete `OnStatusApplied`/`OnStatusRemoved` events still exist (deprecated) |
| `GameDatabase.cs:166-171` | Uses `InvalidDataException` (System.IO) for data validation — should be `FormatException` |

### LOW Priority
| File | Issue |
|------|-------|
| `FPSCounter.cs:73` | String interpolation allocates on every FPS change — could use pre-baked string array |
| `VERADialogueController.cs:403` | `WaitForSeconds(_glitchFlickerRate)` allocated per coroutine start, not cached |
| `HoldToEmbarkController.cs + CharSelectFocusManager.cs` | Both add AudioSource in OnEnable — could create duplicates on same GO |
| `QTEController.cs:328` | `WaitForSecondsRealtime(0.5f)` allocated each time, should be cached |
| `AnimatedBar.cs:56` | `FillColor` getter reads resolvedStyle (computed value may differ from set value) |
| `MainMenuBootstrap.cs:83` | `_soundCallbacks` list is dead code (audio moved to MainMenuController) |
| `Constants.cs:71-73` | `static readonly` Vector2 fields — heap allocated (unavoidable for structs) |

## VB-Toolkit `analyze_code` Results
Ran against BattleManager, StatusEffectManager, TitleScreenVFX, AudioManager, GameDatabase — **0 regex anti-pattern hits** (codebase already clean of Find/LINQ/Camera.main in Update). Tool needs deeper architectural analysis capability for VisualElement count and pooling issues.

---

## Task Completion Requirements

### Task: Fix CharSelect UI (Phase 5b/11)
**Completion criteria:**
- [ ] Open Unity, load CharacterSelect scene
- [ ] Verify prev/next hero buttons are clickable and switch heroes
- [ ] Verify tab buttons (Overview/Abilities/Lore) switch content
- [ ] Verify Embark button starts hold-to-embark flow
- [ ] Verify Back button returns to Main Menu
- [ ] Verify gamepad D-pad navigates between zones
- [ ] Take screenshot for visual verification
- [ ] Fix any UXML binding issues found

### Task: VB-Toolkit Quality Upgrades (Phase 7)
**Completion criteria:**
- [ ] Run `unity_qa analyze_code` on all 128 script files (batch)
- [ ] Run `unity_quality aaa_audit` for combined quality check
- [ ] Run `unity_performance profile_scene` on MainMenu + CharSelect scenes
- [ ] Fix all CRITICAL/HIGH findings from VB-toolkit
- [ ] Generate brand VFX with `unity_vfx create_brand_vfx` for at least 3 brands
- [ ] Run `unity_qa check_compile_status` to verify zero errors

### Task: Complete Fix Prompt (Tiers 2-5)
**Completion criteria:**
- [ ] FIX 11: AIPersonality cache leak
- [ ] FIX 12: SoulSwarmVFX double callbacks
- [ ] FIX 13-17: GeometryChanged leaks (5 files)
- [ ] FIX 18: SaveData validation
- [ ] FIX 19: HeroData GetStatAtLevel clamp
- [ ] FIX 21: MonsterData enum casts
- [ ] FIX 22-26: GameManager/AI robustness
- [ ] FIX 29: Combatant events cleared
- [ ] FIX 31-33, 36: CharSelect remaining
- [ ] FIX 37-39: Repo cleanup
- [ ] Score: 39/39 (currently 16/39)

### Task: Repo Infrastructure
**Completion criteria:**
- [ ] Git LFS installed for binary assets (685MB without LFS)
- [ ] Duplicate files consolidated (heroes.json, monsters.json diverged copies reconciled)
- [ ] Old scan reports archived to Docs/archive/scans/
- [ ] All branches synced and pushed

## Permission Fix
Run once in separate terminal:
```bash
claude config set allowedTools "Bash(*),mcp__vb-unity__*,mcp__vb-blender__*,mcp__gemini-cli__*,mcp__codex-cli__*,mcp__github__*,mcp__serena__*,mcp__memory-graph__*,mcp__sequential-thinking__*,mcp__desktop-commander__*,mcp__claude-in-chrome__*,mcp__plugin_context7_context7__*,mcp__plugin_episodic-memory_episodic-memory__*,mcp__plugin_semgrep-plugin_semgrep__*,mcp__blender__*,Edit,Write,Read,Glob,Grep,WebFetch,WebSearch"
```
