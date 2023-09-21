using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Game;
using RTSEngine.Determinism;

namespace RTSEngine.UI
{
    public class FPSUIHandler : MonoBehaviour, IPostRunGameService
    {
        private int currFPS = 0;

        [SerializeField, Tooltip("Enable to display the current frame rate in the below UI Text")]
        private bool isActive = false;

        [SerializeField, Tooltip("UI Text to display the current FPS counter.")]
        private float fpsCounterPeriod = 0.1f;
        private TimeModifiedTimer fpsCounterTimer;

        [SerializeField, Tooltip("UI Text to display the current FPS counter.")]
        private Text fpsCounterText = null;

        public void Init(IGameManager gameMgr)
        {
            fpsCounterTimer = new TimeModifiedTimer(fpsCounterPeriod);

            if (!isActive && fpsCounterText.IsValid())
                fpsCounterText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isActive
                || !fpsCounterTimer.ModifiedDecrease())
                return;

            currFPS = (int)(1f / Time.unscaledDeltaTime);
            fpsCounterText.text = $"FPS: {currFPS}";

            fpsCounterTimer.Reload();
        }
    }
}
