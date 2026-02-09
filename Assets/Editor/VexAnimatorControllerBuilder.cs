using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// Editor utility that builds an AnimatorController for the Vex character.
    /// Searches for animation clips in the Vex animation folder and creates
    /// a controller with idle, idle variant, and selected states.
    /// </summary>
    public static class VexAnimatorControllerBuilder
    {
        private const string kVexAnimationPath = "Assets/Art/Animations/Characters/Vex/";
        private const string kControllerOutputPath = "Assets/Resources/Art/Animations/Controllers/VexAnimatorController.controller";
        private const string kLogPrefix = "[VexAnimatorControllerBuilder]";

        // Parameter names
        private const string kIdleTimerParam = "IdleTimer";
        private const string kSelectedParam = "Selected";

        // Idle variant transition timing
        private const float kIdleVariantMinTime = 8f;
        private const float kIdleVariantMaxTime = 15f;
        private const float kDefaultTransitionDuration = 0.25f;

        // Preferred clip paths for specific states
        private const string kPrimaryIdlePath = "Assets/Art/Animations/Characters/Vex/ActionAdventure/idle.fbx";
        private const string kWipingSweatPath = "Assets/Art/Animations/Characters/Vex/Standalone/Vex_for_mixamo@Wiping Sweat.fbx";
        private const string kTauntBattlecryPath = "Assets/Art/Animations/Characters/Vex/MeleeAxe/standing taunt battlecry.fbx";
        private const string kTauntChestThumpPath = "Assets/Art/Animations/Characters/Vex/MeleeAxe/standing taunt chest thump.fbx";

        // Procedural clip output paths (used when FBX clips are unavailable)
        private const string kProceduralClipFolder = "Assets/Resources/Art/Animations/Clips";
        private const string kProceduralIdlePath = "Assets/Resources/Art/Animations/Clips/Vex_Idle_Procedural.anim";
        private const string kProceduralSelectedPath = "Assets/Resources/Art/Animations/Clips/Vex_Selected_Procedural.anim";

        /// <summary>
        /// Builds the Vex AnimatorController from discovered animation clips.
        /// Creates idle, idle variant, and selected states on the base layer.
        /// </summary>
        [MenuItem("VeilBreakers/Animation/Diagnose Vex FBX Clips")]
        private static void DiagnoseVexFbxClips()
        {
            // Also check source model
            string[] pathsToCheck = kPriorityFbxPaths
                .Concat(new[] { "Assets/Resources/Art/3D_Models/Characters/Vex.fbx" })
                .ToArray();

            foreach (string fbxPath in pathsToCheck)
            {
                Debug.Log($"{kLogPrefix} --- Diagnosing '{fbxPath}' ---");
                var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"{kLogPrefix}   No ModelImporter found!");
                    continue;
                }

                Debug.Log($"{kLogPrefix}   importAnimation: {importer.importAnimation}, animationType: {importer.animationType}, avatarSetup: {importer.avatarSetup}");

                var defaultClips = importer.defaultClipAnimations;
                Debug.Log($"{kLogPrefix}   defaultClipAnimations: {(defaultClips != null ? defaultClips.Length.ToString() : "null")}");
                if (defaultClips != null)
                    foreach (var dc in defaultClips)
                        Debug.Log($"{kLogPrefix}     default: '{dc.takeName}' name='{dc.name}' frames={dc.firstFrame}-{dc.lastFrame}");

                var configuredClips = importer.clipAnimations;
                Debug.Log($"{kLogPrefix}   clipAnimations: {(configuredClips != null ? configuredClips.Length.ToString() : "null")}");

                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                if (subAssets != null)
                {
                    foreach (Object sub in subAssets)
                    {
                        string typeName = sub != null ? sub.GetType().Name : "null";
                        string name = sub != null ? sub.name : "null";
                        if (sub is AnimationClip ac)
                            Debug.Log($"{kLogPrefix}   SubAsset: [{typeName}] '{name}' length={ac.length}s preview={name.StartsWith("__preview__")}");
                        else
                            Debug.Log($"{kLogPrefix}   SubAsset: [{typeName}] '{name}'");
                    }
                }
                else
                {
                    Debug.Log($"{kLogPrefix}   No sub-assets found.");
                }
            }
        }

        [MenuItem("VeilBreakers/Animation/Build Vex Animator Controller")]
        private static void BuildVexAnimatorController()
        {
            Debug.Log($"{kLogPrefix} Building Vex AnimatorController...");

            // Ensure output directories exist
            string outputDirectory = Path.GetDirectoryName(kControllerOutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                EnsureDirectoryExists(outputDirectory);
            EnsureDirectoryExists(kProceduralClipFolder);

            // Discover FBX-based animation clips
            Dictionary<string, AnimationClip> discoveredClips = DiscoverAnimationClips();
            Debug.Log($"{kLogPrefix} Discovered {discoveredClips.Count} FBX animation clip(s).");

            // Resolve clips (FBX first, then procedural fallback)
            AnimationClip idleClip = ResolveIdleClip(discoveredClips);
            if (idleClip == null)
            {
                Debug.Log($"{kLogPrefix} No FBX idle clip found. Creating procedural idle animation...");
                idleClip = CreateOrLoadProceduralIdleClip();
            }

            AnimationClip idleVariantClip = ResolveIdleVariantClip(discoveredClips, idleClip);
            AnimationClip selectedClip = ResolveSelectedClip(discoveredClips);
            if (selectedClip == null)
            {
                Debug.Log($"{kLogPrefix} No FBX selected clip found. Creating procedural selected animation...");
                selectedClip = CreateOrLoadProceduralSelectedClip();
            }

            // Create or overwrite the controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(kControllerOutputPath);

            // Add parameters
            controller.AddParameter(kIdleTimerParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(kSelectedParam, AnimatorControllerParameterType.Bool);

            // Configure the base layer
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;

            // Create states
            AnimatorState idleState = CreateState(stateMachine, "Idle", idleClip);
            AnimatorState idleVariantState = CreateState(stateMachine, "IdleVariant", idleVariantClip);
            AnimatorState selectedState = CreateState(stateMachine, "Selected", selectedClip);

            // Set Idle as the default state
            stateMachine.defaultState = idleState;

            // Position states for readability in the Animator window
            stateMachine.entryPosition = new Vector3(-200, 0, 0);
            stateMachine.anyStatePosition = new Vector3(-200, 200, 0);
            stateMachine.exitPosition = new Vector3(600, 0, 0);

            PositionState(idleState, stateMachine, new Vector3(200, 0, 0));
            PositionState(idleVariantState, stateMachine, new Vector3(200, 100, 0));
            PositionState(selectedState, stateMachine, new Vector3(200, 200, 0));

            // Transition: Idle -> IdleVariant (when IdleTimer exceeds threshold)
            // Uses the midpoint of min/max as the threshold; runtime code should
            // increment IdleTimer and randomize the trigger point between 8-15s.
            float idleTimerThreshold = (kIdleVariantMinTime + kIdleVariantMaxTime) / 2f;
            AnimatorStateTransition idleToVariant = idleState.AddTransition(idleVariantState);
            idleToVariant.AddCondition(AnimatorConditionMode.Greater, idleTimerThreshold, kIdleTimerParam);
            idleToVariant.hasExitTime = false;
            idleToVariant.duration = kDefaultTransitionDuration;
            idleToVariant.hasFixedDuration = true;

            // Transition: IdleVariant -> Idle (when clip finishes)
            AnimatorStateTransition variantToIdle = idleVariantState.AddTransition(idleState);
            variantToIdle.hasExitTime = true;
            variantToIdle.exitTime = 1f;
            variantToIdle.duration = kDefaultTransitionDuration;
            variantToIdle.hasFixedDuration = true;

            // Transition: Any State -> Selected (when Selected bool is true)
            AnimatorStateTransition anyToSelected = stateMachine.AddAnyStateTransition(selectedState);
            anyToSelected.AddCondition(AnimatorConditionMode.If, 0, kSelectedParam);
            anyToSelected.hasExitTime = false;
            anyToSelected.duration = kDefaultTransitionDuration;
            anyToSelected.hasFixedDuration = true;

            // Transition: Selected -> Idle (when Selected bool is false)
            AnimatorStateTransition selectedToIdle = selectedState.AddTransition(idleState);
            selectedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, kSelectedParam);
            selectedToIdle.hasExitTime = false;
            selectedToIdle.duration = kDefaultTransitionDuration;
            selectedToIdle.hasFixedDuration = true;

            // Save the controller
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{kLogPrefix} Successfully built VexAnimatorController at '{kControllerOutputPath}'.");
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
        }

        /// <summary>
        /// Creates or loads a procedural idle animation clip.
        /// Uses humanoid muscle channels for a subtle breathing/sway motion.
        /// </summary>
        private static AnimationClip CreateOrLoadProceduralIdleClip()
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(kProceduralIdlePath);
            if (existing != null)
            {
                Debug.Log($"{kLogPrefix} Loaded existing procedural idle clip.");
                return existing;
            }

            var clip = new AnimationClip();
            clip.name = "Vex_Idle_Procedural";

            // Breathing: subtle spine front-back curve over 4 seconds
            float duration = 4f;
            var spineKeys = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.5f, 0.08f),
                new Keyframe(duration, 0f),
            };
            var spineCurve = new AnimationCurve(spineKeys);
            // Spine Front-Back muscle channel
            clip.SetCurve("", typeof(Animator), "Spine Front-Back", spineCurve);

            // Subtle chest expansion
            var chestKeys = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.45f, 0.05f),
                new Keyframe(duration, 0f),
            };
            var chestCurve = new AnimationCurve(chestKeys);
            clip.SetCurve("", typeof(Animator), "Chest Front-Back", chestCurve);

            // Very subtle weight shift (left-right)
            var hipKeys = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.25f, 0.02f),
                new Keyframe(duration * 0.75f, -0.02f),
                new Keyframe(duration, 0f),
            };
            var hipCurve = new AnimationCurve(hipKeys);
            clip.SetCurve("", typeof(Animator), "Spine Left-Right", hipCurve);

            // Make it loop
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, kProceduralIdlePath);
            Debug.Log($"{kLogPrefix} Created procedural idle clip at '{kProceduralIdlePath}'.");
            return clip;
        }

        /// <summary>
        /// Creates or loads a procedural selected/taunt animation clip.
        /// A more energetic motion for when the character is selected.
        /// </summary>
        private static AnimationClip CreateOrLoadProceduralSelectedClip()
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(kProceduralSelectedPath);
            if (existing != null)
            {
                Debug.Log($"{kLogPrefix} Loaded existing procedural selected clip.");
                return existing;
            }

            var clip = new AnimationClip();
            clip.name = "Vex_Selected_Procedural";

            float duration = 2f;

            // Confident chest puff - lean back then forward
            var spineKeys = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f, -0.15f),  // Lean back
                new Keyframe(0.8f, 0.1f),    // Forward
                new Keyframe(1.2f, 0.05f),   // Settle
                new Keyframe(duration, 0f),
            };
            clip.SetCurve("", typeof(Animator), "Spine Front-Back", new AnimationCurve(spineKeys));

            // Chest expansion
            var chestKeys = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.4f, 0.12f),
                new Keyframe(1.0f, 0.08f),
                new Keyframe(duration, 0f),
            };
            clip.SetCurve("", typeof(Animator), "Chest Front-Back", new AnimationCurve(chestKeys));

            // Don't loop - plays once on selection
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, kProceduralSelectedPath);
            Debug.Log($"{kLogPrefix} Created procedural selected clip at '{kProceduralSelectedPath}'.");
            return clip;
        }

        /// <summary>
        /// Priority FBX paths that MUST have clip definitions configured.
        /// Only these are auto-configured if missing (to avoid mass reimport).
        /// </summary>
        private static readonly string[] kPriorityFbxPaths = new[]
        {
            kPrimaryIdlePath,
            kWipingSweatPath,
            kTauntBattlecryPath,
            kTauntChestThumpPath,
        };

        /// <summary>
        /// Discovers all animation clips from FBX files under the Vex animation folder.
        /// Returns a dictionary keyed by the asset path of each clip.
        /// </summary>
        private static Dictionary<string, AnimationClip> DiscoverAnimationClips()
        {
            var clips = new Dictionary<string, AnimationClip>();

            string fullPath = Path.Combine(Application.dataPath,
                kVexAnimationPath.Replace("Assets/", ""));

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning(
                    $"{kLogPrefix} Vex animation folder not found at '{fullPath}'. " +
                    "Controller will be created with empty states.");
                return clips;
            }

            string[] fbxFiles = Directory.GetFiles(fullPath, "*.fbx", SearchOption.AllDirectories);

            foreach (string fbxPath in fbxFiles)
            {
                string relativePath = "Assets" + fbxPath
                    .Replace(Application.dataPath, "")
                    .Replace('\\', '/');

                TryLoadClipsFromAsset(relativePath, clips);
            }

            // Check if priority clips are missing - configure meta files and/or force reimport
            var missingPriorityPaths = new List<string>();
            foreach (string priorityPath in kPriorityFbxPaths)
            {
                if (FindClipByAssetPath(clips, priorityPath) == null)
                    missingPriorityPaths.Add(priorityPath);
            }

            if (missingPriorityPaths.Count > 0)
            {
                // First ensure meta files have clip definitions
                foreach (string priorityPath in missingPriorityPaths)
                {
                    Debug.Log($"{kLogPrefix} Priority clip missing from '{priorityPath}', checking meta...");
                    ConfigureClipViaMeta(priorityPath);
                }

                // Force reimport the specific FBX files (not a full project refresh)
                foreach (string priorityPath in missingPriorityPaths)
                {
                    Debug.Log($"{kLogPrefix} Force reimporting '{priorityPath}'...");
                    AssetDatabase.ImportAsset(priorityPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }

                // Retry clip discovery after reimport
                foreach (string priorityPath in missingPriorityPaths)
                {
                    TryLoadClipsFromAsset(priorityPath, clips);
                }

                Debug.Log($"{kLogPrefix} After reimport: {clips.Count} clip(s) discovered.");
            }

            return clips;
        }

        /// <summary>
        /// Configures clip definitions by directly editing the .meta file text.
        /// This avoids SaveAndReimport() which can trigger domain reload.
        /// </summary>
        private static bool ConfigureClipViaMeta(string assetPath)
        {
            string metaPath = assetPath + ".meta";
            string fullMetaPath = Path.Combine(Application.dataPath, "..", metaPath);
            fullMetaPath = Path.GetFullPath(fullMetaPath);

            if (!File.Exists(fullMetaPath))
            {
                Debug.LogWarning($"{kLogPrefix} Meta file not found: '{fullMetaPath}'");
                return false;
            }

            string content = File.ReadAllText(fullMetaPath);
            if (!content.Contains("clipAnimations: []"))
            {
                Debug.Log($"{kLogPrefix} '{assetPath}' already has clip definitions, skipping.");
                return false;
            }

            // Derive clip name from the FBX filename
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            // Handle "Vex_for_mixamo@Wiping Sweat" format
            int atIndex = fileName.IndexOf('@');
            string clipName = atIndex >= 0 ? fileName.Substring(atIndex + 1) : fileName;

            string clipEntry = $@"    clipAnimations:
    - serializedVersion: 16
      name: {clipName}
      takeName: mixamo.com
      internalID: 0
      firstFrame: 0
      lastFrame: 99999
      wrapMode: 0
      orientationOffsetY: 0
      level: 0
      cycleOffset: 0
      loop: 0
      hasAdditiveReferencePose: 0
      loopTime: {(clipName.ToLowerInvariant().Contains("idle") ? "1" : "0")}
      loopBlend: 0
      loopBlendOrientation: 0
      loopBlendPositionY: 0
      loopBlendPositionXZ: 0
      keepOriginalOrientation: 0
      keepOriginalPositionY: 1
      keepOriginalPositionXZ: 0
      heightFromFeet: 0
      mirror: 0
      bodyMask: 01000000010000000100000001000000010000000100000001000000010000000100000001000000010000000100000001000000
      curves: []
      events: []
      transformMask: []
      maskType: 3
      maskSource: {{fileID: 0}}
      additiveReferencePoseFrame: 0";

            content = content.Replace("    clipAnimations: []", clipEntry);
            File.WriteAllText(fullMetaPath, content);
            Debug.Log($"{kLogPrefix} Configured clip '{clipName}' in meta file for '{assetPath}'.");
            return true;
        }

        private static bool TryLoadClipsFromAsset(string relativePath, Dictionary<string, AnimationClip> clips)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(relativePath);
            if (subAssets == null)
                return false;

            bool foundClip = false;
            foreach (Object subAsset in subAssets)
            {
                if (subAsset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    string key = relativePath + "|" + clip.name;
                    if (!clips.ContainsKey(key))
                    {
                        clips[key] = clip;
                    }
                    foundClip = true;
                }
            }
            return foundClip;
        }

        /// <summary>
        /// Resolves the primary idle animation clip.
        /// Prefers the ActionAdventure idle.fbx, then falls back to any clip
        /// whose name contains "idle".
        /// </summary>
        private static AnimationClip ResolveIdleClip(Dictionary<string, AnimationClip> clips)
        {
            // Try the preferred primary idle path
            AnimationClip clip = FindClipByAssetPath(clips, kPrimaryIdlePath);
            if (clip != null)
            {
                Debug.Log($"{kLogPrefix} Using primary idle clip from '{kPrimaryIdlePath}'.");
                return clip;
            }

            // Try Wiping Sweat as fallback
            clip = FindClipByAssetPath(clips, kWipingSweatPath);
            if (clip != null)
            {
                Debug.Log($"{kLogPrefix} Using 'Wiping Sweat' as idle clip (primary idle not found).");
                return clip;
            }

            // Fall back to any clip with "idle" in the name
            clip = FindFirstClipByKeyword(clips, "idle");
            if (clip != null)
            {
                Debug.Log($"{kLogPrefix} Using fallback idle clip: '{clip.name}'.");
                return clip;
            }

            Debug.LogWarning($"{kLogPrefix} No idle animation clip found. Idle state will be empty.");
            return null;
        }

        /// <summary>
        /// Resolves an idle variant clip, different from the primary idle.
        /// Looks for alternate idle clips (idle (2), idle (3), etc.) or Wiping Sweat.
        /// </summary>
        private static AnimationClip ResolveIdleVariantClip(
            Dictionary<string, AnimationClip> clips,
            AnimationClip primaryIdleClip)
        {
            // Try Wiping Sweat first (good character select variant)
            AnimationClip clip = FindClipByAssetPath(clips, kWipingSweatPath);
            if (clip != null && clip != primaryIdleClip)
            {
                Debug.Log($"{kLogPrefix} Using 'Wiping Sweat' as idle variant clip.");
                return clip;
            }

            // Try to find a different idle clip than the primary
            var idleClips = clips.Values
                .Where(c => c.name.ToLowerInvariant().Contains("idle") && c != primaryIdleClip)
                .ToList();

            if (idleClips.Count > 0)
            {
                clip = idleClips[0];
                Debug.Log($"{kLogPrefix} Using '{clip.name}' as idle variant clip.");
                return clip;
            }

            // Try looking/scanning animations as variants
            clip = FindFirstClipByKeyword(clips, "looking");
            if (clip != null && clip != primaryIdleClip)
            {
                Debug.Log($"{kLogPrefix} Using '{clip.name}' as idle variant clip.");
                return clip;
            }

            Debug.LogWarning(
                $"{kLogPrefix} No idle variant animation clip found. " +
                "IdleVariant state will be empty.");
            return null;
        }

        /// <summary>
        /// Resolves a clip for the Selected state.
        /// Prefers taunt/power-up animations.
        /// </summary>
        private static AnimationClip ResolveSelectedClip(Dictionary<string, AnimationClip> clips)
        {
            // Try taunt battlecry first
            AnimationClip clip = FindClipByAssetPath(clips, kTauntBattlecryPath);
            if (clip != null)
            {
                Debug.Log($"{kLogPrefix} Using 'standing taunt battlecry' as selected clip.");
                return clip;
            }

            // Try taunt chest thump
            clip = FindClipByAssetPath(clips, kTauntChestThumpPath);
            if (clip != null)
            {
                Debug.Log($"{kLogPrefix} Using 'standing taunt chest thump' as selected clip.");
                return clip;
            }

            // Fall back to any taunt
            clip = FindFirstClipByKeyword(clips, "taunt");
            if (clip != null)
            {
                Debug.Log($"{kLogPrefix} Using '{clip.name}' as selected clip.");
                return clip;
            }

            // Fall back to any casting animation
            clip = FindFirstClipByKeyword(clips, "casting");
            if (clip != null)
            {
                Debug.Log($"{kLogPrefix} Using '{clip.name}' as selected clip (no taunt found).");
                return clip;
            }

            Debug.LogWarning(
                $"{kLogPrefix} No selected/taunt animation clip found. " +
                "Selected state will be empty.");
            return null;
        }

        /// <summary>
        /// Finds an animation clip by matching its source FBX asset path.
        /// </summary>
        private static AnimationClip FindClipByAssetPath(
            Dictionary<string, AnimationClip> clips,
            string assetPath)
        {
            foreach (var kvp in clips)
            {
                if (kvp.Key.StartsWith(assetPath, System.StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }

        /// <summary>
        /// Finds the first animation clip whose name contains the given keyword
        /// (case insensitive).
        /// </summary>
        private static AnimationClip FindFirstClipByKeyword(
            Dictionary<string, AnimationClip> clips,
            string keyword)
        {
            string lowerKeyword = keyword.ToLowerInvariant();

            foreach (var kvp in clips)
            {
                if (kvp.Value.name.ToLowerInvariant().Contains(lowerKeyword))
                    return kvp.Value;
            }

            return null;
        }

        /// <summary>
        /// Creates an AnimatorState with the given name and optional motion clip.
        /// </summary>
        private static AnimatorState CreateState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip)
        {
            AnimatorState state = stateMachine.AddState(stateName);

            if (clip != null)
            {
                state.motion = clip;
                Debug.Log($"{kLogPrefix} State '{stateName}' assigned clip '{clip.name}'.");
            }
            else
            {
                Debug.LogWarning(
                    $"{kLogPrefix} State '{stateName}' created without a clip. " +
                    "Assign an animation manually.");
            }

            return state;
        }

        /// <summary>
        /// Positions an AnimatorState within the state machine for visual clarity
        /// in the Animator window.
        /// </summary>
        private static void PositionState(
            AnimatorState state,
            AnimatorStateMachine stateMachine,
            Vector3 position)
        {
            // Unity's Animator window uses ChildAnimatorState for positioning
            ChildAnimatorState[] childStates = stateMachine.states;
            for (int i = 0; i < childStates.Length; i++)
            {
                if (childStates[i].state == state)
                {
                    childStates[i].position = position;
                    break;
                }
            }

            stateMachine.states = childStates;
        }

        /// <summary>
        /// Ensures the specified directory exists, creating it if necessary.
        /// Handles Unity asset path format.
        /// </summary>
        private static void EnsureDirectoryExists(string assetFolderPath)
        {
            // Convert asset path to full system path
            string fullPath = Path.Combine(
                Application.dataPath,
                "..",
                assetFolderPath);
            fullPath = Path.GetFullPath(fullPath);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
                Debug.Log($"{kLogPrefix} Created directory: '{assetFolderPath}'");
            }
        }
    }
}
