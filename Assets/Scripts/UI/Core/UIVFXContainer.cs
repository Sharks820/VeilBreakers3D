using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Manages named VFX layers with predictable z-ordering.
    /// Provides named layer lookup and bottom-to-top ordering,
    /// replacing fragile root.Insert(0, ...) and root.Add(...) calls
    /// in VFX subsystems.
    /// </summary>
    public class UIVFXContainer
    {
        private readonly VisualElement _host;
        private readonly Dictionary<string, VisualElement> _layers = new Dictionary<string, VisualElement>();
        private readonly List<string> _layerOrder = new List<string>();

        /// <summary>
        /// Create a UIVFXContainer that manages layers inside the given host element.
        /// </summary>
        public UIVFXContainer(VisualElement host)
        {
            _host = host;
        }

        /// <summary>
        /// Get an existing layer by name, or create a new one.
        /// New layers are inserted at the correct z-position based on _layerOrder.
        /// If layerName is not in _layerOrder, it is appended to the end.
        /// </summary>
        public VisualElement GetOrCreateLayer(string layerName)
        {
            if (_layers.TryGetValue(layerName, out var existing))
                return existing;

            var layer = new VisualElement();
            layer.name = $"vfx-layer-{layerName}";
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.style.overflow = Overflow.Hidden;
            layer.pickingMode = PickingMode.Ignore;

            // Determine insert index based on layer order
            int orderIndex = _layerOrder.IndexOf(layerName);
            if (orderIndex < 0)
            {
                // Not in predefined order -- append to end
                _host.Add(layer);
                _layerOrder.Add(layerName);
            }
            else
            {
                // Find the correct insertion point among existing layers
                int insertIndex = 0;
                for (int i = 0; i < orderIndex; i++)
                {
                    if (_layers.ContainsKey(_layerOrder[i]))
                        insertIndex++;
                }

                if (insertIndex >= _host.childCount)
                    _host.Add(layer);
                else
                    _host.Insert(insertIndex, layer);
            }

            _layers[layerName] = layer;
            return layer;
        }

        /// <summary>
        /// Define the bottom-to-top z-order for layers.
        /// Reorders existing layers to match the specified order.
        /// Layers not in the list remain at the end in their current order.
        /// </summary>
        public void SetLayerOrder(params string[] layerNames)
        {
            _layerOrder.Clear();

            foreach (var name in layerNames)
            {
                if (!_layerOrder.Contains(name))
                    _layerOrder.Add(name);
            }

            // Reorder existing layers to match the new order
            ReorderLayers();
        }

        /// <summary>
        /// Remove a named layer from the host and internal tracking.
        /// </summary>
        public void RemoveLayer(string layerName)
        {
            if (!_layers.TryGetValue(layerName, out var layer))
                return;

            layer.RemoveFromHierarchy();
            _layers.Remove(layerName);
            _layerOrder.Remove(layerName);
        }

        /// <summary>
        /// Remove all layers from the host and clear internal state.
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _layers)
            {
                kvp.Value.RemoveFromHierarchy();
            }
            _layers.Clear();
            _layerOrder.Clear();
        }

        /// <summary>
        /// Check if a layer with the given name exists.
        /// </summary>
        public bool HasLayer(string layerName) => _layers.ContainsKey(layerName);

        /// <summary>
        /// Get a layer by name, returning null if it doesn't exist.
        /// </summary>
        public VisualElement GetLayer(string layerName)
        {
            _layers.TryGetValue(layerName, out var layer);
            return layer;
        }

        /// <summary>
        /// Number of currently managed layers.
        /// </summary>
        public int LayerCount => _layers.Count;

        // =========================================================================
        // INTERNAL
        // =========================================================================

        private void ReorderLayers()
        {
            // Remove all layers from host temporarily
            foreach (var kvp in _layers)
            {
                kvp.Value.RemoveFromHierarchy();
            }

            // Re-add in defined order
            foreach (var name in _layerOrder)
            {
                if (_layers.TryGetValue(name, out var layer))
                {
                    _host.Add(layer);
                }
            }
        }
    }
}
