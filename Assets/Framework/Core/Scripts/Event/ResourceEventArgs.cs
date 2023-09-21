using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using RTSEngine.ResourceExtension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Event
{
    public struct ResourceEventArgs
    {
        public IResource Resource { get; }
        public ResourceTypeInfo ResourceType { get; }
    
        public ResourceEventArgs(IResource resource, ResourceTypeInfo resourceType = null)
        {
            this.Resource = resource;
            if (resourceType.IsValid())
                this.ResourceType = resourceType;
            else
                this.ResourceType = this.Resource.IsValid() ? this.Resource.ResourceType : null;
        }
    }
}
