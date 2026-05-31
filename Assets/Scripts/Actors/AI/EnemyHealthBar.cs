using Game.Combat;
using UnityEngine;

namespace Game.AI
{
    /// Renders a small health bar above each enemy sprite.
    /// Two child SpriteRenderers: dark background + color-coded fill.
    /// Bar hides automatically at full HP to reduce visual clutter.
    public class EnemyHealthBar : MonoBehaviour
    {
        const float BarW    = 0.90f;   // width in local units
        const float BarH    = 0.10f;   // total bar height
        const float FillH   = 0.065f;  // inner fill height
        const float YOffset = 1.30f;   // above sprite center (sprite top ≈ +1 wu)
        const int   Sort    = 20;      // render above all enemy sprites

        Health         _health;
        GameObject     _root;
        SpriteRenderer _fillSR;
        Transform      _fillTr;

        void Awake()
        {
            _health = GetComponent<Health>();
            BuildBar();
        }

        void BuildBar()
        {
            _root = new GameObject("_HealthBar");
            _root.transform.SetParent(transform, false);

            // Background strip
            Spawn("BG", _root.transform,
                new Vector3(0f, YOffset, 0f),
                new Vector3(BarW, BarH, 1f),
                new Color(0.08f, 0.08f, 0.08f, 0.88f), Sort);

            // Fill strip
            var fillGO = Spawn("Fill", _root.transform,
                new Vector3(0f, YOffset, 0f),
                new Vector3(BarW, FillH, 1f),
                new Color(0.15f, 0.90f, 0.25f), Sort + 1);

            _fillSR = fillGO.GetComponent<SpriteRenderer>();
            _fillTr = fillGO.transform;

            // Start hidden (full HP)
            _root.SetActive(false);
        }

        void LateUpdate()
        {
            if (_health == null) return;
            float ratio = Mathf.Clamp01(_health.Ratio);

            bool show = ratio < 0.999f;
            _root.SetActive(show);
            if (!show) return;

            // Fill slides from the left edge
            float fillW   = Mathf.Max(0f, BarW * ratio);
            float offsetX = -BarW * 0.5f + fillW * 0.5f;
            _fillTr.localPosition = new Vector3(offsetX, YOffset, 0f);
            _fillTr.localScale    = new Vector3(fillW, FillH, 1f);

            // Color: green (full) → yellow (half) → red (low)
            _fillSR.color = ratio > 0.5f
                ? Color.Lerp(new Color(1.0f, 0.85f, 0.10f), new Color(0.15f, 0.90f, 0.25f), (ratio - 0.5f) * 2f)
                : Color.Lerp(new Color(0.9f, 0.12f, 0.08f), new Color(1.0f, 0.85f, 0.10f), ratio * 2f);
        }

        static GameObject Spawn(string name, Transform parent, Vector3 localPos, Vector3 localScale, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = WhitePixel();
            sr.color        = color;
            sr.sortingOrder = sortOrder;
            return go;
        }

        static Sprite _pixel;
        static Sprite WhitePixel()
        {
            if (_pixel != null) return _pixel;
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _pixel;
        }
    }
}
