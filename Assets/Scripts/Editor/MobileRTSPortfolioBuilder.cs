using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RealmCommander.RTS;
using RealmCommander.UI;

namespace RealmCommander.Editor
{
    public static class MobileRTSPortfolioBuilder
    {
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Tools/Realm Commander/Apply Mobile RTS Portfolio Upgrade")]
        public static void ApplyMobileRTSPortfolioUpgrade()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            Camera mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (mainCamera == null)
            {
                throw new MissingReferenceException("MainScene에 Camera가 없습니다.");
            }

            if (mainCamera.GetComponent<MobileRTSCameraController>() == null)
            {
                mainCamera.gameObject.AddComponent<MobileRTSCameraController>();
            }

            MobileRTSInput mobileInput = Object.FindFirstObjectByType<MobileRTSInput>();
            if (mobileInput == null)
            {
                var inputObject = new GameObject("MobileRTSInput");
                mobileInput = inputObject.AddComponent<MobileRTSInput>();
            }

            var inputSerializedObject = new SerializedObject(mobileInput);
            inputSerializedObject.FindProperty("mainCamera").objectReferenceValue = mainCamera;
            inputSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            foreach (CanvasScaler scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                if (scaler.GetComponent<MobileSafeArea>() == null)
                {
                    scaler.gameObject.AddComponent<MobileSafeArea>();
                }
            }

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MobileRTSPortfolioBuilder] Mobile RTS input, camera, and responsive UGUI applied.");
        }

        public static void VerifyMobileRTSPortfolioUpgrade()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            Camera mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (mainCamera == null || mainCamera.GetComponent<MobileRTSCameraController>() == null)
                throw new MissingComponentException("Main Camera에 MobileRTSCameraController가 없습니다.");
            if (Object.FindFirstObjectByType<MobileRTSInput>() == null)
                throw new MissingComponentException("MainScene에 MobileRTSInput이 없습니다.");

            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            if (scalers.Length == 0) throw new MissingComponentException("MainScene에 CanvasScaler가 없습니다.");
            foreach (CanvasScaler scaler in scalers)
            {
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    throw new UnityException($"{scaler.name} CanvasScaler가 화면 크기 대응 모드가 아닙니다.");
                if (scaler.GetComponent<MobileSafeArea>() == null)
                    throw new MissingComponentException($"{scaler.name}에 MobileSafeArea가 없습니다.");
            }

            Debug.Log($"[MobileRTSPortfolioBuilder] Validation passed. Canvas count: {scalers.Length}");
        }
    }
}
