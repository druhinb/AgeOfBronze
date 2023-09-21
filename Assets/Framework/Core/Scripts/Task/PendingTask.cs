using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Task
{
    public struct PendingTask
    {
        public IPendingTaskEntityComponent sourceComponent;

        public bool playerCommand;

        public IEntityComponentTaskInput sourceTaskInput;
    }
}
