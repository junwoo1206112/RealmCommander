using System;
using UnityEngine;
using Mirror;

namespace RealmCommander.RTS
{
    public class ResourceGenerator : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Gold;
        [SerializeField] private float generationRate = 5f;
        [SerializeField] private float collectionRadius = 3f;

        public ResourceType ResourceType => resourceType;
        public float GenerationRate => generationRate;

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (ResourceManager.Instance != null)
            {
                float amount = generationRate * Time.deltaTime;

                switch (resourceType)
                {
                    case ResourceType.Gold:
                        ResourceManager.Instance.AddGold(amount);
                        break;
                    case ResourceType.Mana:
                        ResourceManager.Instance.AddMana(amount);
                        break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = resourceType == ResourceType.Gold ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
    }

    public enum ResourceType
    {
        Gold,
        Mana
    }
}
