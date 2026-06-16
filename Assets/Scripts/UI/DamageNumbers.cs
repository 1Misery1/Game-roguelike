using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// Floating damage numbers — a pool of Text on an overlay canvas, recycled when each one finishes (no per-frame allocation).
    public class DamageNumbers : MonoBehaviour
    {
        private const float Lifetime = 1.2f;
        private const float Rise     = 1.8f;

        public static DamageNumbers Instance { get; private set; }

        private class Entry
        {
            public Vector3 worldPos;
            public float   spawnTime;
            public Text    text;
            public Color   baseColor;
        }

        private readonly List<Entry> _active = new List<Entry>();
        private readonly Stack<Text> _pool   = new Stack<Text>();

        private Canvas    _canvas;
        private Transform _root;

        private void Awake()  { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Show(Vector3 worldPos, float amount, bool crit, bool heal = false)
        {
            EnsureUI();

            Color c = heal ? new Color(0.3f, 1f, 0.45f)
                   : crit ? new Color(1f, 0.95f, 0.1f)
                           : new Color(1f, 0.5f, 0.3f);
            string txt   = crit ? $"★{amount:0}!" : $"{amount:0}";
            bool   large = crit || heal;

            var t = _pool.Count > 0 ? _pool.Pop() : NewText();
            t.text      = txt;
            t.fontSize  = large ? 20 : 15;
            t.color     = c;
            t.enabled   = true;
            t.gameObject.SetActive(true);

            _active.Add(new Entry
            {
                worldPos  = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, 0f),
                spawnTime = Time.time,
                text      = t,
                baseColor = c,
            });
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            float now = Time.time;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var e   = _active[i];
                float age = now - e.spawnTime;
                if (age >= Lifetime || cam == null) { Recycle(e); _active.RemoveAt(i); continue; }

                float t      = age / Lifetime;
                var   risen  = e.worldPos + Vector3.up * (Rise * age);
                var   screen = cam.WorldToScreenPoint(risen);
                if (screen.z < 0) { e.text.enabled = false; continue; }

                e.text.enabled = true;
                e.text.rectTransform.position = new Vector3(screen.x, screen.y, 0f);

                var c = e.baseColor;
                c.a = Mathf.Clamp01(1f - t * 1.15f);
                e.text.color = c;
            }
        }

        // ── Object pool / UI ──────────────────────────────────────────────────
        private void EnsureUI()
        {
            if (_canvas != null) return;
            _canvas = UIFactory.CreateOverlayCanvas("DamageNumbersCanvas", sortingOrder: 400);
            _canvas.transform.SetParent(transform, false);
            _root = _canvas.transform;
        }

        private Text NewText()
        {
            var t = UIFactory.Label("Dmg", _root, "", 15, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.sizeDelta = new Vector2(120f, 40f);
            return t;
        }

        private void Recycle(Entry e)
        {
            e.text.enabled = false;
            e.text.gameObject.SetActive(false);
            _pool.Push(e.text);
        }
    }
}
