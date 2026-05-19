using System.Collections.Generic;
using UnityEngine;

namespace Game.Dev
{
    // 运行时加载 Resources/Characters/ 下已切割的精灵表。
    // 优先使用 Unity Editor 导入的切割精灵；若尚未导入则直接加载 Texture2D 并
    // 按帧宽推断分割，无需 Editor 预处理即可显示真实像素图。
    public static class CharacterSpriteLoader
    {
        static readonly Dictionary<string, Sprite[]> _cache = new Dictionary<string, Sprite[]>();

        // 返回精灵表内全部切割后的精灵（供逐帧动画使用）
        public static Sprite[] GetAll(string resourcesSubPath)
        {
            if (_cache.TryGetValue(resourcesSubPath, out var cached)) return cached;
            var sprites = Resources.LoadAll<Sprite>($"Characters/{resourcesSubPath}/sprite_sheet");
            if (sprites == null || sprites.Length == 0)
                sprites = LoadFromTexture($"Characters/{resourcesSubPath}/sprite_sheet");
            _cache[resourcesSubPath] = sprites;
            return sprites;
        }

        // 返回第一张精灵（默认正面站立帧，用于静态显示）
        public static Sprite GetIdle(string resourcesSubPath)
        {
            var all = GetAll(resourcesSubPath);
            return all != null && all.Length > 0 ? all[0] : null;
        }

        // 加载任意 Resources 子路径下的精灵（不限于 Characters/）
        // fullPath 例：Weapons/Daggers/IronDagger
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

        // 备用路径：直接把 PNG 当 Texture2D 加载再手动切割，绕过 Editor 导入管线。
        // Sprite.Create() 不要求贴图可读，GPU 可正常渲染。
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

        // 推断每帧宽度：尝试 n=1..4，取宽高比最接近 0.85 的分法（略高于宽优先）。
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
