Here is the production and scope plan for VeilBreakers3D.

### 1. Vision & Pillars

**Vision:** VeilBreakers3D will deliver a visceral, tactical RPG experience where players master a deep, brand-based combat system to capture and evolve grotesque monsters within a grimdark, painterly world. It merges the strategic depth of party-based RPGs with the immediacy of action combat, all presented with a polished, atmospheric AAA feel that respects the player's intelligence and rewards strategic mastery.

**Pillars:**
1.  **Visceral & Tactical Combat:** Every action is responsive and impactful. Combat is fast-paced but requires thoughtful positioning, ability synergy, and exploitation of the Brand system.
2.  **Grimdark Painterly Realism:** The world is oppressive and dangerous, but beautiful in its decay. The art style is cohesive, atmospheric, and uses stylized realism to create memorable characters and environments.
3.  **Deep Brand & Synergy Mastery:** The core of gameplay is the 10-Brand combat system. Player success is tied directly to their ability to master Brand interactions and synergize them with their Veilbreaker Path.
4.  **Meaningful Monster Evolution:** Capturing and evolving monsters is a central reward loop. Evolutions are significant, offering dramatic visual and gameplay changes that unlock new strategic possibilities.
5.  **Oppressive & Mysterious Atmosphere:** The world feels ancient, haunted, and actively hostile. The Veil is a constant, tangible presence, influencing the environment, story, and gameplay.

---

### 2. Full Scope Breakdown: Epics

#### **Epic 1: Core Combat Loop & AI**
*   **Goal:** Implement and polish the full real-time combat experience, including player controls, monster abilities, and enemy AI behavior.
*   **Player-Facing Outcome:** Players can engage in challenging, satisfying tactical combat against AI-controlled monsters using the full suite of 10-Brand system mechanics and a 6-slot ability loadout.
*   **Key Systems/Components:**
    *   **Scripts:** `PlayerController.cs`, `AbilityController.cs`, `MonsterAI.cs` (FSM-based), `CombatEngine.cs`, `BrandSystem.cs`, `VFXManager.cs`, `SFXManager.cs`.
    *   **Data:** ScriptableObjects for `AbilityData`, `MonsterStats`, `BrandData`.
    *   **Scene:** A dedicated `_CombatTest` scene for rapid iteration.
*   **Dependencies:** Core system designs, basic monster models with rigs.
*   **Definition of Done:**
    *   \[ ] Player can control a Veilbreaker and a party of up to 3 monsters.
    *   \[ ] Player can execute all 6 abilities in their active loadout.
    *   \[ ] All 10 Brands have functional interaction logic (damage mods, status effects).
    *   \[ ] Enemy AI exhibits basic tactical behavior (uses abilities, targets threats, repositions).
    *   \[ ] Combat encounters can be initiated, won, or lost, and the game state resets correctly.
    *   \[ ] Placeholder VFX and SFX are integrated for all core abilities.

#### **Epic 2: Monster Progression & Management**
*   **Goal:** Implement the systems for capturing, evolving, and managing a roster of monsters.
*   **Player-Facing Outcome:** Players feel a strong sense of investment in their monsters as they capture, train, and evolve them, directly impacting their strategic options for combat.
*   **Key Systems/Components:**
    *   **Scripts:** `CaptureSystem.cs`, `EvolutionSystem.cs`, `MonsterRoster.cs`, `PartyManagement_UI.cs`.
    *   **UI:** UI Toolkit `.uxml` and `.uss` files for the Bestiary, Roster, and Party Management screens.
    *   **Data:** ScriptableObjects for `EvolutionTreeData`, `MonsterEncounterData`.
*   **Dependencies:** Core Combat Loop (for weakening monsters), UI System shell.
*   **Definition of Done:**
    *   \[ ] Player can initiate and succeed/fail at capturing a weakened monster below a health threshold.
    *   \[ ] Monsters gain experience and level up after combat victories.
    *   \[ ] A monster can be evolved into a new form via a UI screen when evolution criteria are met, with a clear visual and statistical change.
    *   \[ ] Player can view their full collection of captured monsters.
    *   \[ ] Player can assemble an active combat party from their roster.

