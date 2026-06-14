using NUnit.Framework;
using UnityEngine;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.Tests.EditMode
{
    public class CommandPermissionTests
    {
        [Test]
        public void SelectionManager_SingletonExists()
        {
            var go = new GameObject("SelectionManager");
            var sm = go.AddComponent<SelectionManager>();
            Assert.IsNotNull(SelectionManager.Instance);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CommandManager_SingletonExists()
        {
            var go = new GameObject("CommandManager");
            var cm = go.AddComponent<CommandManager>();
            Assert.IsNotNull(CommandManager.Instance);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EntityRegistry_SingletonExists()
        {
            var go = new GameObject("EntityRegistry");
            var er = go.AddComponent<EntityRegistry>();
            Assert.IsNotNull(EntityRegistry.Instance);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SelectionManager_SelectUnit_StoresSelection()
        {
            var go = new GameObject("SelectionManager");
            go.AddComponent<SelectionManager>();

            var unitGo = new GameObject("TestUnit");
            unitGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            unitGo.AddComponent<Mirror.NetworkIdentity>();
            var unit = unitGo.AddComponent<Unit>();

            SelectionManager.Instance.SelectUnit(unitGo);
            Assert.IsTrue(SelectionManager.Instance.IsUnitSelected(unitGo));

            SelectionManager.Instance.ClearSelection();
            Assert.IsFalse(SelectionManager.Instance.IsUnitSelected(unitGo));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(unitGo);
        }

        [Test]
        public void SelectionManager_AddToSelection_MultipleUnits()
        {
            var go = new GameObject("SelectionManager");
            go.AddComponent<SelectionManager>();

            var unit1 = new GameObject("Unit1");
            var unit2 = new GameObject("Unit2");

            SelectionManager.Instance.SelectUnit(unit1);
            SelectionManager.Instance.AddToSelection(unit2);

            Assert.AreEqual(2, SelectionManager.Instance.SelectedCount);

            SelectionManager.Instance.ClearSelection();
            Assert.AreEqual(0, SelectionManager.Instance.SelectedCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(unit1);
            Object.DestroyImmediate(unit2);
        }
    }
}
