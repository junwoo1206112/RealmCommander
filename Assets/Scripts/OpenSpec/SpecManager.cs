using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace OpenSpec
{
    [Serializable]
    public class SpecData
    {
        public string id;
        public string name;
        public string description;
        public Dictionary<string, object> properties = new Dictionary<string, object>();
    }

    public class SpecManager : MonoBehaviour
    {
        public static SpecManager Instance { get; private set; }

        private Dictionary<string, Dictionary<string, SpecData>> specDatabase = new Dictionary<string, Dictionary<string, SpecData>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool LoadSpecs(string category, string resourcePath)
        {
            TextAsset csvAsset = Resources.Load<TextAsset>(resourcePath);
            if (csvAsset == null)
            {
                Debug.LogError($"CSV 리소스를 찾을 수 없습니다: Resources/{resourcePath}.csv");
                return false;
            }

            return LoadSpecsFromText(category, csvAsset.text);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool LoadSpecsFromText(string category, string csvText)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(csvText))
            {
                Debug.LogError("스펙 category와 CSV 내용은 비어 있을 수 없습니다.");
                return false;
            }

            List<List<string>> rows = ParseCsv(csvText);
            if (rows.Count < 2)
            {
                Debug.LogWarning($"[{category}] CSV에 데이터 행이 없습니다.");
                return false;
            }

            List<string> headers = rows[0];
            var categorySpecs = new Dictionary<string, SpecData>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < rows.Count; i++)
            {
                List<string> values = rows[i];
                if (values.Count == 0 || string.IsNullOrWhiteSpace(values[0])) continue;

                var spec = new SpecData
                {
                    id = values[0].Trim(),
                    name = values.Count > 1 ? values[1].Trim() : string.Empty,
                    description = values.Count > 2 ? values[2].Trim() : string.Empty
                };

                for (int j = 3; j < headers.Count && j < values.Count; j++)
                {
                    string key = headers[j].Trim();
                    if (string.IsNullOrEmpty(key)) continue;
                    spec.properties[key] = ParseValue(values[j]);
                }

                categorySpecs[spec.id] = spec;
            }

            specDatabase[category] = categorySpecs;
            Debug.Log($"[{category}] {categorySpecs.Count}개 스펙 로드 완료");
            return true;
        }

        public SpecData GetSpec(string category, string id)
        {
            if (specDatabase.ContainsKey(category) && specDatabase[category].ContainsKey(id))
            {
                return specDatabase[category][id];
            }
            return null;
        }

        public List<SpecData> GetAllSpecs(string category)
        {
            if (specDatabase.ContainsKey(category))
            {
                return new List<SpecData>(specDatabase[category].Values);
            }
            return new List<SpecData>();
        }

        public T GetProperty<T>(string category, string id, string propertyKey, T defaultValue = default)
        {
            SpecData spec = GetSpec(category, id);
            if (spec == null) return defaultValue;

            if (spec.properties.TryGetValue(propertyKey, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                }
                catch (Exception) when (value is IConvertible)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private static object ParseValue(string rawValue)
        {
            string value = rawValue.Trim();
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                return intValue;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                return floatValue;
            if (bool.TryParse(value, out bool boolValue))
                return boolValue;
            return value;
        }

        private static List<List<string>> ParseCsv(string csvText)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char character = csvText[i];
                if (character == '"')
                {
                    if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (character == ',' && !inQuotes)
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if ((character == '\n' || character == '\r') && !inQuotes)
                {
                    if (character == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n') i++;
                    row.Add(field.ToString());
                    field.Clear();
                    if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0])) rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(character);
                }
            }

            row.Add(field.ToString());
            if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0])) rows.Add(row);
            return rows;
        }
    }
}
