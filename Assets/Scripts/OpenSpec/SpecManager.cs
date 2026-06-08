using System;
using System.Collections.Generic;
using System.IO;
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

        public void LoadSpecs(string category, string csvPath)
        {
            if (!specDatabase.ContainsKey(category))
            {
                specDatabase[category] = new Dictionary<string, SpecData>();
            }

            if (!File.Exists(csvPath))
            {
                Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
                return;
            }

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2) return;

            string[] headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                if (values.Length < headers.Length) continue;

                SpecData spec = new SpecData();
                spec.id = values[0];
                spec.name = values.Length > 1 ? values[1] : "";
                spec.description = values.Length > 2 ? values[2] : "";

                for (int j = 3; j < headers.Length && j < values.Length; j++)
                {
                    string key = headers[j];
                    string value = values[j];

                    if (float.TryParse(value, out float floatValue))
                    {
                        spec.properties[key] = floatValue;
                    }
                    else if (int.TryParse(value, out int intValue))
                    {
                        spec.properties[key] = intValue;
                    }
                    else if (bool.TryParse(value, out bool boolValue))
                    {
                        spec.properties[key] = boolValue;
                    }
                    else
                    {
                        spec.properties[key] = value;
                    }
                }

                specDatabase[category][spec.id] = spec;
            }

            Debug.Log($"[{category}] {specDatabase[category].Count}개 스펙 로드 완료");
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

            if (spec.properties.ContainsKey(propertyKey))
            {
                object value = spec.properties[propertyKey];
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }
    }
}
