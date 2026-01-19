using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// ScriptableObject defining a save shrine location.
    /// Shrines are discovered through exploration and enable manual saving within their radius.
    /// </summary>
    [CreateAssetMenu(fileName = "Shrine", menuName = "VeilBreakers/Shrine Data")]
    public class ShrineData : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this shrine (used in save data)")]
        public string shrineId;

        [Tooltip("Display name shown to player")]
        public string shrineName;

        [Tooltip("Area/region this shrine belongs to")]
        public string areaName;

        [Header("Location")]
        [Tooltip("World position of the shrine")]
        public Vector3 shrinePosition;

        [Tooltip("Radius within which player can save (in world units)")]
        [Range(1f, 100f)]
        public float saveRadius = 20f;

        [Header("Visuals")]
        [Tooltip("Icon shown on map when discovered")]
        public Sprite mapIcon;

        [Tooltip("Prefab to spawn at shrine location (optional)")]
        public GameObject shrinePrefab;

        [Header("Requirements")]
        [Tooltip("Quest ID required to activate this shrine (empty = always active)")]
        public string requiredQuestId;

        [Tooltip("Story flag required to activate (empty = always active)")]
        public string requiredStoryFlag;

        /// <summary>
        /// Checks if a position is within this shrine's save zone.
        /// </summary>
        public bool IsPositionInRange(Vector3 position)
        {
            return Vector3.Distance(position, shrinePosition) <= saveRadius;
        }

        /// <summary>
        /// Gets the distance from a position to this shrine.
        /// </summary>
        public float GetDistanceFrom(Vector3 position)
        {
            return Vector3.Distance(position, shrinePosition);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate ID from name if empty
            if (string.IsNullOrEmpty(shrineId) && !string.IsNullOrEmpty(shrineName))
            {
                shrineId = shrineName.ToLower().Replace(" ", "_");
            }
        }
#endif
    }
}
