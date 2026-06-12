using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RealmCommander.UI;

namespace RealmCommander.Editor
{
    public static class SkillBarSetup
    {
        private const string SkillBarPanelName = "SkillBar_Panel";

        [MenuItem("Tools/Realm Commander/Setup SkillBar UI")]
        public static void SetupSkillBarUI()
        {
            if (!EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveOpenScenes();

            GameObject panel = FindChildRecursive(null, SkillBarPanelName);
            if (panel == null)
            {
                Debug.LogError("[SkillBarSetup] SkillBar_Panel not found in scene.");
                return;
            }

            SkillBarUI skillBar = panel.GetComponent<SkillBarUI>();
            if (skillBar == null)
                skillBar = panel.AddComponent<SkillBarUI>();

            Slider healthBar = FindChildRecursive(panel, "HealthBar")?.GetComponent<Slider>();
            Slider manaBar = FindChildRecursive(panel, "ManaBar")?.GetComponent<Slider>();
            Slider expBar = FindChildRecursive(panel, "ExpBar")?.GetComponent<Slider>();
            TextMeshProUGUI levelText = FindChildRecursive(panel, "LevelText")?.GetComponent<TextMeshProUGUI>();

            string[] skillNames = { "Skill0", "Skill1", "Skill2", "Skill3" };
            SkillSlotUI[] slots = new SkillSlotUI[2];

            for (int i = 0; i < 2; i++)
            {
                GameObject skillObj = FindChildRecursive(panel, skillNames[i]);
                if (skillObj == null) continue;

                Image iconImage = skillObj.GetComponent<Image>();
                Button skillButton = skillObj.GetComponent<Button>();

                GameObject cooldownOverlayObj = FindChildRecursive(skillObj, "CooldownOverlay");
                if (cooldownOverlayObj == null)
                {
                    cooldownOverlayObj = new GameObject("CooldownOverlay");
                    cooldownOverlayObj.transform.SetParent(skillObj.transform, false);
                    RectTransform rt = cooldownOverlayObj.AddComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.localPosition = Vector3.zero;
                    rt.localScale = Vector3.one;

                    Image overlayImg = cooldownOverlayObj.AddComponent<Image>();
                    overlayImg.color = new Color(0, 0, 0, 0.6f);
                    overlayImg.type = Image.Type.Filled;
                    overlayImg.fillMethod = Image.FillMethod.Vertical;
                    overlayImg.fillOrigin = 0;
                    overlayImg.fillClockwise = true;

                    CanvasRenderer cr = cooldownOverlayObj.AddComponent<CanvasRenderer>();
                }

                GameObject cooldownTextObj = FindChildRecursive(skillObj, "CooldownText");
                if (cooldownTextObj == null)
                {
                    cooldownTextObj = new GameObject("CooldownText");
                    cooldownTextObj.transform.SetParent(skillObj.transform, false);
                    RectTransform rt = cooldownTextObj.AddComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.localPosition = Vector3.zero;
                    rt.localScale = Vector3.one;

                    TextMeshProUGUI tmp = cooldownTextObj.AddComponent<TextMeshProUGUI>();
                    tmp.text = "";
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 14;
                    tmp.color = Color.white;

                    CanvasRenderer cr = cooldownTextObj.AddComponent<CanvasRenderer>();
                }

                Image overlay = FindChildRecursive(skillObj, "CooldownOverlay")?.GetComponent<Image>();
                TextMeshProUGUI cooldownText = FindChildRecursive(skillObj, "CooldownText")?.GetComponent<TextMeshProUGUI>();

                slots[i] = new SkillSlotUI();
                SerializedObject slotObj = new SerializedObject(skillBar);
                SerializedProperty skillSlotsProp = slotObj.FindProperty("skillSlots");

                while (skillSlotsProp.arraySize <= i)
                    skillSlotsProp.InsertArrayElementAtIndex(i);

                SerializedProperty slotElement = skillSlotsProp.GetArrayElementAtIndex(i);
                slotElement.FindPropertyRelative("iconImage").objectReferenceValue = iconImage;
                slotElement.FindPropertyRelative("cooldownOverlay").objectReferenceValue = overlay;
                slotElement.FindPropertyRelative("cooldownText").objectReferenceValue = cooldownText;
                slotElement.FindPropertyRelative("skillButton").objectReferenceValue = skillButton;
                slotObj.ApplyModifiedPropertiesWithoutUndo();
            }

            SerializedObject barSO = new SerializedObject(skillBar);
            barSO.FindProperty("heroHealthBar").objectReferenceValue = healthBar;
            barSO.FindProperty("heroManaBar").objectReferenceValue = manaBar;
            barSO.FindProperty("levelText").objectReferenceValue = levelText;
            barSO.FindProperty("expBar").objectReferenceValue = expBar;
            barSO.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < 2; i++)
            {
                GameObject skillObj = FindChildRecursive(panel, skillNames[i]);
                if (skillObj == null) continue;

                Button btn = skillObj.GetComponent<Button>();
                if (btn == null) continue;

                btn.onClick.RemoveAllListeners();

                int capturedIndex = i;
                btn.onClick.AddListener(() =>
                {
                    if (skillBar != null)
                        skillBar.OnSkillClicked(capturedIndex);
                });
            }

            for (int i = 2; i < skillNames.Length; i++)
            {
                GameObject inactiveSlot = FindChildRecursive(panel, skillNames[i]);
                if (inactiveSlot != null)
                    inactiveSlot.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[SkillBarSetup] PASS panel={SkillBarPanelName} slots=2 callbacks=2");
        }

        private static GameObject FindChildRecursive(GameObject parent, string name)
        {
            if (parent == null)
            {
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (root.name == name) return root;
                    GameObject found = FindChildRecursive(root, name);
                    if (found != null) return found;
                }
                return null;
            }

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                Transform child = parent.transform.GetChild(i);
                if (child.name == name) return child.gameObject;
                GameObject found = FindChildRecursive(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
