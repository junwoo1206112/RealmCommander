using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.Tests.PlayMode
{
    public class ResourceIsolationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ResourceManager_TeamIsolation_AcrossFrames()
        {
            var go = new GameObject("ResourceManager");
            go.AddComponent<Mirror.NetworkIdentity>();
            go.AddComponent<ResourceManager>();

            yield return null;

            var rm = ResourceManager.Instance;
            Assert.IsNotNull(rm);

            rm.AddGold(0, 100f);
            rm.AddGold(1, 50f);

            yield return null;

            float t0 = rm.GetGold(0);
            float t1 = rm.GetGold(1);

            Assert.AreEqual(100f, t0, 0.01f);
            Assert.AreEqual(50f, t1, 0.01f);

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator ResourceManager_PassiveIncome_BothTeams()
        {
            var go = new GameObject("ResourceManager");
            go.AddComponent<Mirror.NetworkIdentity>();
            var rm = go.AddComponent<ResourceManager>();

            yield return null;

            float t0Before = rm.GetGold(0);
            float t1Before = rm.GetGold(1);

            rm.AddGold(0, 10f);
            rm.AddGold(1, 10f);

            yield return null;

            Assert.Greater(rm.GetGold(0), t0Before);
            Assert.Greater(rm.GetGold(1), t1Before);

            Object.DestroyImmediate(go);
        }
    }
}
