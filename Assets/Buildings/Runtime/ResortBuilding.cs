using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParadiseTycoon.Buildings
{
    [Serializable]
    public sealed class BuildingLevel
    {
        [SerializeField, Min(0)] int cost;
        [SerializeField, Min(1)] int capacity;
        [SerializeField, Min(0.1f)] float secondsPerRequest;
        [SerializeField] Sprite sprite;

        public int Cost => cost;
        public int Capacity => capacity;
        public float SecondsPerRequest => secondsPerRequest;
        public Sprite Sprite => sprite;
    }

    public sealed class ResortBuilding : MonoBehaviour
    {
        [SerializeField] string buildingId;
        [SerializeField] string displayName;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] SpriteRenderer construction;
        [SerializeField] BuildingLevel[] levels;

        Vector3 originalScale;
        Vector3 originalPosition;
        Bounds originalBounds;
        SpriteRenderer visibleRenderer;
        Vector2[] vertices;
        ushort[] triangles;

        public string BuildingId => buildingId;
        public string DisplayName => displayName;
        public int InitialLevel { get; private set; }
        public int MaxLevel => levels.Length;
        public int Level { get; private set; }
        public BuildingLevel CurrentService => GetLevel(Level);
        public SortingGroup SortingGroup { get; private set; }

        void Awake()
        {
            SortingGroup = GetComponent<SortingGroup>();
            originalScale = visual.transform.localScale;
            originalPosition = visual.transform.localPosition;
            originalBounds = visual.sprite.bounds;
            InitialLevel = visual.gameObject.activeSelf ? 1 : 0;
            ApplyLevel(InitialLevel);
        }

        public BuildingLevel GetLevel(int level) => level > 0 && level <= MaxLevel ? levels[level - 1] : null;

        internal void ApplyLevel(int level)
        {
            if (visibleRenderer != null && level == Level) return;

            Level = level;
            bool built = level > 0;
            visual.gameObject.SetActive(built);
            if (construction != null) construction.gameObject.SetActive(!built);
            if (built)
            {
                visual.sprite = GetLevel(level).Sprite;
                // Match the original footprint and keep the sprite's lower edge at the same height.
                Bounds bounds = visual.sprite.bounds;
                float ratio = originalBounds.size.x / bounds.size.x;
                Vector3 scale = originalScale * ratio;
                float flip = visual.flipX ? -1f : 1f;
                Vector3 offset = new Vector3(
                    flip * (originalBounds.center.x * originalScale.x - bounds.center.x * scale.x),
                    originalBounds.min.y * originalScale.y - bounds.min.y * scale.y,
                    0f);
                visual.transform.localScale = scale;
                visual.transform.localPosition = originalPosition + offset;
            }

            visibleRenderer = built ? visual : construction;
            if (visibleRenderer == null) return;
            vertices = visibleRenderer.sprite.vertices;
            triangles = visibleRenderer.sprite.triangles;
        }

        internal bool Contains(Vector3 worldPoint)
        {
            if (visibleRenderer == null || !visibleRenderer.enabled || !visibleRenderer.gameObject.activeInHierarchy) return false;

            Bounds bounds = visibleRenderer.bounds;
            if (worldPoint.x < bounds.min.x || worldPoint.x > bounds.max.x ||
                worldPoint.y < bounds.min.y || worldPoint.y > bounds.max.y)
                return false;

            Vector2 localPoint = visibleRenderer.transform.InverseTransformPoint(worldPoint);
            if (visibleRenderer.flipX) localPoint.x = -localPoint.x;
            if (visibleRenderer.flipY) localPoint.y = -localPoint.y;

            for (int index = 0; index < triangles.Length; index += 3)
            {
                Vector2 a = vertices[triangles[index]];
                Vector2 b = vertices[triangles[index + 1]];
                Vector2 c = vertices[triangles[index + 2]];
                // Imported sprite meshes can contain degenerate triangles at transparent edges.
                if (Mathf.Abs(Cross(b - a, c - a)) < 0.000001f) continue;
                float ab = Cross(b - a, localPoint - a);
                float bc = Cross(c - b, localPoint - b);
                float ca = Cross(a - c, localPoint - c);
                if ((ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f))
                    return true;
            }

            return false;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
