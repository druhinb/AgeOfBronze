using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Multiplayer.Event;
using RTSEngine.Multiplayer.Logging;
using RTSEngine.Multiplayer.Utilities;
using RTSEngine.Multiplayer.UI;

namespace RTSEngine.Multiplayer.Mirror
{
    public class MultiplayerManagerUI : MonoBehaviour, IMultiplayerManagerUI
    {
        #region Attributes
        [SerializeField, Tooltip("Canvas that has all of the UI elements as its child objects.")]
        private Canvas mainCanvas = null;

        [SerializeField, Tooltip("UI Panel that represents the main menu where the user is able to launch the lobby.")]
        private GameObject mainMenu = null;

        [SerializeField, Tooltip("UI Panel that represents the loading menu to the lobby menu.")]
        private GameObject loadingMenu = null;

        [SerializeField, Tooltip("UI Input Field used to allow the player to input the network address of their target lobby.")]
        private InputField addressInput = null;
        [SerializeField, Tooltip("UI Input Field used to allow the player to input the port of their target lobby.")]
        private InputField portInput = null;

        // Other components
        protected IMultiplayerManager multiplayerMgr { private set; get; }
        protected IMultiplayerLoggingService logger { private set; get; }
        protected IMultiplayePlayerMessageUIHandler playerMessageUIHandler { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IMultiplayerManager multiplayerMgr)
        {
            this.multiplayerMgr = multiplayerMgr;
            this.logger = multiplayerMgr.GetService<IMultiplayerLoggingService>();
            this.playerMessageUIHandler = multiplayerMgr.GetService<IMultiplayePlayerMessageUIHandler>(); 

            this.multiplayerMgr.MultiplayerStateUpdated += HandleMultiplayerStateUpdated;

            if (!logger.RequireValid(mainCanvas,
                $"[{GetType().Name}] The 'Main Canvas' field must be assigned!"))
                return;

            // LAN fields:
            if (!logger.RequireValid(addressInput,
                $"[{GetType().Name}] The 'Address Input' field must be assigned!"))
                return;
            if (!logger.RequireValid(portInput,
                $"[{GetType().Name}] The 'Port Input' field must be assigned!"))
                return;

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            this.multiplayerMgr.MultiplayerStateUpdated -= HandleMultiplayerStateUpdated;

            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }
        #endregion

        #region Handling Event: Multiplayer State Update
        private void HandleMultiplayerStateUpdated(IMultiplayerManager sender, MultiplayerStateEventArgs args)
        {
            mainCanvas.gameObject.SetActive(args.State == MultiplayerState.main || args.State == MultiplayerState.loadingLobby);

            if (mainMenu)
                mainMenu.gameObject.SetActive(args.State == MultiplayerState.main);

            if (loadingMenu)
                loadingMenu.gameObject.SetActive(args.State == MultiplayerState.loadingLobby);

            if(args.State != MultiplayerState.main && args.State != MultiplayerState.loadingLobby)
                playerMessageUIHandler.Message.Hide();
        }
        #endregion

        #region Server Access Data UI Handling 
        public void UpdateServerAccessDataUI ()
        {
            addressInput.text = multiplayerMgr.CurrServerAccessData.networkAddress;
            portInput.text = multiplayerMgr.CurrServerAccessData.port;
        }

        public void OnServerAccessDataChange()
        {
            // Attempt to update the LAN access data and use the return value as the updated values of both input fields in case the input was not accepted by the manager.
            multiplayerMgr.UpdateServerAccessData(new ServerAccessData
            {
                networkAddress = addressInput.text,
                port = portInput.text
            });

            UpdateServerAccessDataUI();
        }
        #endregion
    }
}
