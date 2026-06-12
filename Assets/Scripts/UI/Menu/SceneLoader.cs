using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmCommander.UI
{
    public class SceneLoader : MonoBehaviour
    {
        public string sceneName;

        public void OnSceneLoad()
        {
            if (!string.IsNullOrEmpty(sceneName))
                SceneManager.LoadScene(sceneName);
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
