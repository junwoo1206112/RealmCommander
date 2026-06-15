using RealmCommander.Core;
using UnityEngine;

namespace RealmCommander.RTS
{
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Gold;
        [SerializeField] private int owningTeamId = -1;
        [SerializeField] private float gatherRadius = 4f;
        [SerializeField] private float amountPerWorkerPerSecond = 4f;
        [SerializeField] private int maxWorkers = 4;
        [SerializeField] private Color goldColor = new Color(1f, 0.78f, 0.12f);
        [SerializeField] private Color manaColor = new Color(0.15f, 0.78f, 1f);

        private Renderer cachedRenderer;
        private MaterialPropertyBlock colorBlock;
        private float updateInterval = 0.5f;
        private float nextUpdateTime;

        public void Configure(ResourceType type, int teamId = -1)
        {
            resourceType = type;
            if (teamId >= 0) owningTeamId = teamId;
            ApplyVisual();
        }

        private void Awake()
        {
            cachedRenderer = GetComponent<Renderer>();
            nextUpdateTime = Time.time + Random.Range(0f, updateInterval);
            ApplyVisual();
        }

        private void Update()
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + updateInterval;

            if (!Mirror.NetworkServer.active || ResourceManager.Instance == null) return;

            int workersPerTeam = 0;
            int producingTeam = owningTeamId;
            if (producingTeam < 0) producingTeam = CountDominantTeamWorkers(out workersPerTeam);
            else workersPerTeam = CountTeamWorkers(producingTeam);

            if (workersPerTeam <= 0) return;

            float amount = workersPerTeam * amountPerWorkerPerSecond * updateInterval;
            if (resourceType == ResourceType.Gold)
                ResourceManager.Instance.AddGold(producingTeam, amount);
            else
                ResourceManager.Instance.AddMana(producingTeam, amount);
        }

        private int CountDominantTeamWorkers(out int dominantCount)
        {
            int team0 = 0;
            int team1 = 0;
            EntityRegistry registry = EntityRegistry.Instance;
            if (registry == null) { dominantCount = 0; return 0; }

            float sqrRadius = gatherRadius * gatherRadius;
            Vector3 pos = transform.position;

            foreach (Unit unit in registry.AllUnits)
            {
                if (unit == null || !unit.IsAlive || !unit.CanGatherResources) continue;
                if ((unit.transform.position - pos).sqrMagnitude > sqrRadius) continue;
                if (unit.IsEnemy) team1++;
                else team0++;
            }

            if (team0 >= team1) { dominantCount = Mathf.Min(team0, maxWorkers); return 0; }
            { dominantCount = Mathf.Min(team1, maxWorkers); return 1; }
        }

        private int CountTeamWorkers(int teamId)
        {
            int workers = 0;
            EntityRegistry registry = EntityRegistry.Instance;
            if (registry == null) return 0;

            float sqrRadius = gatherRadius * gatherRadius;
            Vector3 pos = transform.position;

            foreach (Unit unit in registry.AllUnits)
            {
                if (unit == null || !unit.IsAlive || !unit.CanGatherResources) continue;
                if ((unit.IsEnemy ? 1 : 0) != teamId) continue;
                if ((unit.transform.position - pos).sqrMagnitude > sqrRadius) continue;
                workers++;
                if (workers >= maxWorkers) return workers;
            }

            return workers;
        }

        private void ApplyVisual()
        {
            cachedRenderer ??= GetComponent<Renderer>();
            if (cachedRenderer == null) return;
            colorBlock ??= new MaterialPropertyBlock();
            colorBlock.SetColor("_Color", resourceType == ResourceType.Gold ? goldColor : manaColor);
            cachedRenderer.SetPropertyBlock(colorBlock);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = resourceType == ResourceType.Gold ? goldColor : manaColor;
            Gizmos.DrawWireSphere(transform.position, gatherRadius);
        }
    }
}
