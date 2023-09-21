using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.ResourceExtension;
using RTSEngine.Lobby.Logging;

namespace RTSEngine.Lobby.UI
{
    [System.Serializable]
    public class ResourceInputDropdownSelector : DropdownSelector<IEnumerable<ResourceTypeInput>> 
    {
        [System.Serializable]
        public struct Option 
        {
            public string name;
            public List<ResourceTypeInput> resources;
        }
        [SerializeField, Tooltip("Possible choices for the resources that the player can select from.")]
        private Option[] options = new Option[0];

        public ResourceInputDropdownSelector() : base(Enumerable.Empty<ResourceTypeInput>(), "Resources") { }

        public void Init(ILobbyManager lobbyMgr)
        {
            elementsDic.Clear();
            foreach (Option element in options)
            {
                if(element.resources.Select(resource => resource.type).Where(resource => resource.IsValid()).Distinct().Count() != element.resources.Count)
                {
                    lobbyMgr.GetService<ILobbyLoggingService>().LogError($"[InitialResourceSelector - {element.name}] Initial resource types either have invalid or duplicate elements assigned!");
                    return;
                }
                elementsDic.Add(elementsDic.Count, element.resources);
            }

            base.Init(options.Select(element => element.name), lobbyMgr);
        }
    }
}
