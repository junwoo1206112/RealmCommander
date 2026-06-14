using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.Tests.PlayMode
{
    public class TeamOwnershipPlayModeTests
    {
        [UnityTest]
        public IEnumerator Unit_TeamChange_UpdatesVisual()
        {
            var go = new GameObject("TestUnit");
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            go.AddComponent<Mirror.NetworkIdentity>();
            var unit = go.AddComponent<Unit>();

            yield return null;

            unit.ConfigureTeam(true);
            yield return null;
            Assert.IsTrue(unit.IsEnemy);

            unit.ConfigureTeam(false);
            yield return null;
            Assert.IsFalse(unit.IsEnemy);

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator Building_TeamChange_UpdatesVisual()
        {
            var go = new GameObject("TestBuilding");
            go.AddComponent<Mirror.NetworkIdentity>();
            var building = go.AddComponent<Building>();

            yield return null;

            building.ConfigureTeam(1);
            yield return null;
            Assert.AreEqual(1, building.TeamId);

            building.ConfigureTeam(0);
            yield return null;
            Assert.AreEqual(0, building.TeamId);

            Object.DestroyImmediate(go);
        }
    }
}
