using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using RealmCommander.RTS;

namespace RealmCommander.Editor
{
    public class ProjectSetup : EditorWindow
    {
        [MenuItem("Tools/Realm Commander/Setup Project")]
        public static void SetupProject()
        {
            if (!EditorUtility.DisplayDialog("프로젝트 세팅", 
                "Realm Commander 프로젝트를 자동 세팅하시겠습니까?\n\n" +
                "1. 지형 생성 및 NavMesh 베이크\n" +
                "2. 게임 매니저 배치\n" +
                "3. 유닛 Prefab 생성\n" +
                "4. 카메라 세팅\n" +
                "5. UI Canvas 구성", 
                "세팅", "취소"))
            {
                return;
            }

            CreateTerrain();
            CreateManagers();
            CreateUnitPrefab();
            CreateBuildingPrefabs();
            CreateCamera();
            CreateUI();
            SpawnUnits();
            SpawnBuildings();
            SaveScene();

            EditorUtility.DisplayDialog("완료", "프로젝트 세팅이 완료되었습니다!", "확인");
        }

        private static void CreateTerrain()
        {
            // Ground 생성
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.isStatic = true;

            // 머티리얼 인스턴스 생성하여 색상 적용 (URP 호환)
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            Material groundMat = new Material(groundRenderer.sharedMaterial);
            groundMat.color = new Color(0.3f, 0.5f, 0.3f);
            groundRenderer.material = groundMat;

            // NavMeshSurface 추가 및 베이크
            var surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType != null)
            {
                var surface = ground.AddComponent(surfaceType);
                var serializedObj = new SerializedObject(surface);
                var collectObjectsProp = serializedObj.FindProperty("m_CollectObjects");
                if (collectObjectsProp != null)
                {
                    collectObjectsProp.enumValueIndex = 0; // All
                    serializedObj.ApplyModifiedProperties();
                }
                
                // NavMesh 베이크
                var buildMethod = surfaceType.GetMethod("BuildNavMesh");
                if (buildMethod != null)
                {
                    buildMethod.Invoke(surface, null);
                }
                Debug.Log("NavMesh 베이크 완료");
            }
            else
            {
                Debug.LogWarning("NavMeshSurface를 찾을 수 없습니다. 패키지 매니저에서 AI Navigation 패키지를 설치하세요.");
            }

            Debug.Log("지형 생성 완료");
        }

        private static void CreateManagers()
        {
            // GameManager
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<Core.GameManager>();
            Undo.RegisterCreatedObjectUndo(gameManager, "Create GameManager");

            // SelectionManager
            GameObject selectionManager = new GameObject("SelectionManager");
            selectionManager.AddComponent<Core.SelectionManager>();
            Undo.RegisterCreatedObjectUndo(selectionManager, "Create SelectionManager");

            // CommandManager
            GameObject commandManager = new GameObject("CommandManager");
            commandManager.AddComponent<Core.CommandManager>();
            Undo.RegisterCreatedObjectUndo(commandManager, "Create CommandManager");

            // ResourceManager
            GameObject resourceManager = new GameObject("ResourceManager");
            resourceManager.AddComponent<RTS.ResourceManager>();
            Undo.RegisterCreatedObjectUndo(resourceManager, "Create ResourceManager");

            // QuestManager
            GameObject questManager = new GameObject("QuestManager");
            questManager.AddComponent<RPG.QuestManager>();
            Undo.RegisterCreatedObjectUndo(questManager, "Create QuestManager");

            // SpecManager
            GameObject specManager = new GameObject("SpecManager");
            specManager.AddComponent<OpenSpec.SpecManager>();
            Undo.RegisterCreatedObjectUndo(specManager, "Create SpecManager");

            // SpecInitializer
            GameObject specInitializer = new GameObject("SpecInitializer");
            specInitializer.AddComponent<Core.SpecInitializer>();
            Undo.RegisterCreatedObjectUndo(specInitializer, "Create SpecInitializer");

            Debug.Log("매니저 생성 완료");
        }

