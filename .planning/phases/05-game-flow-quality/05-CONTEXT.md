---
phase: 05-game-flow-quality
---

# Phase 5: Game Flow & Quality Context

## Current State

### Game Flow
- Bootstrap (scene 0) initializes all managers in 4 phases, then loads MainMenu
- MainMenu has New Game, Continue, Settings, Credits, Exit buttons
- Settings button is wired via MainMenuBootstrap event handler -> OpenSettings()
- MainMenuController.ShowSettings() has a TODO stub but actual flow works through events
- CharacterSelect loads via VBSceneManager or SceneManager.LoadScene
- Embark flow: hold-to-embark -> cinematic -> save -> load Overworld

### Settings Panel
- SettingsPanelController exists (71KB, fully implemented with Audio/Graphics/Controls tabs)
- SettingsManager handles persistence via PlayerPrefs JSON
- MainMenuBootstrap creates settings overlay, instantiates template, initializes controller
- OnClose event wired to CloseSettings in MainMenuBootstrap

### Scene Loading
- VBSceneManager provides fade transitions, async loading, progress tracking
- ScreenTransition singleton also exists for UI-level transitions
- Both are used in different parts of the codebase

### Known Issues
- MainMenuController.ShowSettings() has dead TODO code (actual flow uses events)
- Need to verify no flash of wrong scene on startup
- Need full code quality audit across all Phase 1-4 modified files
