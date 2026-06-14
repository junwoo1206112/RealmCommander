using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.Tests.PlayMode
{
    public class CommandPermissionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SelectionManager_PersistsAcrossFrames()
        {
            var go = new GameObject("SelectionManager");
            go.AddComponent<SelectionManager>();

            yield return null;

            var unitGo = new GameObject("TestUnit");
            unitGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            unitGo.AddComponent<Mirror.NetworkIdentity>();
            unitGo.AddComponent<Unit>();

            SelectionManager.Instance.SelectUnit(unitGo);
            yield return null;

            Assert.IsTrue(SelectionManager.Instance.IsUnitSelected(unitGo));
            Assert.AreEqual(1, SelectionManager.Instance.SelectedCount);

            SelectionManager.Instance.ClearSelection();
            yield return null;

            Assert.AreEqual(0, SelectionManager.Instance.SelectedCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(unitGo);
        }

        [UnityTest]
        public IEnumerator CommandManager_IssueMoveCommand_DoesNotThrow()
        {
            var go = new GameObject("CommandManager");
            go.AddComponent<CommandManager>();
            var unitGo = new GameObject("TestUnit");
            unitGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            unitGo.AddComponent<Mirror.NetworkIdentity>();
            unitGo.AddComponent<Unit>();

            SelectionManager.Instance.SelectUnit(unitGo);

            yield return null;

            Assert.DoesNotThrow(() =>
            {
                CommandManager.Instance.IssueMoveCommand(Vector3.forward);
            });

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(unitGo);
        }
    }
}
