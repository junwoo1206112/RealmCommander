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

            SpecManager.Instance.LoadSpecs("units", "Specs/units");
            SpecManager.Instance.LoadSpecs("buildings", "Specs/buildings");
            SpecManager.Instance.LoadSpecs("skills", "Specs/skills");

            Debug.Log("OpenSpec 초기화 완료");
        }
    }
}
