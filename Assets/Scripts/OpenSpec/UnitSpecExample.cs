using UnityEngine;
using OpenSpec;

namespace RealmCommander.RTS
{
    public class UnitSpecExample : MonoBehaviour
    {
        private void Start()
        {
            // 유닛 스펙 예제
            ExampleUnitSpecs();
            
            // 건물 펙 예제
            ExampleBuildingSpecs();
            
            // 스킬 스펙 예제
            ExampleSkillSpecs();
        }

        private void ExampleUnitSpecs()
        {
            Debug.Log("=== 유닛 스 예제 ===");
            
            // 모든 유닛 스펙 가져오기
            var units = SpecManager.Instance.GetAllSpecs("units");
            foreach (var unit in units)
            {
                Debug.Log($"유닛: {unit.name}");
                Debug.Log($"  - HP: {SpecManager.Instance.GetProperty<float>("units", unit.id, "MaxHealth")}");
                Debug.Log($"  - 공격력: {SpecManager.Instance.GetProperty<float>("units", unit.id, "AttackDamage")}");
                Debug.Log($"  - 이동속도: {SpecManager.Instance.GetProperty<float>("units", unit.id, "MoveSpeed")}");
                Debug.Log($"  - 생산비용: {SpecManager.Instance.GetProperty<float>("units", unit.id, "GoldCost")} Gold");
            }
        }

        private void ExampleBuildingSpecs()
        {
            Debug.Log("=== 건물 스펙 예제 ===");
            
            var buildings = SpecManager.Instance.GetAllSpecs("buildings");
            foreach (var building in buildings)
            {
                Debug.Log($"건물: {building.name}");
                Debug.Log($"  - HP: {SpecManager.Instance.GetProperty<float>("buildings", building.id, "MaxHealth")}");
                Debug.Log($"  - 건설시간: {SpecManager.Instance.GetProperty<float>("buildings", building.id, "BuildTime")}초");
                Debug.Log($"  - 비용: {SpecManager.Instance.GetProperty<float>("buildings", building.id, "GoldCost")} Gold");
            }
        }

        private void ExampleSkillSpecs()
        {
            Debug.Log("=== 스킬 스펙 예제 ===");
            
            var skills = SpecManager.Instance.GetAllSpecs("skills");
            foreach (var skill in skills)
            {
                Debug.Log($"스킬: {skill.name}");
                Debug.Log($"  - 피해량: {SpecManager.Instance.GetProperty<float>("skills", skill.id, "Damage")}");
                Debug.Log($"  - 쿨다운: {SpecManager.Instance.GetProperty<float>("skills", skill.id, "Cooldown")}초");
                Debug.Log($"  - 마나비용: {SpecManager.Instance.GetProperty<float>("skills", skill.id, "ManaCost")}");
            }
        }
    }
}
