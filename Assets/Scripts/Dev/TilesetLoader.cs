using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Dev
{
    // Loads Kenney Tiny Dungeon tileset (tilemap_packed.png) from the local file system
    // at runtime, bypassing Unity's import pipeline. Tile indices are 0-based,
    // reading left-to-right top-to-bottom in the 12×11 sprite grid.
    public static class TilesetLoader
    {
        const int   COLS = 12;
        const int   SZ   = 16;
        const float PPU  = 16f;

        static Texture2D _sheet;
        static readonly Dictionary<int, Sprite> _cache = new Dictionary<int, Sprite>();

        static Texture2D Sheet
        {
            get
            {
                if (_sheet != null) return _sheet;
                var path = Path.Combine(Application.dataPath,
                    "Resources/Tilesets/tiny_dungeon/Tilemap/tilemap_packed.png");
                if (!File.Exists(path)) return null;
                _sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode   = TextureWrapMode.Clamp
                };
                _sheet.LoadImage(File.ReadAllBytes(path));
                return _sheet;
            }
        }

        public static bool IsAvailable => Sheet != null;

        // Returns a sprite for the tile at the given 0-based index.
        // Layout: 12 tiles wide × 11 rows tall (CC0, Kenney Tiny Dungeon).
        // Confirmed tile groups from TMX analysis:
        //   indices  0-11  : top wall row
        //   indices 12-23  : wall side/mid row
        //   indices 36-47  : wall-to-floor transition
        //   indices 48-59  : stone floor tiles (most common in open areas)
        //   indices 60-71  : props row A (torches, bones, items)
        //   indices 72-83  : props row B (skulls, barrels, chests)
        //   indices 84-95  : props row C (doors, crates, deco)
        public static Sprite Get(int index)
        {
            if (index < 0) return null;
            if (_cache.TryGetValue(index, out var s)) return s;
            var tex = Sheet;
            if (tex == null) return null;
            int col = index % COLS;
            int row = index / COLS;
            int py  = (tex.height / SZ - 1 - row) * SZ;  // flip Y: Unity origin = bottom-left
            var spr = Sprite.Create(tex,
                          new Rect(col * SZ, py, SZ, SZ),
                          new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
            _cache[index] = spr;
            return spr;
        }
    }
}
