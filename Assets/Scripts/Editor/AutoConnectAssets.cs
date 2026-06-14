using UnityEngine;
using UnityEditor;
using RealmCommander.Audio;

namespace RealmCommander.Editor
{
    public static class AutoConnectAssets
    {
        [MenuItem("Tools/Realm Commander/Auto-Connect VFX & Audio")]
        public static void ConnectAll()
        {
            ConnectAudioManager();
            Debug.Log("[AutoConnect] Audio connections completed.");
        }

        private static void ConnectAudioManager()
        {
            var manager = Object.FindAnyObjectByType<AudioManager>();
            if (manager == null)
            {
                Debug.Log("[AutoConnect] AudioManager not found in scene.");
                return;
            }

            var so = new SerializedObject(manager);
            ConnectAudioClip(so, "unitSelectClip", "UnitSelect");
            ConnectAudioClip(so, "unitAttackClip", "UnitAttack");
            ConnectAudioClip(so, "unitMoveClip", "UnitMove");
            ConnectAudioClip(so, "unitSpawnClip", "UnitSpawn");
            ConnectAudioClip(so, "buildingCompleteClip", "BuildingComplete");
            ConnectAudioClip(so, "skillCastClip", "SkillCast");
            ConnectAudioClip(so, "victoryClip", "Victory");
            ConnectAudioClip(so, "defeatClip", "Defeat");
            so.ApplyModifiedProperties();
            Debug.Log("[AutoConnect] AudioManager clips connected.");
        }

        private static void ConnectAudioClip(SerializedObject so, string fieldName, string clipName)
        {
            string path = $"Assets/Audio/{clipName}.clip";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) return;

            var prop = so.FindProperty(fieldName);
            if (prop != null && prop.objectReferenceValue == null)
                prop.objectReferenceValue = clip;
        }
    }
}
