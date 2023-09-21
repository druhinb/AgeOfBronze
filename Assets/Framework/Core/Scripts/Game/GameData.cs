﻿using RTSEngine.Determinism;
using RTSEngine.ResourceExtension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Game
{
    public struct GameData
    {
        public DefeatConditionType defeatCondition;

        public TimeModifierOptions timeModifierOptions;

        public IEnumerable<ResourceTypeInput> initialResources;

        public IEnumerable<int> factionSlotIndexSeed;
    }
}
