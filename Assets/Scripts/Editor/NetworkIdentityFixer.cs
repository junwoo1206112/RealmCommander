using UnityEngine;
using UnityEditor;
using Mirror;
using System.Collections.Generic;

namespace RealmCommander.Editor
{
    public class NetworkIdentityFixer
    {
        [MenuItem("Tools/Realm Commander/Fix NetworkIdentity (Enhanced)")]
        public static void FixAllNetworkIdentities()
        {
            int fixedCount = 0;
            var fixedObjects = new List<string>();

            // 씬의 모든 GameObject 검색 (비활성 포함)
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (var obj in allObjects)
            {
                // 씬에 있는 오브젝트만 처리 (프리팹 제외)
                if (!obj.scene.IsValid()) continue;

                // NetworkBehaviour 컴포넌트 직접 검색
                var networkBehaviours = obj.GetComponents<NetworkBehaviour>();

                if (networkBehaviours.Length > 0 && obj.GetComponent<NetworkIdentity>() == null)
                {
                    Undo.RecordObject(obj, "Add NetworkIdentity");
                    obj.AddComponent<NetworkIdentity>();
                    fixedCount++;
                    fixedObjects.Add(obj.name);
                    Debug.Log($"[FIX] Added NetworkIdentity to: {obj.name}");
                }
            }

            string message = fixedCount > 0
                ? $"{fixedCount}개의 GameObject에 NetworkIdentity를 추가했습니다:\n\n{string.Join("\n", fixedObjects)}"
                : "NetworkIdentity가 필요한 오브젝트를 찾지 못했습니다.";

            EditorUtility.DisplayDialog("NetworkIdentity Fix Complete", message, "확인");
        }

        [MenuItem("Tools/Realm Commander/Validate Network Setup")]
        public static void ValidateNetworkSetup()
        {
            var issues = new List<string>();

            // NetworkManager 확인
            var nm = Object.FindFirstObjectByType<NetworkManager>();
            if (nm == null)
            {
                issues.Add(" NetworkManager가 씬에 없습니다.");
            }
            else
            {
                if (nm.GetComponent<NetworkIdentity>() == null)
                {
                    issues.Add("❌ NetworkManager에 NetworkIdentity가 없습니다.");
                }
                else
                {
                    issues.Add("✅ NetworkManager 설정 OK");
                }
            }

            // 모든 NetworkBehaviour 확인
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int checkedCount = 0;
            int okCount = 0;

            foreach (var obj in allObjects)
            {
                if (!obj.scene.IsValid()) continue;

                var networkBehaviours = obj.GetComponents<NetworkBehaviour>();
                if (networkBehaviours.Length > 0)
                {
                    checkedCount++;
                    if (obj.GetComponent<NetworkIdentity>() != null)
                    {
                        okCount++;
                    }
                    else
                    {
                        issues.Add($"❌ {obj.name}: NetworkBehaviour 있음 but NetworkIdentity 없음");
                    }
                }
            }

            issues.Add($"\n📊 통계: {checkedCount}개 확인, {okCount}개 OK, {checkedCount - okCount}개 문제");

            string message = string.Join("\n", issues);
            EditorUtility.DisplayDialog("Network Validation", message, "확인");
        }
    }
}