        private static void CreateUnitPrefab()
        {
            // Unit Prefab 생성
            GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name = "Unit";
            unit.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // NavMeshAgent 추가
            NavMeshAgent agent = unit.AddComponent<NavMeshAgent>();
            agent.speed = 5f;
            agent.acceleration = 8f;
            agent.radius = 0.5f;
            agent.height = 1f;

            // Selection Indicator (원형 표시)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "SelectionIndicator";
            indicator.transform.SetParent(unit.transform);
            indicator.transform.localPosition = new Vector3(0, -0.5f, 0);
            indicator.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
            Material indicatorMat = new Material(indicatorRenderer.sharedMaterial);
            indicatorMat.color = Color.green;
            indicatorRenderer.material = indicatorMat;
            indicator.SetActive(false);

            // Unit 스크립트 추가
            RTS.Unit unitScript = unit.AddComponent<RTS.Unit>();
            
            // SelectionIndicator 참조 설정 (SerializedObject 사용)
            SerializedObject so = new SerializedObject(unitScript);
            so.FindProperty("selectionIndicator").objectReferenceValue = indicator;
            so.FindProperty("unitRenderer").objectReferenceValue = unit.GetComponent<Renderer>();
            so.ApplyModifiedProperties();

            // Prefab 저장
            string prefabPath = "Assets/Prefabs/Unit.prefab";
            PrefabUtility.SaveAsPrefabAsset(unit, prefabPath);
            
            // 씬에서 삭제
            GameObject.DestroyImmediate(unit);

            Debug.Log($"Unit Prefab 생성 완료: {prefabPath}");
        }

        private static void CreateBuildingPrefabs()
        {
            // Base Prefab
            CreateBuildingPrefab("Base", PrimitiveType.Cube, new Vector3(3, 1.5f, 3), Color.blue, BuildingType.Base, null);
            
            // Barracks Prefab with Unit production
            CreateBuildingPrefab("Barracks", PrimitiveType.Cube, new Vector3(2, 1f, 2), Color.gray, BuildingType.Barracks, "Assets/Prefabs/Unit.prefab");

            Debug.Log("건물 Prefab 생성 완료");
        }

        private static void CreateBuildingPrefab(string name, PrimitiveType shape, Vector3 scale, Color color, BuildingType type, string unitPrefabPath)
        {
            GameObject building = GameObject.CreatePrimitive(shape);
            building.name = name;
            building.transform.localScale = scale;

            // 머티리얼 인스턴스 생성하여 색상 적용 (URP 호환)
            Renderer buildingRenderer = building.GetComponent<Renderer>();
            Material mat = new Material(buildingRenderer.sharedMaterial);
            mat.color = color;
            buildingRenderer.material = mat;

            // Selection Indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "SelectionIndicator";
            indicator.transform.SetParent(building.transform);
            indicator.transform.localPosition = new Vector3(0, -scale.y / 2 - 0.1f, 0);
            indicator.transform.localScale = new Vector3(scale.x * 1.2f, 0.1f, scale.z * 1.2f);
            Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
            Material indicatorMat = new Material(indicatorRenderer.sharedMaterial);
            indicatorMat.color = Color.cyan;
            indicatorRenderer.material = indicatorMat;
            indicator.SetActive(false);

            // Building 스크립트 추가
            Building buildingScript = building.AddComponent<Building>();
            
            SerializedObject so = new SerializedObject(buildingScript);
            so.FindProperty("buildingName").stringValue = name;
            so.FindProperty("buildingType").enumValueIndex = (int)type;
            so.FindProperty("selectionIndicator").objectReferenceValue = indicator;
            so.FindProperty("buildingRenderer").objectReferenceValue = building.GetComponent<Renderer>();
            
            // Production Queue 설정 (Barracks인 경우)
            if (unitPrefabPath != null)
            {
                GameObject unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(unitPrefabPath);
                if (unitPrefab != null)
                {
                    var productionQueue = so.FindProperty("productionQueue");
                    productionQueue.arraySize = 1;
                    
                    var element = productionQueue.GetArrayElementAtIndex(0);
                    element.FindPropertyRelative("unitName").stringValue = "Soldier";
                    element.FindPropertyRelative("unitPrefab").objectReferenceValue = unitPrefab;
                    element.FindPropertyRelative("productionTime").floatValue = 3f;
                    element.FindPropertyRelative("goldCost").floatValue = 50f;
                    element.FindPropertyRelative("manaCost").floatValue = 0f;
                }
            }
            
            so.ApplyModifiedProperties();

            // Prefab 저장
            string prefabPath = $"Assets/Prefabs/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(building, prefabPath);
            
            GameObject.DestroyImmediate(building);

            Debug.Log($"{name} Prefab 생성 완료: {prefabPath}");
        }

