---
paths:
  - "Assets/Scripts/Systems/Audio/**/*.cs"
  - "Assets/Scripts/Audio/**/*.cs"
  - "Assets/Audio/**/*"
---

# Audio System Rules

## Tools
- SFX generation: `unity_audio action=ai_sfx` (ElevenLabs)
- Music loops: `unity_audio action=adaptive_music`
- VO pipeline: `unity_audio action=voice_over_pipeline`
- Spatial audio: `unity_audio action=spatial_audio`

## Known Issues
- All non-music AudioSources must have volume > 0 (PlayOneShot is silent at volume=0)
- VERA audio interactions: cry->"QUIET!", "help me"->silence, plea->growl, afraid->silence

## Clip Standards
- SFX: mono, 44.1/48kHz, 16-bit
- Music: stereo, 44.1/48kHz
- No dynamic range > 12dB (games need consistent mix)
