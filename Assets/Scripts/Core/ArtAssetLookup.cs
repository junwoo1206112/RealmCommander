using UnityEngine;

namespace RealmCommander.Core
{
    public static class ArtAssetLookup
    {
        private const string IconRoot = "Art/Icons/";
        private const string TerrainRoot = "Art/Terrain/";

        public static Sprite LoadIcon(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId)) return null;
            return Resources.Load<Sprite>(IconRoot + Normalize(assetId));
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
            return Resources.Load<Texture2D>(TerrainRoot + (id.StartsWith("terrain_") ? id : "terrain_" + id));
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }
    }
}
