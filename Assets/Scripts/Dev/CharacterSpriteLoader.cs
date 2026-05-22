using System.Collections.Generic;
using UnityEngine;

namespace Game.Dev
{
    // Loads pre-sliced sprite sheets from Resources/Characters/ at runtime.
    // Prefers Unity Editor-imported sliced sprites; falls back to loading Texture2D directly
    // and inferring frame splits by width, bypassing Editor preprocessing.
    public static class CharacterSpriteLoader
    {
        static readonly Dictionary<string, Sprite[]> _cache = new Dictionary<string, Sprite[]>();

        // Returns all sliced sprites in the sheet (for frame animation).
        public static Sprite[] GetAll(string resourcesSubPath)
        {
            if (_cache.TryGetValue(resourcesSubPath, out var cached)) return cached;
            var sprites = Resources.LoadAll<Sprite>($"Characters/{resourcesSubPath}/sprite_sheet");
            if (sprites == null || sprites.Length == 0)
                sprites = LoadFromTexture($"Characters/{resourcesSubPath}/sprite_sheet");
            _cache[resourcesSubPath] = sprites;
            return sprites;
        }

        // Returns the first sprite (default front idle frame, for static display).
        public static Sprite GetIdle(string resourcesSubPath)
        {
            var all = GetAll(resourcesSubPath);
            return all != null && all.Length > 0 ? all[0] : null;
        }

        // Loads a sprite from any Resources sub-path (not limited to Characters/).
        // fullPath example: Weapons/Daggers/IronDagger
        public static Sprite GetIdleAt(string fullPath)
        {
            var key = "\x01" + fullPath;
            if (_cache.TryGetValue(key, out var cached))
                return cached != null && cached.Length > 0 ? cached[0] : null;
            var sprites = Resources.LoadAll<Sprite>($"{fullPath}/sprite_sheet");
            if (sprites == null || sprites.Length == 0)
                sprites = LoadFromTexture($"{fullPath}/sprite_sheet");
            _cache[key] = sprites;
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        public static void ClearCache() => _cache.Clear();

        // Fallback: load PNG as Texture2D and slice manually, bypassing the Editor import pipeline.
        // Sprite.Create() does not require a readable texture; GPU renders it normally.
        static Sprite[] LoadFromTexture(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point;
            int w = tex.width, h = tex.height;
            int frameW = GuessFrameWidth(w, h);
            int n = Mathf.Max(1, w / frameW);
            var result = new Sprite[n];
            for (int i = 0; i < n; i++)
                result[i] = Sprite.Create(tex,
                    new Rect(i * frameW, 0, frameW, h),
                    new Vector2(0.5f, 0.5f), 32f);
            return result;
        }

        // Infers frame width: tries n=1..4, picks the split whose aspect ratio is closest to 0.85.
        static int GuessFrameWidth(int w, int h)
        {
            float bestScore = float.MaxValue;
            int bestN = 1;
            for (int n = 1; n <= 4; n++)
            {
                if (w % n != 0) continue;
                float ratio = (float)h * n / w; // = h / (w/n)
                float score = Mathf.Abs(ratio - 0.85f) + (n - 1) * 0.1f;
                if (score < bestScore) { bestScore = score; bestN = n; }
            }
            return w / bestN;
        }
    }
}
