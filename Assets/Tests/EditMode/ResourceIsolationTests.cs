using NUnit.Framework;
using UnityEngine;
using RealmCommander.RTS;
using RealmCommander.Core;

namespace RealmCommander.Tests.EditMode
{
    public class ResourceIsolationTests
    {
        private GameObject managerObj;
        private ResourceManager resourceManager;

        [SetUp]
        public void SetUp()
        {
            managerObj = new GameObject("ResourceManager");
            managerObj.AddComponent<Mirror.NetworkIdentity>();
            resourceManager = managerObj.AddComponent<ResourceManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void ResourceManager_SingletonExists()
        {
            Assert.IsNotNull(ResourceManager.Instance);
        }

        [Test]
        public void ResourceManager_Team0_Team1_Independent()
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return;

            rm.AddGold(0, 100f);
            rm.AddGold(1, 50f);

            float t0 = rm.GetGold(0);
            float t1 = rm.GetGold(1);

            Assert.Greater(t0, t1, "Team 0 gold should be higher after adding more");
        }

        [Test]
        public void ResourceManager_TrySpend_TeamIsolation()
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return;

            rm.AddGold(0, 200f);
            rm.AddGold(1, 200f);

            rm.TrySpend(0, 50f, 0f);

            float t0 = rm.GetGold(0);
            float t1 = rm.GetGold(1);

            Assert.AreEqual(150f, t0, 0.01f);
            Assert.AreEqual(200f, t1, 0.01f, "Team 1 should be unaffected");
        }

        [Test]
        public void ResourceManager_CanAfford_ReturnsCorrectly()
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return;

            rm.AddGold(0, 100f);
            rm.AddMana(0, 50f);

            Assert.IsTrue(rm.CanAfford(0, 50f, 25f));
            Assert.IsFalse(rm.CanAfford(0, 150f, 0f));
            Assert.IsFalse(rm.CanAfford(0, 0f, 100f));
        }
    }
}
