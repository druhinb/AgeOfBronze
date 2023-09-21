using RTSEngine.Event;
using System;
using UnityEngine;

namespace RTSEngine.Model
{
    public interface ICachedModel
    {
        IMonoBehaviour Source { get; }

        bool IsRenderering { get; }
        Vector2 Position2D { get; }
        Vector3 Center { get; }

        event CustomEventHandler<ICachedModel, EventArgs> CachedModelDisabled;

        bool Show();
        void OnCached();
    }
}