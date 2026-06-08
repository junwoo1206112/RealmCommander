using UnityEngine;
using UnityEditor;
using OpenSpec;

namespace OpenSpec.Editor
{
    public class SpecImporter : EditorWindow
    {
        [MenuItem("Tools/OpenSpec/Import All Specs")]
        public static void ImportAllSpecs()
        {
            string specsFolder = "Assets/Resources/Specs";
            
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            if (!AssetDatabase.IsValidFolder(specsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Specs");
            }

            // 유닛 스펙 import
            ImportSpecs("units", $"{specsFolder}/units.csv");
            
            // 건물 스펙 import
            ImportSpecs("buildings", $"{specsFolder}/buildings.csv");
            
            // 스킬 스펙 import
            ImportSpecs("skills", $"{specsFolder}/skills.csv");

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("OpenSpec", "모든 스펙이 import되었습니다!", "확인");
        }

        [MenuItem("Tools/OpenSpec/Import Unit Specs")]
        public static void ImportUnitSpecs()
        {
            ImportSpecs("units", "Assets/Resources/Specs/units.csv");
        }

        [MenuItem("Tools/OpenSpec/Import Building Specs")]
        public static void ImportBuildingSpecs()
        {
            ImportSpecs("buildings", "Assets/Resources/Specs/buildings.csv");
        }

        [MenuItem("Tools/OpenSpec/Import Skill Specs")]
        public static void ImportSkillSpecs()
        {
            ImportSpecs("skills", "Assets/Resources/Specs/skills.csv");
        }

        [MenuItem("Tools/OpenSpec/Generate Documentation")]
        public static void GenerateDocumentation()
        {
            string docsFolder = "Assets/Resources/Specs/Docs";
            
            if (!AssetDatabase.IsValidFolder(docsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Specs", "Docs");
            }

            GenerateSpecDoc("units", "Assets/Resources/Specs/units.csv", $"{docsFolder}/UnitSpecs.md");
            GenerateSpecDoc("buildings", "Assets/Resources/Specs/buildings.csv", $"{docsFolder}/BuildingSpecs.md");
            GenerateSpecDoc("skills", "Assets/Resources/Specs/skills.csv", $"{docsFolder}/SkillSpecs.md");

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("OpenSpec", "문서가 생성되었습니다!\nAssets/Resources/Specs/Docs/", "확인");
        }

        private static void ImportSpecs(string category, string csvPath)
        {
            if (!System.IO.File.Exists(csvPath))
            {
                Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
                return;
            }

            string[] lines = System.IO.File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                Debug.LogWarning($"CSV 파일이 비어있습니다: {csvPath}");
                return;
            }

            Debug.Log($"[{category}] {lines.Length - 1}개 스펙 import 완료");
        }

        private static void GenerateSpecDoc(string category, string csvPath, string outputPath)
        {
            if (!System.IO.File.Exists(csvPath)) return;

            string[] lines = System.IO.File.ReadAllLines(csvPath);
            if (lines.Length < 2) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"# {category.ToUpper()} SPECS");
            sb.AppendLine();
            sb.AppendLine($"총 {lines.Length - 1}개");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            string[] headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                if (values.Length < headers.Length) continue;

                sb.AppendLine($"## {values[1]} ({values[0]})");
                sb.AppendLine();
                
                if (values.Length > 2)
                {
                    sb.AppendLine($"> {values[2]}");
                    sb.AppendLine();
                }

                sb.AppendLine("| Property | Value |");
                sb.AppendLine("|----------|-------|");

                for (int j = 3; j < headers.Length && j < values.Length; j++)
                {
                    sb.AppendLine($"| {headers[j]} | {values[j]} |");
                }

                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            System.IO.File.WriteAllText(outputPath, sb.ToString());
            Debug.Log($"문서 생성 완료: {outputPath}");
        }
    }
}
