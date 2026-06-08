using UnityEngine;
using OpenSpec;

namespace RealmCommander.Core
{
    public class SpecInitializer : MonoBehaviour
    {
        private void Awake()
        {
            InitializeSpecs();
        }

        private void InitializeSpecs()
        {
            if (SpecManager.Instance == null)
            {
                GameObject specManagerObj = new GameObject("SpecManager");
                specManagerObj.AddComponent<SpecManager>();
            }

            // CSV 파일 경로 (Resources 폴더)
            string unitsPath = System.IO.Path.Combine(Application.dataPath, "Resources/Specs/units.csv");
            string buildingsPath = System.IO.Path.Combine(Application.dataPath, "Resources/Specs/buildings.csv");
            string skillsPath = System.IO.Path.Combine(Application.dataPath, "Resources/Specs/skills.csv");

            // 펙 로드
            SpecManager.Instance.LoadSpecs("units", unitsPath);
            SpecManager.Instance.LoadSpecs("buildings", buildingsPath);
            SpecManager.Instance.LoadSpecs("skills", skillsPath);

            Debug.Log("OpenSpec 초기화 완료");
        }
    }
}
