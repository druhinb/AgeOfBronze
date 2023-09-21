using RTSEngine.Entities;
using RTSEngine.Game;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace RTSEngine.Model
{

    public abstract class ModelConnectionsBase<T> where T : Component
    {
        [SerializeField]
        private List<T> connectedChildren = new List<T>();
        public IReadOnlyList<T> ConnectedChildren => connectedChildren;

        public bool IsChildValid(int index, EntityModelConnections modelParent)
        {
            return index.IsValidIndex(connectedChildren)
                && connectedChildren[index].IsValid()
                && connectedChildren[index].transform.GetComponentInParent<EntityModelConnections>() == modelParent;
        }

        public bool IsChildValid(T child, EntityModelConnections modelParent)
        {
            return child.IsValid()
                && child.transform.GetComponentInParent<EntityModelConnections>() == modelParent;
        }

        public int Count => connectedChildren.Count;

        public ModelConnectionsBase()
        {

        }

        public ModelConnectionsBase(T[] children)
        {
            this.connectedChildren = children.ToList();
        }

        public T Get(int index)
        {
            return index.IsValidIndex(connectedChildren) ? connectedChildren[index] : null;
        }

        public int GetIndex(T child)
        {
            return connectedChildren.IndexOf(child);
        }
    }

    [System.Serializable]
    public class ModelTransformConnections : ModelConnectionsBase<Transform> {
        public ModelTransformConnections(Transform[] children) : base (children)
        {
        }

        public ModelTransformConnections()
        {
        }
    }

    [System.Serializable]
    public class ModelAnimatorConnections : ModelConnectionsBase<Animator> {
        public ModelAnimatorConnections(Animator[] children) : base (children)
        {
        }

        public ModelAnimatorConnections()
        {

        }
    }

    [System.Serializable]
    public class ModelRendererConnections : ModelConnectionsBase<Renderer> {
        public ModelRendererConnections(Renderer[] children) : base (children)
        {
        }

        public ModelRendererConnections()
        {

        }
    }

    public class EntityModelConnections : MonoBehaviour, IEntityModelConnections
    {
        [SerializeField, HideInInspector, Tooltip("The Transform elements, children of this model object, that are used by components in this Entity.")]
        private ModelTransformConnections transformConnections = new ModelTransformConnections();
        public ModelTransformConnections TransformConnections => transformConnections; 
        public IReadOnlyList<Transform> ConnectedTransformChildren => transformConnections.ConnectedChildren;

        [SerializeField, HideInInspector, Tooltip("The Animator elements, children of this model object, that are used by components in this Entity.")]
        private ModelAnimatorConnections animatorConnections = new ModelAnimatorConnections();
        public ModelAnimatorConnections AnimatorConnections => animatorConnections;
        public IReadOnlyList<Animator> ConnectedAnimatorChildren => animatorConnections.ConnectedChildren;

        [SerializeField, HideInInspector, Tooltip("The Renderer elements, children of this model object, that are used by components in this Entity.")]
        private ModelRendererConnections rendererConnections = new ModelRendererConnections();
        public ModelRendererConnections RendererConnections => rendererConnections;
        public IReadOnlyList<Renderer> ConnectedRendererChildren => rendererConnections.ConnectedChildren;

        public void Init (IGameManager gameMgr, IEntity entity)
        {
            bool modelParentIncluded = false;
            foreach (Transform childTransform in transformConnections.ConnectedChildren)
            {
                // If the model parent transform is already included in the transformConnections children then stop here
                // Else we want to add the parent transform so that it is tracked correctly (active status, rotation, position, etc..)
                if (childTransform == transform)
                    modelParentIncluded = true;
            }

            if (!modelParentIncluded)
                return;

            transformConnections = new ModelTransformConnections(
                transformConnections.ConnectedChildren.Append(transform).ToArray());
        }
    }
}