        private static void CreateCamera()
        {
            // 메인 카메라 RTS 스타일로 설정 (더 가까이)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            mainCam.transform.position = new Vector3(0, 10, -8);
            mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);
            mainCam.backgroundColor = new Color(0.1f, 0.15f, 0.2f);
            mainCam.orthographic = false;
            mainCam.fieldOfView = 50f;

            // 미니맵 카메라 생성
            GameObject minimapCamObj = new GameObject("MinimapCamera");
            Camera minimapCam = minimapCamObj.AddComponent<Camera>();
            minimapCam.orthographic = true;
            minimapCam.orthographicSize = 50f;
            minimapCam.transform.position = new Vector3(0, 100, 0);
            minimapCam.transform.rotation = Quaternion.Euler(90, 0, 0);
            minimapCam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
            minimapCam.clearFlags = CameraClearFlags.SolidColor;
            minimapCam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            minimapCam.depth = -1;

            Debug.Log("카메라 세팅 완료");
        }

        private static void CreateUI()
        {
            // Canvas 생성
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem 생성
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // HUD Panel (상단)
            CreateHUDPanel(canvasObj.transform);

            // Minimap Panel (좌하단)
            CreateMinimapPanel(canvasObj.transform);

            // SkillBar Panel (우하단)
            CreateSkillBarPanel(canvasObj.transform);

            // Selection Panel (하단 중앙)
            CreateSelectionPanel(canvasObj.transform);

            // Building Panel (우측)
            CreateBuildingPanel(canvasObj.transform);

            Debug.Log("UI Canvas 구성 완료");
        }

        private static void CreateHUDPanel(Transform parent)
        {
            GameObject hudPanel = CreatePanel(parent, "HUD_Panel", 
                new Vector2(0, 1), new Vector2(1, 1), 
                new Vector2(0, -50), new Vector2(0, -100));

            // Gold Text
            CreateText(hudPanel.transform, "GoldText", "Gold: 500", 
                new Vector2(20, 0), new Vector2(200, 40), TextAlignmentOptions.Left);

            // Mana Text
            CreateText(hudPanel.transform, "ManaText", "Mana: 100/200", 
                new Vector2(250, 0), new Vector2(200, 40), TextAlignmentOptions.Left);

            // Speed Text
            CreateText(hudPanel.transform, "SpeedText", "Speed: 1.0x", 
                new Vector2(-300, 0), new Vector2(150, 40), TextAlignmentOptions.Right);

            // Pause Button
            CreateButton(hudPanel.transform, "PauseButton", "Pause", 
                new Vector2(-100, 0), new Vector2(100, 40));

            // Speed Up Button
            CreateButton(hudPanel.transform, "SpeedUpButton", ">>", 
                new Vector2(-200, 0), new Vector2(50, 40));

            // Speed Down Button
            CreateButton(hudPanel.transform, "SpeedDownButton", "<<", 
                new Vector2(-250, 0), new Vector2(50, 40));

            // HUDController 추가
            var hudController = hudPanel.AddComponent<UI.HUDController>();
        }

        private static void CreateMinimapPanel(Transform parent)
        {
            GameObject minimapPanel = CreatePanel(parent, "Minimap_Panel",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(150, 150), new Vector2(250, 250));

            // Minimap RawImage
            GameObject minimapImage = new GameObject("MinimapImage");
            minimapImage.transform.SetParent(minimapPanel.transform, false);
            RectTransform minimapRect = minimapImage.AddComponent<RectTransform>();
            minimapRect.anchorMin = Vector2.zero;
            minimapRect.anchorMax = Vector2.one;
            minimapRect.offsetMin = Vector2.zero;
            minimapRect.offsetMax = Vector2.zero;
            RawImage rawImage = minimapImage.AddComponent<RawImage>();
            rawImage.color = new Color(1, 1, 1, 0.8f);

            // Player Indicator
            GameObject playerIndicator = new GameObject("PlayerIndicator");
            playerIndicator.transform.SetParent(minimapPanel.transform, false);
            RectTransform indicatorRect = playerIndicator.AddComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(10, 10);
            Image indicatorImage = playerIndicator.AddComponent<Image>();
            indicatorImage.color = Color.green;

            // MinimapController 추가
            var minimapController = minimapPanel.AddComponent<RTS.MinimapController>();
        }

        private static void CreateSkillBarPanel(Transform parent)
        {
            GameObject skillPanel = CreatePanel(parent, "SkillBar_Panel",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-300, 50), new Vector2(-50, 200));

            // Hero Info
            CreateText(skillPanel.transform, "LevelText", "Lv.1",
                new Vector2(0, 120), new Vector2(100, 30), TextAlignmentOptions.Center);

            // HP Bar
            CreateSlider(skillPanel.transform, "HealthBar", Color.red,
                new Vector2(0, 80), new Vector2(200, 20));

            // MP Bar
            CreateSlider(skillPanel.transform, "ManaBar", Color.blue,
                new Vector2(0, 50), new Vector2(200, 20));

            // EXP Bar
            CreateSlider(skillPanel.transform, "ExpBar", Color.yellow,
                new Vector2(0, 20), new Vector2(200, 10));

            // Skill Buttons
            for (int i = 0; i < 4; i++)
            {
                CreateButton(skillPanel.transform, $"Skill{i}", (i + 1).ToString(),
                    new Vector2(-75 + i * 50, -30), new Vector2(40, 40));
            }
        }

        private static void CreateSelectionPanel(Transform parent)
        {
            GameObject selectionPanel = CreatePanel(parent, "Selection_Panel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-150, 220), new Vector2(150, 300));

            CreateText(selectionPanel.transform, "SelectionCountText", "Selected: 0",
                new Vector2(0, 60), new Vector2(200, 30), TextAlignmentOptions.Center);

            CreateSlider(selectionPanel.transform, "HealthBar", Color.green,
                new Vector2(0, 20), new Vector2(250, 20));

            CreateText(selectionPanel.transform, "UnitInfoText", "HP: 100/100",
                new Vector2(0, -10), new Vector2(200, 30), TextAlignmentOptions.Center);

            selectionPanel.SetActive(false);
        }

        private static void CreateBuildingPanel(Transform parent)
        {
            GameObject buildingPanel = CreatePanel(parent, "Building_Panel",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-350, 50), new Vector2(-50, 300));

            CreateText(buildingPanel.transform, "BuildingNameText", "Building Name",
                new Vector2(0, 120), new Vector2(250, 30), TextAlignmentOptions.Center);

            CreateSlider(buildingPanel.transform, "HealthBar", Color.green,
                new Vector2(0, 80), new Vector2(250, 20));

            CreateText(buildingPanel.transform, "HealthText", "HP: 500/500",
                new Vector2(0, 50), new Vector2(200, 30), TextAlignmentOptions.Center);

            // Production Content
            GameObject productionContent = new GameObject("ProductionContent");
            productionContent.transform.SetParent(buildingPanel.transform, false);
            RectTransform contentRect = productionContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 0);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, 40);

            // BuildingUI 추가
            buildingPanel.AddComponent<UI.BuildingUI>();

            buildingPanel.SetActive(false);
        }

        // Helper Methods
        private static GameObject CreatePanel(Transform parent, string name, 
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.5f);

            return panel;
        }

        private static void CreateText(Transform parent, string name, string text,
            Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = alignment;
        }

        private static void CreateButton(Transform parent, string name, string text,
            Vector2 position, Vector2 size)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateSlider(Transform parent, string name, Color fillColor,
            Vector2 position, Vector2 size)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;

            slider.fillRect = fillRect;
        }

        private static void SpawnUnits()
        {
            string prefabPath = "Assets/Prefabs/Unit.prefab";
            GameObject unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (unitPrefab == null)
            {
                Debug.LogError($"Unit Prefab을 찾을 수 없습니다: {prefabPath}");
                return;
            }

            // 5개의 유닛을 그리드 형태로 배치
            Vector3 startPosition = new Vector3(-5, 0.5f, -5);
            float spacing = 2f;

            for (int i = 0; i < 5; i++)
            {
                float x = startPosition.x + (i % 3) * spacing;
                float z = startPosition.z + (i / 3) * spacing;
                Vector3 position = new Vector3(x, startPosition.y, z);

                GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(unitPrefab);
                unit.transform.position = position;
                unit.name = $"Unit_{i + 1}";
                
                Undo.RegisterCreatedObjectUndo(unit, $"Spawn Unit {i + 1}");
            }

            Debug.Log("유닛 5개 배치 완료");
        }

        private static void SpawnBuildings()
        {
            // Base 배치
            SpawnBuilding("Base", new Vector3(0, 0.75f, 0));
            
            // Barracks 배치
            SpawnBuilding("Barracks", new Vector3(5, 0.5f, 0));

            Debug.Log("건물 배치 완료");
        }

        private static void SpawnBuilding(string name, Vector3 position)
        {
            string prefabPath = $"Assets/Prefabs/{name}.prefab";
            GameObject buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (buildingPrefab == null)
            {
                Debug.LogError($"{name} Prefab을 찾을 수 없습니다: {prefabPath}");
                return;
            }

            GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(buildingPrefab);
            building.transform.position = position;
            building.name = name;
            
            // Barracks인 경우 Production Queue 설정
            if (name == "Barracks")
            {
                Building buildingScript = building.GetComponent<Building>();
                if (buildingScript != null)
                {
                    SerializedObject so = new SerializedObject(buildingScript);
                    var productionQueue = so.FindProperty("productionQueue");
                    
                    if (productionQueue.arraySize == 0)
                    {
                        string unitPrefabPath = "Assets/Prefabs/Unit.prefab";
                        GameObject unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(unitPrefabPath);
                        
                        if (unitPrefab != null)
                        {
                            productionQueue.arraySize = 1;
                            var element = productionQueue.GetArrayElementAtIndex(0);
                            element.FindPropertyRelative("unitName").stringValue = "Soldier";
                            element.FindPropertyRelative("unitPrefab").objectReferenceValue = unitPrefab;
                            element.FindPropertyRelative("productionTime").floatValue = 3f;
                            element.FindPropertyRelative("goldCost").floatValue = 50f;
                            element.FindPropertyRelative("manaCost").floatValue = 0f;
                            so.ApplyModifiedProperties();
                            
                            Debug.Log("Barracks Production Queue 설정 완료");
                        }
                    }
                }
            }
            
            Undo.RegisterCreatedObjectUndo(building, $"Spawn {name}");
        }

        private static void SaveScene()
        {
            string scenePath = "Assets/Scenes/MainScene.unity";
            
            // Scenes 폴더가 없으면 생성
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            // 현재 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            // 새 씬으로 저장
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"씬 저장 완료: {scenePath}");
        }
    }
}
