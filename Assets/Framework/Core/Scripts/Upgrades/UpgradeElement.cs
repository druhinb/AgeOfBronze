﻿using RTSEngine.EntityComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Upgrades
{
    [Serializable]
    public struct UpgradeElement<T>
    {
        //the code that defines the source that gets upgraded.
        public string sourceCode;

        //the target that replaces the element with the above code.
        public T target;
    }
}
