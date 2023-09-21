﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Upgrades
{
    [System.Serializable]
    public struct EntityUpgradeComponentMatcherElement
    {
        [EntityComponentCode()]
        public string sourceComponentCode;
        [EntityComponentCode(3, "upgradeTarget")]
        public string targetComponentCode;
    }
}