#### **Epic 3: Player Progression & Customization**
*   **Goal:** Implement the Veilbreaker Path, Path-Brand Synergy, and Corruption systems.
*   **Player-Facing Outcome:** The player's choices feel impactful, offering distinct gameplay styles and narrative trade-offs through the Path and Corruption mechanics.
*   **Key Systems/Components:**
    *   **Scripts:** `VeilbreakerPathSystem.cs`, `PathBrandSynergySystem.cs`, `CorruptionSystem.cs`, `PlayerStats.cs`.
    *   **UI:** UI Toolkit screens for visualizing Path progression and Corruption status.
    *   **Data:** ScriptableObjects for `VeilbreakerPathData` (defining the 4 paths and their tiers).
*   **Dependencies:** Core system designs, UI System shell.
*   **Definition of Done:**
    *   \[ ] Player can select and commit to one of the 4 Veilbreaker Paths.
    *   \[ ] Path abilities and bonuses unlock at defined progression points.
    *   \[ ] Tiered Path-Brand synergy bonuses are calculated and applied correctly in combat.
    *   \[ ] Corruption level changes based on defined in-game actions (e.g., using specific abilities, quest choices).
    *   \[ ] The positive and negative effects of Corruption are applied to the player character.

---

### 3. Phase Plan

*   **Pre-Production (Complete):** Core systems designed, tech stack chosen, UI framework established.
*   **Vertical Slice (10 weeks):**
    *   **Deliverables:** A 15-minute, polished demo proving the core fantasy. Includes: one fully playable combat encounter, the "First Zone" environment, one complete monster capture-and-evolve loop, one Veilbreaker Path progression to Tier 2, and a functional main menu -> game -> exit loop.
    *   **Quality Gates:** Stable 60 FPS on target min-spec PC. DoD for core epics are met for the slice's content. Combat "feel" is reviewed and approved. No crash bugs within the slice's scope.
*   **Alpha (16 weeks):**
    *   **Deliverables:** Feature Complete. All Veilbreaker Paths, all monster evolution trees, and all core game systems are implemented. All game zones are "grey-boxed" and playable from start to finish. All monster and player abilities are functional, even with placeholder assets.
    *   **Quality Gates:** Feature complete. No placeholder *code*. The game is playable from start to finish without blockers. Performance budgets are being met across all grey-box levels.
*   **Beta (12 weeks):**
    *   **Deliverables:** Content Complete. All final art, models, textures, VFX, and audio are integrated. All quests, dialogue, and text are final. The team's focus shifts entirely to bug fixing, balancing, and polish.
    *   **Quality Gates:** Zero "A" priority (crash, blocker) bugs. Performance is at or above target on min-spec and recommended-spec machines. Game is approved by internal and/or external QA.
*   **Release Candidate (RC) (4 weeks):**
    *   **Deliverables:** A potentially shippable build. Only critical, show-stopping bugs discovered during final regression testing are fixed.
    *   **Quality Gates:** Build is declared "golden." No changes unless they fix a launch-blocking bug. Build passes all required platform checks (e.g., Steam).

---

### 4. Build & CI Plan (GitHub Actions)

*   **Branching Strategy:**
    *   `main`: Shipped/stable builds. Merges only from `develop` for releases.
    *   `develop`: The primary integration branch. Represents the current Alpha/Beta state. Nightly builds are generated from here.
    *   `feature/<task-name>`: All new work is done here. Merged into `develop` via Pull Requests (PRs).
    *   `hotfix/<bug-name>`: For critical fixes on `main`. Merged into `main` and `develop`.
