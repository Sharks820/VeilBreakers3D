using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using VeilBreakers.Core;

namespace VeilBreakers.UI.VFX
{
    /// <summary>
    /// Title screen video background subsystem.
    /// Manages ping-pong dual VideoPlayer setup, RenderTextures,
    /// and seamless looping via forward + reversed video files.
    /// </summary>
    public class TitleScreenVideoBackground : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZEFIELD CONFIG
        // =============================================================================

        [Header("Background Override")]
        [SerializeField] internal bool _overrideBackgroundWithPortal = true;
        [SerializeField, Range(0f, 1f)] internal float _backgroundDarken = 0.00f;
        [Tooltip("Optional. Auto-loads from Resources/Art/UI/MainMenu/mainmenu_background_portal when empty.")]
        [SerializeField] internal Texture2D _backgroundPortalTexture;

        [Header("Background Video (Looping)")]
        [SerializeField] internal bool _useVideoBackground = true;
        [SerializeField] internal VideoClip _backgroundVideoClip;
        [Tooltip("Path to video in StreamingAssets or Resources if clip not assigned")]
        [SerializeField] internal string _videoPath = "Art/UI/MainMenu/background_video";

        // =============================================================================
        // STATE
        // =============================================================================

        private VideoPlayer _videoPlayerForward;
        private VideoPlayer _videoPlayerReversed;
        private RenderTexture _videoRenderTextureForward;
        private RenderTexture _videoRenderTextureReversed;
        private bool _usePingPongLoop;
        private double _videoLength;
        private bool _isVideoPlaying;
        private string _videoFilePath;
        private string _videoFilePathReversed;
        private VisualElement _backgroundElement;
        private VisualElement _videoOverlayElement;
        private bool _isDestroyed;
        private Coroutine _deferredVideoSetupCoroutine;
        private bool _videoSetupQueued;

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize with the background element reference.
        /// </summary>
        public void Initialize(VisualElement backgroundElement)
        {
            _backgroundElement = backgroundElement;
        }

        /// <summary>
        /// Start video background playback (deferred).
        /// </summary>
        public void StartPlayback()
        {
            ApplyBackground();
        }

        /// <summary>
        /// Get whether video is currently playing.
        /// </summary>
        public bool IsPlaying => _isVideoPlaying;

        /// <summary>
        /// Whether video background is enabled.
        /// </summary>
        public bool UseVideoBackground => _useVideoBackground;

        /// <summary>
        /// Stop and cleanup all video resources.
        /// </summary>
        public void Cleanup()
        {
            _isVideoPlaying = false;

            if (_deferredVideoSetupCoroutine != null)
            {
                StopCoroutine(_deferredVideoSetupCoroutine);
                _deferredVideoSetupCoroutine = null;
            }

            if (_videoPlayerForward != null)
            {
                _videoPlayerForward.prepareCompleted -= OnForwardVideoPrepared;
                _videoPlayerForward.loopPointReached -= OnForwardVideoEnded;
                _videoPlayerForward.Stop();
                if (_videoPlayerForward.gameObject != gameObject)
                {
                    Destroy(_videoPlayerForward.gameObject);
                }
            }

            if (_videoPlayerReversed != null)
            {
                _videoPlayerReversed.prepareCompleted -= OnReversedVideoPrepared;
                _videoPlayerReversed.loopPointReached -= OnReversedVideoEnded;
                _videoPlayerReversed.Stop();
                if (_videoPlayerReversed.gameObject != gameObject)
                {
                    Destroy(_videoPlayerReversed.gameObject);
                }
            }

            if (_videoRenderTextureForward != null)
            {
                _videoRenderTextureForward.Release();
                Destroy(_videoRenderTextureForward);
            }
            if (_videoRenderTextureReversed != null)
            {
                _videoRenderTextureReversed.Release();
                Destroy(_videoRenderTextureReversed);
            }
        }

        /// <summary>
        /// Set destroyed flag to stop deferred setup.
        /// </summary>
        public void MarkDestroyed()
        {
            _isDestroyed = true;
        }

        // =============================================================================
        // INTERNAL
        // =============================================================================

