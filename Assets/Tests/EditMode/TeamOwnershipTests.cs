using NUnit.Framework;
using UnityEngine;
using RealmCommander.RTS;
using RealmCommander.Core;

namespace RealmCommander.Tests.EditMode
{
    public class TeamOwnershipTests
    {
        [Test]
        public void Unit_ConfigureTeam_SetsIsEnemy()
        {
            var go = new GameObject("TestUnit");
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            go.AddComponent<Mirror.NetworkIdentity>();
            var unit = go.AddComponent<Unit>();

            unit.ConfigureTeam(true);
            Assert.IsTrue(unit.IsEnemy, "Unit should be enemy after ConfigureTeam(true)");

            unit.ConfigureTeam(false);
            Assert.IsFalse(unit.IsEnemy, "Unit should not be enemy after ConfigureTeam(false)");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Building_ConfigureTeam_SetsTeamId()
        {
            var go = new GameObject("TestBuilding");
            go.AddComponent<Mirror.NetworkIdentity>();
            var building = go.AddComponent<Building>();

            building.ConfigureTeam(1);
            Assert.AreEqual(1, building.TeamId, "Building should have team 1");

            building.ConfigureTeam(0);
            Assert.AreEqual(0, building.TeamId, "Building should have team 0");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Unit_CanIssueLocalCommands_ClientOwned()
        {
            var go = new GameObject("TestUnit");
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            go.AddComponent<Mirror.NetworkIdentity>();
            var unit = go.AddComponent<Unit>();

            Assert.IsTrue(unit.CanIssueLocalCommands, "Standalone should allow commands");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Building_CanIssueLocalCommands_ChecksTeam()
        {
            var go = new GameObject("TestBuilding");
            go.AddComponent<Mirror.NetworkIdentity>();
            var building = go.AddComponent<Building>();

            Assert.IsTrue(building.CanIssueLocalCommands, "Standalone should allow commands");

            Object.DestroyImmediate(go);
        }
    }
}
