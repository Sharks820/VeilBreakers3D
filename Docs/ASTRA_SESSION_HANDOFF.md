# SESSION HANDOFF — FOR ASTRA
**Written:** 2026-09-28
**Topic:** Wolf creature, second round: visual review gate + bite-contact fix
**Why:** The parent session stopped with two things unfinished: the Fable grader and the seeded battle test. It had also hit the monthly spend limit; the weekly limit resets Oct 1, 2pm America/Chicago.

> **Source:** This file is built from the parent session's last transcript. None of the wolf work, `verification/`, the render-verify hook, or the saved memory had been pushed to GitHub when this was written. File paths, hashes and counts below come from that transcript. Check anything else on disk before you rely on it.

---

## 1. Re-anchor first (do this before anything else)

1. `git status` and `git log --oneline -10` on the local machine.
2. Read `CLAUDE.md`, then the lessons the parent session saved to memory this round.
3. Check that the Unity Editor is open and **not** in Play mode. If it is in Play mode, a seeded battle test may still be running. Leave it alone until it finishes.
4. Read the render gate's pending marker. Write down its `captureSetId` and `source`.
   - Expected: `2e89fc8f39843e41695a7a50c6c973a8f7c02165c7dc02f6c7d99225476fff61`, `source=play_mode_camera_rt`, 101 frames.
   - If the marker now names a **different** set, a newer capture happened (the seeded battle test may have rendered frames). A verdict on `2e89fc8f` cannot clear it. Tell the user before you do anything else.

---

## 2. Open work, in priority order

### A. Bite contact: confirm your pass-2 fix
**Status:** 5 of 6 contact checks passed before the fix. The fix compiles, 72/72 tests pass, and the seeded battle test was **running when the session ended. Its result is unknown.**

- **Failing check:** one pounce stops **3.8 cm short**. The limit is **+3 cm**.
- **Attempt 1 (quick fix):** made it worse and was **reverted**. Do not re-apply it.
- **Root cause (your trace):** the aim was checked against a **stale target pose** and **froze 67 ms before contact**.
- **Attempt 2 (your fix):** the aim now keeps updating through the hit frame, using live mouth/skin data.

**Next:**
1. Get the seeded battle test result. If it never finished, re-run it in Play mode and keep hands off Unity for about a minute.
2. Pass criteria, all at once:
   - [ ] The short pounce now stops within the +3 cm limit
   - [ ] The Untamed wolf's bites still land
   - [ ] The chest and legs don't sink into the target
   - [ ] Nothing freezes
   - [ ] Every leap covers at least 95% of its full distance
   - [ ] 72/72 tests still pass, with zero compile errors
3. **If attempt 2 fails:** that is 2 failed attempts on this problem. Per `CLAUDE.md` §1.1–1.2, re-read the context and try a fundamentally different approach, or escalate to the user. Do not stack a third patch on this approach.

### B. Visual gate: finish the independent panel for `2e89fc8f`
| Grader | Role | Score | State |
|---|---|---|---|
| `wolf-model-sonnet-j` | Gate grader | **7.7** (was 6.7) | Done. Lighting 7.8 · Materials 7.5 · Architecture 8.0 · Composition 7.8 |
| `wolf-model-fable-i` | Gate grader | — | **Still running when the session ended.** The agent probably died with it. |
| Kimi | Advisory only, not a gate grader | 7.2 | Done |

**Next:**
1. Look under `verification/` for `VERDICT-wolf-model-fable-i.json` **and** its `vb.grader-receipt@1` artifact.
2. If either is missing, launch a fresh independent `unity-visual-qa` Fable grader on the **same** capture set `2e89fc8f`. It must write its own VERDICT file and its own per-frame decode receipts.
3. Once both receipts are in, merge them into `.claude/hooks/.art-qa-verdict.json` as the **non-grading coordinator**:
   - Schema: `vb.art-panel@2`
   - `captureSetId`: copied **verbatim** from the marker
   - `verdict.frames`: all **101** frames listed
   - Each grader entry names its `vb.grader-receipt@1` artifact with per-frame decode receipts. The exact shape is in the header of `render-verify.js`.
   - Panel score = the **lower** of the two grader scores
