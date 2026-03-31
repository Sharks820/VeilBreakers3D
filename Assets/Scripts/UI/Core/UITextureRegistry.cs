using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Tracks runtime Texture2D allocations for leak-free cleanup.
    /// Register textures on creation, call DestroyAll() in OnDisable/Cleanup.
    /// </summary>
    public class UITextureRegistry
    {
        private readonly List<Texture2D> _textures = new List<Texture2D>();

        /// <summary>
        /// Register a texture for tracking. Returns the same texture for inline usage.
        /// Usage: element.style.backgroundImage = _registry.Register(UIGradientHelper.CreateVerticalGradient(...));
        /// </summary>
        public Texture2D Register(Texture2D tex)
        {
            if (tex != null) _textures.Add(tex);
            return tex;
        }

        /// <summary>
        /// Remove a specific texture from tracking (for ownership transfers).
        /// </summary>
        public void Unregister(Texture2D tex)
        {
            if (tex != null) _textures.Remove(tex);
        }

        /// <summary>
        /// Destroy all tracked textures and clear the list. Call in OnDisable or cleanup.
        /// </summary>
        public void DestroyAll()
        {
            for (int i = _textures.Count - 1; i >= 0; i--)
            {
                if (_textures[i] != null)
                    Object.Destroy(_textures[i]);
            }
            _textures.Clear();
        }

        /// <summary>Number of currently tracked textures.</summary>
        public int Count => _textures.Count;
    }
}
