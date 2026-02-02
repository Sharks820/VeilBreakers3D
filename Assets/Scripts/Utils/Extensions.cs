using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Utils
{
    /// <summary>
    /// Extension methods for common operations in VeilBreakers.
    /// </summary>
    public static class Extensions
    {
        // ============================================================
        // TRANSFORM EXTENSIONS
        // ============================================================

        /// <summary>
        /// Reset transform to default local position, rotation, and scale.
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Set the X position while keeping Y and Z.
        /// </summary>
        public static void SetPositionX(this Transform transform, float x)
        {
            var pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        /// <summary>
        /// Set the Y position while keeping X and Z.
        /// </summary>
        public static void SetPositionY(this Transform transform, float y)
        {
            var pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        /// <summary>
        /// Set the Z position while keeping X and Y.
        /// </summary>
        public static void SetPositionZ(this Transform transform, float z)
        {
            var pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }

        /// <summary>
        /// Destroy all children of this transform.
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Destroy all children immediately (for Editor use).
        /// </summary>
        public static void DestroyAllChildrenImmediate(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        // ============================================================
        // VECTOR EXTENSIONS
        // ============================================================

        /// <summary>
        /// Return a Vector3 with the Y component set to a new value.
        /// </summary>
        public static Vector3 WithY(this Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }

        /// <summary>
        /// Return a Vector3 with the X component set to a new value.
        /// </summary>
        public static Vector3 WithX(this Vector3 v, float x)
        {
            return new Vector3(x, v.y, v.z);
        }

        /// <summary>
        /// Return a Vector3 with the Z component set to a new value.
        /// </summary>
        public static Vector3 WithZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }

        /// <summary>
        /// Convert Vector3 to Vector2 (XY plane).
        /// </summary>
        public static Vector2 ToVector2XY(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        /// <summary>
        /// Convert Vector3 to Vector2 (XZ plane for top-down).
        /// </summary>
        public static Vector2 ToVector2XZ(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        /// <summary>
        /// Get a random point within a radius of this position.
        /// </summary>
        public static Vector3 RandomPointInRadius(this Vector3 center, float radius)
        {
            var randomDir = UnityEngine.Random.insideUnitSphere * radius;
            return center + randomDir;
        }

        /// <summary>
        /// Get a random point within a radius on the XZ plane.
        /// </summary>
        public static Vector3 RandomPointInRadiusXZ(this Vector3 center, float radius)
        {
            var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            var dist = UnityEngine.Random.Range(0f, radius);
            return new Vector3(
                center.x + Mathf.Cos(angle) * dist,
                center.y,
                center.z + Mathf.Sin(angle) * dist
            );
        }

        // ============================================================
        // GAMEOBJECT EXTENSIONS
        // ============================================================

        /// <summary>
        /// Get or add a component to a GameObject.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// Check if a GameObject has a specific component.
        /// </summary>
        public static bool HasComponent<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() != null;
        }

        /// <summary>
        /// Set the layer of this GameObject and all children.
        /// </summary>
        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        // ============================================================
        // COLLECTION EXTENSIONS
        // ============================================================

        /// <summary>
        /// Get a random element from a list.
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Shuffle a list in place using Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null || list.Count <= 1) return;

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Check if an index is valid for a list.
        /// </summary>
        public static bool IsValidIndex<T>(this IList<T> list, int index)
        {
            return list != null && index >= 0 && index < list.Count;
        }

        /// <summary>
        /// Try to get an element at index, returning default if invalid.
        /// </summary>
        public static T GetAtIndexOrDefault<T>(this IList<T> list, int index, T defaultValue = default)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;
            return list[index];
        }

        /// <summary>
        /// Find the index of an element matching a predicate.
        /// </summary>
        public static int FindIndex<T>(this IList<T> list, Predicate<T> match)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                    return i;
            }
            return -1;
        }

        // ============================================================
        // STRING EXTENSIONS
        // ============================================================

        /// <summary>
        /// Check if a string is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Check if a string is null, empty, or whitespace.
        /// </summary>
        public static bool IsNullOrWhitespace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Truncate a string to a maximum length with optional ellipsis.
        /// </summary>
        public static string Truncate(this string str, int maxLength, bool addEllipsis = true)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;

            if (addEllipsis && maxLength > 3)
                return str.Substring(0, maxLength - 3) + "...";

            return str.Substring(0, maxLength);
        }

        /// <summary>
        /// Convert a string to title case (first letter of each word uppercase).
        /// </summary>
        public static string ToTitleCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }

        // ============================================================
        // NUMBER EXTENSIONS
        // ============================================================

        /// <summary>
        /// Remap a value from one range to another.
        /// Returns toMin if fromMin equals fromMax (avoids division by zero).
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            // Guard against division by zero when source range is zero
            if (Mathf.Approximately(fromMax, fromMin))
                return toMin;

            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// Check if a float is approximately equal to another (within tolerance).
        /// </summary>
        public static bool Approximately(this float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        /// <summary>
        /// Clamp a value between 0 and 1.
        /// </summary>
        public static float Clamp01(this float value)
        {
            return Mathf.Clamp01(value);
        }

        /// <summary>
        /// Get the percentage (0-100) of a value relative to a max.
        /// </summary>
        public static float ToPercent(this int current, int max)
        {
            if (max <= 0) return 0f;
            return (current / (float)max) * 100f;
        }

        /// <summary>
        /// Get the ratio (0-1) of a value relative to a max.
        /// </summary>
        public static float ToRatio(this int current, int max)
        {
            if (max <= 0) return 0f;
            return current / (float)max;
        }

        // ============================================================
        // COLOR EXTENSIONS
        // ============================================================

        /// <summary>
        /// Return a color with modified alpha.
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Lighten a color by a percentage (0-1).
        /// </summary>
        public static Color Lighten(this Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        /// <summary>
        /// Darken a color by a percentage (0-1).
        /// </summary>
        public static Color Darken(this Color color, float amount)
        {
            return Color.Lerp(color, Color.black, amount);
        }

        // ============================================================
        // COMPONENT EXTENSIONS
        // ============================================================

        /// <summary>
        /// Enable or disable a component.
        /// </summary>
        public static void SetEnabled(this Behaviour behaviour, bool enabled)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }

        /// <summary>
        /// Toggle a component's enabled state.
        /// </summary>
        public static void ToggleEnabled(this Behaviour behaviour)
        {
            if (behaviour != null)
                behaviour.enabled = !behaviour.enabled;
        }

        // ============================================================
        // ANIMATOR EXTENSIONS
        // ============================================================

        /// <summary>
        /// Check if an animator has a parameter with the given name.
        /// </summary>
        public static bool HasParameter(this Animator animator, string paramName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (var param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Safely set a bool parameter if it exists.
        /// </summary>
        public static void SetBoolSafe(this Animator animator, string paramName, bool value)
        {
            if (animator.HasParameter(paramName))
                animator.SetBool(paramName, value);
        }

        /// <summary>
        /// Safely set a trigger if it exists.
        /// </summary>
        public static void SetTriggerSafe(this Animator animator, string paramName)
        {
            if (animator.HasParameter(paramName))
                animator.SetTrigger(paramName);
        }

        // ============================================================
        // RIGIDBODY EXTENSIONS
        // ============================================================

        /// <summary>
        /// Reset all velocity on a rigidbody.
        /// </summary>
        public static void ResetVelocity(this Rigidbody rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