        private void ApplyBackground()
        {
            if (_backgroundElement == null) return;

            if (_useVideoBackground)
            {
                if (!_videoSetupQueued)
                {
                    _videoSetupQueued = true;
                    _deferredVideoSetupCoroutine = StartCoroutine(SetupVideoBackgroundDeferred());
                }
                return;
            }

            // Fall back to static image
            if (_overrideBackgroundWithPortal && _backgroundPortalTexture == null)
            {
                _backgroundPortalTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_background_portal");
            }

            if (_overrideBackgroundWithPortal && _backgroundPortalTexture != null)
            {
                _backgroundElement.style.backgroundImage = new StyleBackground(_backgroundPortalTexture);
                _backgroundElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                _backgroundElement.style.unityBackgroundImageTintColor = Color.white;
            }

            if (_overrideBackgroundWithPortal)
            {
                _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, Mathf.Clamp01(_backgroundDarken));
            }
        }

        /// <summary>
        /// Fallback to static background when video fails.
        /// </summary>
        public void ApplyStaticFallback()
        {
            if (_overrideBackgroundWithPortal && _backgroundPortalTexture == null)
            {
                _backgroundPortalTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_background_portal");
            }

            if (_overrideBackgroundWithPortal && _backgroundPortalTexture != null)
            {
                _backgroundElement.style.backgroundImage = new StyleBackground(_backgroundPortalTexture);
                _backgroundElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                _backgroundElement.style.unityBackgroundImageTintColor = Color.white;
                _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, Mathf.Clamp01(_backgroundDarken));
            }
        }

        private IEnumerator SetupVideoBackgroundDeferred()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);

            _videoSetupQueued = false;
            _deferredVideoSetupCoroutine = null;

            if (_isDestroyed || _backgroundElement == null || !_useVideoBackground)
            {
                yield break;
            }

            SetupVideoBackground();
        }

        private void SetupVideoBackground()
        {
            _videoFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, "background_video.mp4");
            if (!System.IO.File.Exists(_videoFilePath))
            {
                _videoFilePath = System.IO.Path.Combine(Application.dataPath, "Art/UI/MainMenu/background_video.mp4");
            }
            bool hasForwardFile = System.IO.File.Exists(_videoFilePath);

            _videoFilePathReversed = System.IO.Path.Combine(Application.streamingAssetsPath, "background_video_reversed.mp4");
            if (!System.IO.File.Exists(_videoFilePathReversed))
            {
                _videoFilePathReversed = System.IO.Path.Combine(Application.dataPath, "Art/UI/MainMenu/background_video_reversed.mp4");
            }
            bool hasReversedFile = System.IO.File.Exists(_videoFilePathReversed);

            bool useVideoClipSource = !hasForwardFile && _backgroundVideoClip != null;
            _usePingPongLoop = !useVideoClipSource && hasForwardFile && hasReversedFile;

            if (!hasForwardFile && _backgroundVideoClip == null)
            {
                ErrorLogger.Warn($"[TitleScreenVideoBackground] Video file not found at: {_videoFilePath}");
                _useVideoBackground = false;
                ApplyStaticFallback();
                return;
            }

            if (!_usePingPongLoop)
            {
                ErrorLogger.Warn("[TitleScreenVideoBackground] Reversed background video missing; falling back to single-player loop.");
            }

            int rtWidth = Mathf.Max(Screen.width, 1920);
            int rtHeight = Mathf.Max(Screen.height, 1080);
            _videoRenderTextureForward = CreateVideoRenderTexture(rtWidth, rtHeight, "BackgroundVideoRT_Forward");
            if (_usePingPongLoop)
            {
                _videoRenderTextureReversed = CreateVideoRenderTexture(rtWidth, rtHeight, "BackgroundVideoRT_Reversed");
            }

            _videoPlayerForward = CreateVideoPlayer(
                _videoRenderTextureForward,
                useVideoClipSource ? null : _videoFilePath,
                useVideoClipSource ? _backgroundVideoClip : null);

            if (_usePingPongLoop)
            {
                _videoPlayerReversed = CreateVideoPlayer(_videoRenderTextureReversed, _videoFilePathReversed, null);
            }
            else
            {
                _videoPlayerReversed = null;
            }

            _videoPlayerForward.prepareCompleted += OnForwardVideoPrepared;
            _videoPlayerForward.Prepare();

            _backgroundElement.style.backgroundColor = Color.black;

            ErrorLogger.Log(_usePingPongLoop
                ? "[TitleScreenVideoBackground] Ping-pong dual video setup initiated (forward + reversed)."
                : "[TitleScreenVideoBackground] Single-player video setup initiated.");
        }

        private RenderTexture CreateVideoRenderTexture(int width, int height, string name)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.name = name;
            rt.filterMode = FilterMode.Bilinear;
            rt.antiAliasing = 1;
            rt.useMipMap = false;
            rt.Create();
            return rt;
        }

        private VideoPlayer CreateVideoPlayer(RenderTexture targetTexture, string videoUrl, VideoClip clip)
        {
            var playerObj = new GameObject(targetTexture.name + "_Player");
            playerObj.transform.SetParent(transform);
            var player = playerObj.AddComponent<VideoPlayer>();

            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = targetTexture;
            player.isLooping = false;
            player.skipOnDrop = false;
            player.playbackSpeed = 1.0f;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.aspectRatio = VideoAspectRatio.Stretch;

            if (clip != null)
            {
                player.source = VideoSource.VideoClip;
                player.clip = clip;
            }
            else
            {
                player.source = VideoSource.Url;
                player.url = videoUrl;
            }

            return player;
        }

        private void OnForwardVideoPrepared(VideoPlayer vp)
        {
            _videoLength = vp.length;
            _isVideoPlaying = true;

            ErrorLogger.Log($"[TitleScreenVideoBackground] Forward video prepared - Duration: {_videoLength:F2}s");

            if (_overrideBackgroundWithPortal)
            {
                if (_backgroundPortalTexture == null)
                    _backgroundPortalTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_background_portal");
                if (_backgroundPortalTexture != null)
                {
                    _backgroundElement.style.backgroundImage = new StyleBackground(_backgroundPortalTexture);
                    _backgroundElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    _backgroundElement.style.unityBackgroundImageTintColor = Color.white;
                    _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }
            }

            if (_videoOverlayElement == null)
            {
                _videoOverlayElement = new VisualElement();
                _videoOverlayElement.name = "video-overlay";
                _videoOverlayElement.pickingMode = PickingMode.Ignore;
                _videoOverlayElement.style.position = Position.Absolute;
                _videoOverlayElement.style.left = 0;
                _videoOverlayElement.style.top = 0;
                _videoOverlayElement.style.right = 0;
                _videoOverlayElement.style.bottom = 0;

                var parent = _backgroundElement.parent;
                if (parent != null)
                {
                    int bgIndex = parent.IndexOf(_backgroundElement);
                    parent.Insert(bgIndex + 1, _videoOverlayElement);
                }
            }

            _videoOverlayElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_videoRenderTextureForward));
            _videoOverlayElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

            vp.Play();

            if (_usePingPongLoop && _videoPlayerReversed != null)
            {
                SetupPingPongEvents();
                _videoPlayerReversed.prepareCompleted += OnReversedVideoPrepared;
                _videoPlayerReversed.Prepare();
            }
            else
            {
                vp.isLooping = true;
            }
        }

        private void OnReversedVideoPrepared(VideoPlayer vp)
        {
            ErrorLogger.Log("[TitleScreenVideoBackground] Reversed video prepared and standing by at frame 0");
            vp.time = 0;
            vp.Pause();
        }

        private void SetupPingPongEvents()
        {
            if (!_usePingPongLoop || _videoPlayerForward == null || _videoPlayerReversed == null) return;

            _videoPlayerForward.loopPointReached += OnForwardVideoEnded;
            _videoPlayerReversed.loopPointReached += OnReversedVideoEnded;
        }

        private void OnForwardVideoEnded(VideoPlayer vp)
        {
            if (!_isVideoPlaying) return;
            if (!_usePingPongLoop || _videoPlayerReversed == null || _videoRenderTextureReversed == null)
            {
                vp.time = 0;
                vp.Play();
                return;
            }

            var targetEl = _videoOverlayElement ?? _backgroundElement;
            targetEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_videoRenderTextureReversed));

            _videoPlayerReversed.Play();
            _videoPlayerForward.Pause();
            _videoPlayerForward.time = 0;
        }

        private void OnReversedVideoEnded(VideoPlayer vp)
        {
            if (!_isVideoPlaying) return;
            if (_videoPlayerForward == null || _videoRenderTextureForward == null)
            {
                return;
            }

            var targetEl = _videoOverlayElement ?? _backgroundElement;
            targetEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_videoRenderTextureForward));

            _videoPlayerForward.Play();
            _videoPlayerReversed.Pause();
            _videoPlayerReversed.time = 0;
        }
    }
}
