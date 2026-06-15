using System.Collections.Generic;
using UnityEngine;

namespace RealmCommander.Core
{
    public static class ArtAssetLookup
    {
        private const string IconRoot = "Art/Icons/";
        private const string TerrainRoot = "Art/Terrain/";

        private static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> terrainCache = new Dictionary<string, Texture2D>();

        public static Sprite LoadIcon(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId)) return null;
            string key = Normalize(assetId);
            if (iconCache.TryGetValue(key, out Sprite cached))
                return cached;
            Sprite sprite = Resources.Load<Sprite>(IconRoot + key);
            if (sprite != null)
                iconCache[key] = sprite;
            return sprite;
        }

        public static Sprite LoadUnitIcon(string unitIdOrName)
        {
            string id = Normalize(unitIdOrName);
            if (string.IsNullOrEmpty(id)) return null;
            return LoadIcon(id.StartsWith("unit_") ? id : "unit_" + id);
        }

        public static Texture2D LoadTerrainTexture(string terrainId)
        {
            string id = Normalize(terrainId);
            if (string.IsNullOrEmpty(id)) return null;
            if (terrainCache.TryGetValue(id, out Texture2D cached))
                return cached;
            Texture2D texture = Resources.Load<Texture2D>(TerrainRoot + (id.StartsWith("terrain_") ? id : "terrain_" + id));
            if (texture != null)
                terrainCache[id] = texture;
            return texture;
        }

        public static void ClearCache()
        {
            iconCache.Clear();
            terrainCache.Clear();
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }
    }
}
