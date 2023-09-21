using UnityEngine;

namespace RTSEngine.Model
{
    public interface IModelChild
    {
        bool IsRenderering { get; }
    }

    public interface IModelChildTransform : IModelChild
    {
        bool IsActive { get; set; }
        // When enabled, the activity of the object will be stored in memory but not reflected in the actual active status of the gameObject itself
        bool IsActiveCached { get; }
        void SetActiveCached(bool isActiveCached, bool statePreCache);

        Vector3 LocalPosition { get; set; }
        Vector3 Position { get; set; }

        Quaternion LocalRotation { get; set; }
        Quaternion Rotation { get; set; }

        Vector3 LocalScale { get; set; }
    }

    public interface IModelChildAnimator : IModelChild
    {
        float Speed { get; set; }

        RuntimeAnimatorController Controller { get; set; }

        void SetBool(string name, bool value);
        bool GetBool(string name);
    }

    public interface IModelChildRenderer  : IModelChild
    {
        bool SetColor(int materialID, Color color);
        bool SetColor(int materialID, string propertyName, Color color);
    }
}