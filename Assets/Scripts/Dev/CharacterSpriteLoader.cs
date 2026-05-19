using System.Collections.Generic;
using UnityEngine;

namespace Game.Dev
{
    // 运行时加载 Resources/Characters/ 下已切割的精灵表。
    // 须先在 Unity Editor 执行 Tools > Game > Setup Character Sprites，
    // 才能在此处取到真实精灵；否则返回 null（各系统自动回退到程序化生成）。
    public static class CharacterSpriteLoader
    {
        static readonly Dictionary<string, Sprite[]> _cache = new Dictionary<string, Sprite[]>();

        // 返回精灵表内全部切割后的精灵（供逐帧动画使用）
        public static Sprite[] GetAll(string resourcesSubPath)
        {
            if (_cache.TryGetValue(resourcesSubPath, out var cached)) return cached;
            var sprites = Resources.LoadAll<Sprite>($"Characters/{resourcesSubPath}/sprite_sheet");
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
            var key = "\x01" + fullPath; // 避免与 Characters/ 路径的 key 碰撞
            if (_cache.TryGetValue(key, out var cached))
                return cached != null && cached.Length > 0 ? cached[0] : null;
            var sprites = Resources.LoadAll<Sprite>($"{fullPath}/sprite_sheet");
            _cache[key] = sprites;
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        public static void ClearCache() => _cache.Clear();
    }
}
