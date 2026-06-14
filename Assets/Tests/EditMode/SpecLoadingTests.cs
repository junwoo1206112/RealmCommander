using NUnit.Framework;
using UnityEngine;
using RealmCommander.Core;

namespace RealmCommander.Tests.EditMode
{
    public class SpecLoadingTests
    {
        [Test]
        public void SpecManager_CanBeCreated()
        {
            var go = new GameObject("SpecManager");
            var manager = go.AddComponent<OpenSpec.SpecManager>();
            Assert.IsNotNull(manager);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ArtAssetLookup_LoadUnitIcon_ReturnsNullOrSprite()
        {
            Sprite result = ArtAssetLookup.LoadUnitIcon("unit_soldier");
            Assert.IsTrue(result == null || result is Sprite);
        }

        [Test]
        public void ArtAssetLookup_LoadIcon_EmptyString_ReturnsNull()
        {
            Sprite result = ArtAssetLookup.LoadIcon("");
            Assert.IsNull(result);
        }

        [Test]
        public void ArtAssetLookup_LoadIcon_NullString_ReturnsNull()
        {
            Sprite result = ArtAssetLookup.LoadIcon(null);
            Assert.IsNull(result);
        }
    }
}
