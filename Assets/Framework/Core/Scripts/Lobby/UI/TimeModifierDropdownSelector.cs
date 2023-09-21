using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;

namespace RTSEngine.Lobby.UI
{
    [System.Serializable]
    public class TimeModifierDropdownSelector : DropdownSelector<float> 
    {
        [SerializeField, Tooltip("Possible choices for the time modifier that the player can select from.")]
        private TimeModifierOption[] options = new TimeModifierOption[0];

        public IEnumerable<TimeModifierOption> Options => options;

        public TimeModifierDropdownSelector() : base(1.0f, "Time Modifier") { }

        public void Init(ILobbyManager lobbyMgr)
        {
            elementsDic.Clear();
            foreach (TimeModifierOption element in options)
                elementsDic.Add(elementsDic.Count, element.modifier);

            base.Init(options.Select(element => element.name), lobbyMgr);
        }
    }
}