4. Report the result to the user.

**⚠ Threshold mismatch. Raise it with the user and don't resolve it yourself:**
- The session treated **7.5** as the user's pass bar. By that bar, Sonnet's 7.7 passes, and the wolf passes if Fable scores ≥ 7.5.
- The stop hook says a panel clears only with **`pass:true, score>=8.5`**.
- With Sonnet at 7.7, the hook **will not clear** on an honest merge, whatever Fable scores. Do not raise scores, edit the hook, or edit the marker to get past it. Report the gap and let the user decide.

### C. Visual follow-ups: HOLD until Fable's grade is in
The rule from the parent session: touch nothing visual until both graders are in and agree on what matters.

- **Top candidate** (if the graders agree): the face at battle distance, seen from the front. It still reads as a black shape. Kimi suggests a faint rim light or glow on the muzzle, **set in the material**.
- Smaller items still open:
  - A pale chest and pale undersides of the front legs show in front close-ups (Kimi)
  - No contact shadow under the paws (Kimi)
  - The clean wolf's idle mouth hangs slightly open (Kimi)
  - The clean wolf's fur reads flat (Sonnet: vendor model, close-up only)
  - The mouth and teeth are plain (Sonnet: vendor model, close-up only)
  - The jump-bite tucks its front legs (Sonnet: vendor model, close-up only)
- **Do not touch:** the 85% corrupted skin. Sonnet says it "already reads well."

**Sequencing note:** capture `2e89fc8f` was taken **before** your bite-contact fix. Grading it is still valid for this art round. Any new capture taken after the fix, or after visual changes, creates a new pending set and needs a new two-grader panel.

---

## 3. What's already done this round (don't redo)

| Issue from the 6.5 review | Result | Evidence |
|---|---|---|
| Pale legs | **Fixed.** Corruption now covers the whole body at 85% | Front-to-rear leg brightness ratio 1.27 → **1.03** |
| Black face | **Better.** Brow, muzzle and ears now read; the wet-black look is kept | Face brightness 13.9 → **22.0** |
| Floating poses | **Fixed.** The ground reaches every wolf | Confirmed by Kimi |
| Jump-bite and hit-reaction timing | **Fixed.** Captured at the jaw's widest point and at the peak of the recoil | Confirmed by Kimi |
| Jaw complaint | Stays resolved | Kimi |
| Leg/body match in side views | Resolved | Kimi |

The stale verdict on disk (`c2c680b0…5dce`) is for an **older** capture set. It cannot speak for `2e89fc8f`.

---

## 4. Hard rules for this handoff

- **Never self-grade.** A frame you graded yourself is not a frame that passed. The 59-round failure came from exactly that. The coordinator only merges; it never scores.
- **Kimi is advisory.** Kimi's 7.2 does not count toward the gate.
- **Play-mode captures only.** Only `play_mode_game_view` or `play_mode_camera_rt` can clear the gate. `scene_view`, `unknown` and edit-mode RT frames cannot.
- **If the Unity Editor is closed, or Play mode is unavailable, say so explicitly and stop.** The gate is designed to let you stop in that case.
- **Hands off Unity** while a seeded battle test is running.
- **One change at a time.** Re-run the tests after each fix. Don't mix the bite-contact work with visual changes in one step.
- **Don't write a pass that didn't happen.** If a check is red, report it as red, with the numbers.

---

## 5. Handing back to the user

Report in this order:
1. Seeded battle test: pass or fail, with the pounce gap in cm
2. Fable's score, and the merged panel score (the lower of the two)
3. The 7.5 vs 8.5 threshold question
4. The recommended next visual change, if the graders agree
