using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Minimap.Cameras
{
    public interface IMinimapCameraController : IPreRunGameService
    {
        Camera MinimapCamera { get; }
        Canvas MinimapCanvas { get; }

        bool IsMouseOverMinimap();
        bool WorldPointToLocalPointInMinimapCanvas(Vector3 worldPoint, out Vector3 localPoint, float height = 0);
        Vector2 WorldPointToScreenPoint(Vector3 targetPosition);
    }
}