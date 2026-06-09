using UnityEngine;
using Mirror;
using RealmCommander.RTS;

namespace RealmCommander.Core
{
    public class UnitSpawner : NetworkBehaviour
    {
        [Header("Unit Prefabs")]
        [SerializeField] private GameObject friendlyUnitPrefab;
        [SerializeField] private GameObject enemyUnitPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private int friendlyUnitCount = 5;
        [SerializeField] private int enemyUnitCount = 5;
        [SerializeField] private Vector3 friendlySpawnPosition = new Vector3(-10, 0, 0);
        [SerializeField] private Vector3 enemySpawnPosition = new Vector3(10, 0, 0);
        [SerializeField] private float spawnRadius = 3f;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnUnits();
        }

        [Server]
        private void SpawnUnits()
        {
            // 아군 유닛 생성
            for (int i = 0; i < friendlyUnitCount; i++)
            {
                if (friendlyUnitPrefab != null)
                {
                    Vector3 spawnPos = friendlySpawnPosition + Random.insideUnitSphere * spawnRadius;
                    spawnPos.y = 0.5f;

                    GameObject unit = Instantiate(friendlyUnitPrefab, spawnPos, Quaternion.identity);
                    unit.name = $"FriendlyUnit_{i}";
                    NetworkServer.Spawn(unit);
                }
            }

            // 적 유닛 생성
            for (int i = 0; i < enemyUnitCount; i++)
            {
                if (enemyUnitPrefab != null)
                {
                    Vector3 spawnPos = enemySpawnPosition + Random.insideUnitSphere * spawnRadius;
                    spawnPos.y = 0.5f;

                    GameObject unit = Instantiate(enemyUnitPrefab, spawnPos, Quaternion.identity);
                    unit.name = $"EnemyUnit_{i}";

                    var unitComponent = unit.GetComponent<Unit>();
                    if (unitComponent != null)
                    {
                        // 적 유닛으로 설정 (필요 시)
                    }

                    NetworkServer.Spawn(unit);
                }
            }

            Debug.Log($"유닛 생성 완료: 아군 {friendlyUnitCount}명, 적 {enemyUnitCount}명");
        }
    }
}
