using RTSEngine.Cameras;
using RTSEngine.Game;
using RTSEngine.Service;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using System;
using UnityEngine;

namespace RTSEngine.Audio
{
    public class AudioListenerHandler : MonoBehaviour, IPostRunGameService
    {
        [SerializeField, Tooltip("Drag and drop the gameobject that includes the Audio Listener component to force it to stay on the base terrain level.")]
        private AudioListener audioListener = null;

        private RaycastHitter hitter;

        protected ITerrainManager terrainMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();

            if (!audioListener.IsValid())
                return;

            this.mainCameraController.CameraPositionUpdated += HandleCameraPositionUpdated;

            hitter = new RaycastHitter(terrainMgr.BaseTerrainLayerMask);
        }

        private void OnDestroy()
        {
            this.mainCameraController.CameraPositionUpdated -= HandleCameraPositionUpdated;
        }

        private void HandleCameraPositionUpdated(IMainCameraController sender, EventArgs args)
        {
            if (hitter.Hit(mainCameraController.ScreenPointToRay(new Vector2(Screen.width / 2.0f, Screen.height / 2.0f)), out RaycastHit hit))
                audioListener.transform.position = hit.point;
        }
    }
}
