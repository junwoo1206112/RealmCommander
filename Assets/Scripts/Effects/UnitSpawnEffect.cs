using UnityEngine;

namespace RealmCommander.Effects
{
    public class UnitSpawnEffect : MonoBehaviour
    {
        [Header("Spawn Effect Settings")]
        [SerializeField] private GameObject spawnVFXPrefab;
        [SerializeField] private float effectDuration = 1f;
        [SerializeField] private Color spawnColor = Color.cyan;
        [SerializeField] private float spawnFlashSpeed = 0.1f;

        public void PlaySpawnEffect(Vector3 position)
        {
            if (spawnVFXPrefab != null)
            {
                GameObject vfx = Instantiate(spawnVFXPrefab, position, Quaternion.identity);
                Destroy(vfx, effectDuration);
            }

            StartCoroutine(SpawnFlashRoutine(position));
        }

        private System.Collections.IEnumerator SpawnFlashRoutine(Vector3 position)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.position = position;
            flash.transform.localScale = Vector3.one * 0.5f;

            var renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = spawnColor;
            }

            Destroy(flash, effectDuration);

            float elapsed = 0f;
            while (elapsed < effectDuration)
            {
                float scale = Mathf.Lerp(0.5f, 0f, elapsed / effectDuration);
                flash.transform.localScale = Vector3.one * scale;
                elapsed += spawnFlashSpeed;
                yield return new WaitForSeconds(spawnFlashSpeed);
            }
        }
    }
}