*   **Automated Build Workflow (`.github/workflows/build-and-test.yml`):**
    1.  **Trigger:** On `push` to `develop` or `workflow_dispatch`.
    2.  **Checkout & Cache:** Checkout code, cache the project's `Library` folder.
    3.  **Activate License:** Use `game-ci/unity-request-activation-file` and secrets.
    4.  **Run Tests:** Use `game-ci/unity-test-runner` to execute all EditMode and PlayMode tests. Fails the workflow on any test failure.
    5.  **Build Project:** Use `game-ci/unity-builder` to create a `StandaloneWindows64` build using a static `BuildScript.cs` method.
    6.  **Upload Artifact:** Upload the zipped build and test results.
*   **Artifact Naming:** `VeilBreakers3D_v<Major>.<Minor>.<BuildNum>-<Branch>`. Example: `VeilBreakers3D_v0.2.123-develop`.
*   **Test Strategy:**
    *   **EditMode (`Assets/Tests/EditMode`):** For all system logic, data validation, and non-visual components. Must be fast.
    *   **PlayMode (`Assets/Tests/PlayMode`):** For AI behavior, UI interactions, and player controls.
    *   **Smoke Test:** A dedicated PlayMode test (`SmokeTest.unity`) that loads critical scenes (Main Menu, First Zone) and asserts that no errors are logged on startup. This is part of the CI test suite.
*   **Performance Budgets:**
    *   **Framework:** Use the `Unity.PerformanceTesting` package.
    *   **Checks:** Create performance tests for spawning monsters, activating VFX-heavy abilities, and loading zones.
    *   **Automation:** Run these tests in CI. Set failure thresholds for mean frame time and managed memory allocation to prevent performance regressions.

---

### 5. AI Generation Pipeline

*   **What to Safely AI-Generate:**
    *   **Concept Art:** Characters, environments, monsters.
    *   **Textures:** Stylized environmental and prop textures, decals (grime, blood).
    *   **VFX Textures:** Noise textures, erosion maps, and sprite sheets for effects.
    *   **SFX:** Ambient loops, UI feedback, and basic ability sounds (as a base for layering).
    *   **Placeholder Voice/Copy:** Monster "barks" and initial drafts of lore entries.
    *   **UI Elements:** Icons, borders, and background textures.
*   **What NOT to AI-Generate (or requires heavy human oversight):**
    *   **Final Code/Logic:** Never. AI can be used for boilerplate suggestions, but must be reviewed.
    *   **Hero Character Models & Rigs:** Requires precise topology for animation.
    *   **Key Gameplay Animations:** Player locomotion, signature attacks. These define the "feel."
    *   **Level Design & Flow:** AI can't ensure good pacing and gameplay.
    *   **Final Music Score & Sound Design:** Needs a human touch for emotional impact.
*   **Workflow & Human Review Gates:**
    1.  **Naming Convention:** Raw AI assets must be saved to `Assets/AI_Source/<Category>/<AssetName>_ai_raw.png`. They are **not** committed to `develop`.
    2.  **Review:** An asset cannot be used until the Art Director approves it for style consistency.
    3.  **Cleanup:** An artist must clean up, optimize (e.g., ensure power-of-two dimensions), and create proper in-engine assets (materials, prefabs).
    4.  **Integration:** The final, cleaned asset is moved to its permanent project location (e.g., `Assets/Art/Textures/Environments`) and committed via a feature branch PR.

---

### 6. Top 15 Risks & Mitigations

1.  **Risk:** Combat "Feel" isn't fun.
    **Mitigation:** Prioritize combat iteration during the Vertical Slice. Rapidly prototype controls, timing, and feedback.
2.  **Risk:** Scope creep from new feature ideas.
    **Mitigation:** Adhere strictly to the phased plan. All changes must be approved and weighed against the ship date.
3.  **Risk:** AI-generated art lacks a cohesive style.
    **Mitigation:** Establish a rigorous Art Bible and prompting guide. The Art Director is the final gatekeeper for all assets.
4.  **Risk:** Performance targets are missed on min-spec hardware.
    **Mitigation:** Integrate automated performance testing into CI from day one. Set and enforce strict budgets for draw calls, polys, and memory.
5.  **Risk:** The 10-Brand system is too complex for new players.
    **Mitigation:** Design a clear combat UI that highlights advantages/disadvantages. Develop a robust, non-intrusive tutorial system.
