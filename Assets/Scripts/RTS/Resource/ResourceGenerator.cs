using System;
using UnityEngine;
using Mirror;

namespace RealmCommander.RTS
{
    public class ResourceGenerator : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Gold;
        [SerializeField] private float generationRate = 5f;
        [SerializeField, Range(0, 1)] private int teamId;

        public ResourceType ResourceType => resourceType;
        public float GenerationRate => generationRate;

        private void Start()
        {
            Building owner = GetComponentInParent<Building>();
            if (owner != null)
                teamId = owner.TeamId;
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (ResourceManager.Instance != null)
            {
                float amount = generationRate * Time.deltaTime;

                switch (resourceType)
                {
                    case ResourceType.Gold:
                        ResourceManager.Instance.AddGold(teamId, amount);
                        break;
                    case ResourceType.Mana:
                        ResourceManager.Instance.AddMana(teamId, amount);
                        break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = resourceType == ResourceType.Gold ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 3f);
        }
    }

    public enum ResourceType
    {
        Gold,
        Mana
    }
}
