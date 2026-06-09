using UnityEngine;

namespace RealmCommander.Effects
{
    public class SkillEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [SerializeField] private GameObject impactVFXPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject auraPrefab;
        [SerializeField] private float effectDuration = 2f;

        private static SkillEffect instance;

        private void Awake()
        {
            instance = this;
        }

        public static void PlayImpactEffect(Vector3 position, Color color)
        {
            if (instance == null) return;

            if (instance.impactVFXPrefab != null)
            {
                GameObject vfx = Instantiate(instance.impactVFXPrefab, position, Quaternion.identity);
                Destroy(vfx, instance.effectDuration);
            }

            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.position = position;
            flash.transform.localScale = Vector3.one * 1.5f;

            var renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                renderer.material.SetFloat("_Mode", 3);
            }

            Destroy(flash, instance.effectDuration);
        }

        public static void PlayProjectileEffect(Vector3 from, Vector3 to, Color color)
        {
            if (instance == null) return;

            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.transform.position = from;
            proj.transform.localScale = Vector3.one * 0.3f;

            var renderer = proj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            instance.StartCoroutine(instance.ProjectileRoutine(proj, from, to));
        }

        private System.Collections.IEnumerator ProjectileRoutine(GameObject projectile, Vector3 from, Vector3 to)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration && projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (projectile != null)
            {
                PlayImpactEffect(to, Color.yellow);
                Destroy(projectile);
            }
        }

        public static void PlayAuraEffect(GameObject target, Color color)
        {
            if (instance == null || target == null) return;

            GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aura.transform.SetParent(target.transform);
            aura.transform.localPosition = Vector3.up * 1f;
            aura.transform.localScale = Vector3.one * 2f;

            var renderer = aura.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                renderer.material.SetFloat("_Mode", 3);
            }

            Object.Destroy(aura, instance.effectDuration);
        }
    }
}
