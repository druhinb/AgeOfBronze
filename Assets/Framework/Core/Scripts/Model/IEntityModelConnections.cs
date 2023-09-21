using System.Collections.Generic;

using UnityEngine;

namespace RTSEngine.Model
{
    public interface IEntityModelConnections
    {
        IReadOnlyList<Transform> ConnectedTransformChildren { get; }
        IReadOnlyList<Animator> ConnectedAnimatorChildren { get; }
        IReadOnlyList<Renderer> ConnectedRendererChildren { get; }
    }
}
