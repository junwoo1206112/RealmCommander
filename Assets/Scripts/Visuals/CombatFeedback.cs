using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RealmCommander.Core;

namespace RealmCommander.Visuals
{
    public class CombatFeedback : MonoBehaviour
    {
        private static Material hitMaterial;
        private static readonly Queue<GameObject> pulsePool = new Queue<GameObject>();
        private static readonly Queue<GameObject> textPool = new Queue<GameObject>();
        private const int MaxPoolSize = 20;

        public static void PlayHit(GameObject target, Color color)
        {
            if (target == null) return;
            CombatFeedback feedback = target.GetComponent<CombatFeedback>();
            if (feedback == null)
                feedback = target.AddComponent<CombatFeedback>();
            feedback.StopAllCoroutines();
            feedback.StartCoroutine(feedback.HitRoutine(color));
        }

        public static void ShowDamageNumber(GameObject target, float damage, bool isCrit = false)
        {
            if (target == null) return;
            CombatFeedback feedback = target.GetComponent<CombatFeedback>();
            if (feedback == null)
                feedback = target.AddComponent<CombatFeedback>();
            feedback.StartCoroutine(feedback.DamageTextRoutine(damage, isCrit));
        }

        private IEnumerator HitRoutine(Color color)
        {
            GameObject pulse = GetPooledPulse();
            pulse.transform.SetParent(transform, false);
            pulse.transform.localPosition = Vector3.up * 0.65f;
            pulse.transform.localScale = Vector3.one * 0.25f;
            pulse.SetActive(true);

            Renderer renderer = pulse.GetComponent<Renderer>();
            if (renderer != null)
            {
                EnsureMaterial();
                renderer.sharedMaterial = hitMaterial;
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }

            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration && pulse != null && pulse.activeSelf)
            {
                float t = elapsed / duration;
                pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.9f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            ReturnToPool(pulse, pulsePool);
        }

        private IEnumerator DamageTextRoutine(float damage, bool isCrit)
        {
            GameObject textObj = GetPooledText();
            textObj.transform.SetParent(null);
            textObj.transform.position = transform.position + Vector3.up * 1.5f + Random.insideUnitSphere * 0.3f;
            textObj.SetActive(true);

            TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
            if (tmp == null) yield break;

            tmp.text = damage >= 100 ? Mathf.FloorToInt(damage).ToString() : damage.ToString("F0");
            tmp.fontSize = isCrit ? 6f : 4f;
            tmp.color = isCrit ? Color.yellow : Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = isCrit ? FontStyles.Bold : FontStyles.Normal;

            Vector3 startPos = textObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 1.5f;
            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration && textObj != null && textObj.activeSelf)
            {
                float t = elapsed / duration;
                textObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f - t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            ReturnToPool(textObj, textPool);
        }

        private static GameObject GetPooledPulse()
        {
            if (pulsePool.Count > 0)
            {
                GameObject obj = pulsePool.Dequeue();
                if (obj != null) return obj;
            }
            return CreatePulseObject();
        }

        private static GameObject CreatePulseObject()
        {
            GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pulse.name = "HitPulse_Pooled";
            Collider collider = pulse.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return pulse;
        }

        private static GameObject GetPooledText()
        {
            if (textPool.Count > 0)
            {
                GameObject obj = textPool.Dequeue();
                if (obj != null) return obj;
            }
            return CreateTextObject();
        }

        private static GameObject CreateTextObject()
        {
            GameObject textObj = new GameObject("DamageText_Pooled");
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            Canvas canvas = textObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 0.5f);
            return textObj;
        }

        private static void ReturnToPool(GameObject obj, Queue<GameObject> pool)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(null);
            if (pool.Count < MaxPoolSize)
                pool.Enqueue(obj);
            else
                Destroy(obj);
        }

        private static void EnsureMaterial()
        {
            if (hitMaterial != null) return;
            hitMaterial = StaticResources.GetOrCreateMaterial("Unlit/Color", Color.white);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
