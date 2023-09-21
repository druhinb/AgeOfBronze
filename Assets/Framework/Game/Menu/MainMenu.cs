using RTSEngine.Scene;
using UnityEngine;

namespace RTSEngine.Demo
{
	public class MainMenu : MonoBehaviour {

        [SerializeField]
        private GameObject multiplayerButton = null;
        [SerializeField]
        private GameObject webGLMultiplayerMsg = null;
        [SerializeField]
        private GameObject exitButton = null;

        [SerializeField, Tooltip("Define properties for loading target scenes from this scene.")]
        private SceneLoader sceneLoader = new SceneLoader();

        [SerializeField]
        private int targetFrameRate = 60;

        private void Awake()
        {
            bool enabledMP = true;
#if UNITY_WEBGL
            enabledMP = false;
#endif

            multiplayerButton.SetActive(enabledMP);
            webGLMultiplayerMsg.SetActive(!enabledMP);
            exitButton.SetActive(enabledMP);

            Application.targetFrameRate = targetFrameRate;
        }

        public void LeaveGame ()
		{
			Application.Quit ();
		}

		public void LoadScene(string sceneName)
		{
            sceneLoader.LoadScene(sceneName, source: this);
		}
	}
}