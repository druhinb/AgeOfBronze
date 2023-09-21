using System;

using UnityEngine.UI;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Event;
using RTSEngine.Determinism;

namespace RTSEngine.UI
{
    public class TimeModifierUIHandler : MonoBehaviour, IPostRunGameService
    {
        #region Attributes
        [SerializeField, Tooltip("Define the potential time modifier options for this map. If no element is defined, the default time modifier will be added as the only element.")]
        private TimeModifierOption[] options = new TimeModifierOption[0];
        [SerializeField, Tooltip("When enabled and the game starts from a menu with a valid game build instance then the options defined above will overwritten by the options from the game build data.")]
        private bool overrideOptionsByGameData = true;

        [SerializeField, Tooltip("Default option index. If out of range of the above array, it would default to 0.")]
        private int currOptionID = -1;

        [SerializeField, Tooltip("UI Button object used to allow the player to go through the available time modifier options.")]
        private Button updateOptionButton = null;
        [SerializeField, Tooltip("UI Text object used to display the current time modifier label out of the above options.")]
        private Text optionLabelText = null;

        [SerializeField, Tooltip("Show a tooltip regarding changing the time modifier when hovering over the button?")]
        private bool showTooltip = true;

        //Services
        protected IGameLoggingService logger { private set; get; }
        protected ITimeModifier timeModifier { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.timeModifier = gameMgr.GetService<ITimeModifier>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            foreach(TimeModifierOption inputOption in options)
                if (inputOption.modifier <= 0.0f)
                {
                    logger.LogError("[TimeModifierUIHandler] The 'Options' fields includes one or more elements where the time modifier value is <= 0.0. This is not allowed!", source: this);
                    return;
                }

            if (!logger.RequireTrue(options.Length > 0,
              $"[{GetType().Name}] No Time Modifier UI options have been assigned. Time modifier will remain set to default value of {TimeModifier.CurrentModifier}",
              type: LoggingType.info))
            {
                options = new TimeModifierOption[]
                {
                    new TimeModifierOption
                    {
                        modifier = TimeModifier.CurrentModifier,
                        name = "default"
                    }
                };
            }

            timeModifier.ModifierUpdated += HandleTimeModifierUpdated;

            if(updateOptionButton)
                updateOptionButton.interactable = RTSHelper.IsMasterInstance();

            // As this is a post run game service, it is initialized after the ITimeModifier component so after the initial time modifier value is set.
            // In case the game started with a build instance with time modifier game data and we are okay with overriding the options defined here by the ones in the game build data
            if (overrideOptionsByGameData && gameMgr.CurrBuilder.IsValid())
                options = timeModifier.Options.values;
            else
                timeModifier.SetOptions(options, initialOptionID: 0);

            // Else, we attempt to see if the initial time modifier value falls on one of the defined options and assign it if so
            RefreshUI();

            // Else, we simply force one modifier from the options we defined in the inspector
            // In case, the current local player is not allowed to update the time
            if(RTSHelper.IsMasterInstance())
            {
                if (!currOptionID.IsValidIndex(options))
                    currOptionID = 0;
                SetTimeModifierOption(currOptionID);
            }
        }

        private void OnDestroy()
        {
            timeModifier.ModifierUpdated -= HandleTimeModifierUpdated;
        }
        #endregion

        #region Handling Button Click / Updating Time Modifier 
        public void OnButtonClick()
        {
            SetTimeModifierOption(currOptionID.GetNextIndex(options));
        }

        private void SetTimeModifierOption(int optionID)
        {
            if (!logger.RequireTrue(optionID.IsValidIndex(options)
                && timeModifier.SetModifier(options[optionID].modifier, playerCommand: false) == ErrorMessage.none,
              $"[{GetType().Name}] Unable to update the time modifier to option of index '{optionID}'"))
                return;
        }
        #endregion

        #region Handling Button Text
        private void RefreshUI()
        {
            for(int optionID = 0; optionID < options.Length; optionID++)
            {
                if(options[optionID].modifier == TimeModifier.CurrentModifier)
                {
                    currOptionID = optionID;
                    if (optionLabelText)
                        optionLabelText.text = $"*{TimeModifier.CurrentModifier}";
                    return;
                }    
            }
        }

        private void HandleTimeModifierUpdated(ITimeModifier sender, EventArgs args)
        {
            RefreshUI();
        }
        #endregion

        #region Handling Tooltip
        public void DisplayTaskTooltip()
        {
            if (showTooltip)
            {
                globalEvent.RaiseShowTooltipGlobal(
                    this,
                    new MessageEventArgs(MessageType.info, message: GetTooltipMessage()));
            }
        }

        protected virtual string GetTooltipMessage()
        {
            return "Change the simulation's speed.";
        }

        public void HideTaskTooltip ()
        {
            globalEvent.RaiseHideTooltipGlobal(this);
        }
        #endregion
    }
}
