using System.IO;
using UnityEngine;
using UnityEditor;

namespace RealmCommander.Editor
{
    public static class AudioPlaceholderGenerator
    {
        private const string AudioFolder = "Assets/Audio";
        private const int SampleRate = 44100;
        private const int DurationSamples = 44100;

        [MenuItem("Tools/Realm Commander/Generate Audio Placeholders")]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder(AudioFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }

            CreatePlaceholder("UnitSelect");
            CreatePlaceholder("UnitAttack");
            CreatePlaceholder("UnitMove");
            CreatePlaceholder("UnitSpawn");
            CreatePlaceholder("BuildingComplete");
            CreatePlaceholder("SkillCast");
            CreatePlaceholder("Victory");
            CreatePlaceholder("Defeat");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Audio] Placeholder clips generated. Replace with real audio in Inspector.");
        }

        private static void CreatePlaceholder(string name)
        {
            string assetPath = $"{AudioFolder}/{name}.clip";

            if (AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath) != null)
            {
                Debug.Log($"[Audio] {name} already exists, skipping.");
                return;
            }

            string fullPath = Path.GetFullPath(assetPath.Replace(".clip", ".wav"));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            WriteSilentWav(fullPath);
            AssetDatabase.ImportAsset(assetPath.Replace(".clip", ".wav"));
            Debug.Log($"[Audio] Created placeholder: {assetPath.Replace(".clip", ".wav")}");
        }

        private static void WriteSilentWav(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = SampleRate * 1 * 16 / 8;
                int blockAlign = 1 * 16 / 8;
                int dataSize = DurationSamples * blockAlign;

                bw.Write(0x46464952);
                bw.Write(36 + dataSize);
                bw.Write(0x45564157);
                bw.Write(0x20746D66);
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(SampleRate);
                bw.Write(byteRate);
                bw.Write((short)blockAlign);
                bw.Write((short)16);
                bw.Write(0x61746164);
                bw.Write(dataSize);

                for (int i = 0; i < DurationSamples; i++)
                    bw.Write((short)0);
            }
        }
    }
}