6.  **Risk:** Monster evolution feels like a simple model swap.
    **Mitigation:** Invest in a high-impact evolution sequence (VFX, SFX) and ensure the resulting stat/ability changes are significant.
7.  **Risk:** The planned Unity 6 / URP migration breaks the project late in development.
    **Mitigation:** Maintain the `backup/pre-unity6` branch. Periodically attempt the upgrade on a separate branch to log and address breaking changes proactively.
8.  **Risk:** Build pipeline is slow or unstable, hindering iteration.
    **Mitigation:** Use CI caching aggressively. Ensure build agents are sufficiently powerful. A slow build is a bug.
9.  **Risk:** Small team burnout from AAA ambitions.
    **Mitigation:** Be realistic with phase timeboxes. Aggressively cut scope, not quality or team health.
10. **Risk:** Data management with thousands of ScriptableObjects becomes unmanageable.
    **Mitigation:** Invest time in custom editor tools and data validators to streamline data entry and prevent errors.
11. **Risk:** UI Toolkit performance degrades on complex screens.
    **Mitigation:** Profile the UI continuously. Use best practices: visual element recycling, disabling hidden panels, and minimizing layout recalculations.
12. **Risk:** The title screen's VFX overlay blocks or degrades UI performance.
    **Mitigation:** Ensure the UI camera renders on top with a higher depth. Render the VFX to a separate texture or use a dedicated render pass that doesn't interfere with UI batching.
13. **Risk:** The game is not balanced, leading to dominant strategies.
    **Mitigation:** Schedule specific "balancing sprints" during Beta. Use analytics from playtests to identify overpowered or underused abilities/monsters.
14. **Risk:** The "grimdark" art style becomes visually monotonous.
    **Mitigation:** Enforce the "painterly" pillar. Use strategic color, lighting, and VFX to create contrast and points of interest.
15. **Risk:** The tactical combat feels slow compared to the "action-forward" goal.
    **Mitigation:** Focus on animation responsiveness, ability queuing, and cancel windows to ensure the player always feels in control.

---

### 7. Immediate 14-Day Plan: Sprint 1 (Vertical Slice Kickoff)

*   **Priority 1 (CI/CD & Project Foundation):**
    *   \[ ] Task: Create the initial `.github/workflows/build-and-test.yml` file. (1 day)
    *   \[ ] Task: Create `BuildScript.cs` and configure the project for automated builds and testing. (1 day)
    *   \[ ] Task: Run the first successful `develop` branch build from a GitHub Action. (0.5 days)
*   **Priority 2 (Core Combat - First Pass):**
    *   \[ ] Task: Implement basic player character movement and camera controls in a test scene. (1 day)
    *   \[ ] Task: Implement a single Brand ability (e.g., a Fire attack) with placeholder VFX and damage logic. (1 day)
    *   \[ ] Task: Create a simple "dummy" enemy AI that can be damaged and defeated. (1 day)
    *   \[ ] Task: Create the first EditMode test for the Brand System (e.g., `FireDealsCorrectDamage`). (1 day)
*   **Priority 3 (Art & AI Pipeline):**
    *   \[ ] Task: Write the first draft of the Art Bible and AI Prompting Guidelines. (2 days)
    *   \[ ] Task: Generate, clean, and import the first 5 AI-assisted environmental textures (e.g., stone, dirt, wood) and commit them to the project. (2 days)
*   **Priority 4 (UI & VFX Validation):**
    *   \[ ] Task: Create a test scene to confirm the title screen VFX overlay renders correctly without interfering with the Main Menu UI buttons. (0.5 days)
*   **Priority 5 (Backlog Refinement):**
    *   \[ ] Task: Create placeholder ScriptableObjects for all 10 Brands and the 4 Veilbreaker Paths. (1 day)
    *   \[ ] Task: Decompose "Epic 1: Core Combat Loop & AI" into smaller user stories for the next sprint. (1 day)
