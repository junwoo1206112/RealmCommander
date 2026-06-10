using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealmCommander.Core;
using RealmCommander.RPG;

namespace RealmCommander.UI
{
    public class SkillBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Hero hero;
        [SerializeField] private SkillSlotUI[] skillSlots;
        [SerializeField] private Slider heroHealthBar;
        [SerializeField] private Slider heroManaBar;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider expBar;

        private void Start()
        {
            if (hero == null)
            {
                gameObject.SetActive(false);
                return;
            }

            hero.OnStatsChanged += UpdateHeroUI;
            UpdateHeroUI(hero.Data);
        }

        private void OnDestroy()
        {
            if (hero != null)
            {
                hero.OnStatsChanged -= UpdateHeroUI;
            }
        }

        private void Update()
        {
            if (hero == null) return;
            UpdateSkillCooldowns();
        }

        private void UpdateHeroUI(HeroData data)
        {
            if (data == null) return;

            if (heroHealthBar != null)
            {
                heroHealthBar.value = data.HealthPercent;
            }

            if (heroManaBar != null)
            {
                heroManaBar.value = data.ManaPercent;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv.{data.level}";
            }

            if (expBar != null)
            {
                expBar.value = data.ExpPercent;
            }
        }

        private void UpdateSkillCooldowns()
        {
            if (hero == null || hero.Data == null) return;

            for (int i = 0; i < skillSlots.Length && i < hero.Data.skills.Count; i++)
            {
                if (skillSlots[i] != null)
                {
                    skillSlots[i].UpdateCooldown(hero.Data.skills[i]);
                }
            }
        }

        public void OnSkillClicked(int index)
        {
            if (hero == null) return;

            GameObject target = null;
            var selected = SelectionManager.Instance?.SelectedUnits;
            if (selected != null && selected.Count > 0)
            {
                target = selected[0];
            }

            hero.TryCastSkill(index, target);
        }
    }

    [System.Serializable]
    public class SkillSlotUI
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private Button skillButton;

        public void UpdateCooldown(SkillData skill)
        {
            if (skill == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = skill.icon;
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = skill.CooldownPercent;
                cooldownOverlay.gameObject.SetActive(!skill.IsReady);
            }

            if (cooldownText != null)
            {
                if (skill.IsReady)
                {
                    cooldownText.text = "";
                }
                else
                {
                    cooldownText.text = $"{skill.currentCooldown:F1}";
                }
            }

            if (skillButton != null)
            {
                skillButton.interactable = skill.IsReady;
            }
        }
    }
}
