using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Minimap.Cameras;
using RTSEngine.Utilities;

namespace RTSEngine.BuildingExtension
{
    public class BorderMinimapUIObject : PoolableObject, IBorderObject
    {
        #region Attributes
        [SerializeField, Tooltip("The UI Image component to color with the faction's color that owns the border object.")]
        private Image image = null;
        // to handle sorting order of border objects.
        private Canvas canvas;

        [SerializeField, Tooltip("The height at which the border object will be created.")]
        private float height = 20.0f;

        protected IMinimapCameraController minimapCameraController { get; private set; }
        protected IBuildingManager buildingMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnPoolableObjectInit()
        {
            if (!image.IsValid())
            {
                logger.LogError("[BorderMinimapUIObject] The 'Image' prefab must be assigned!", source: this);
                return;
            }

            this.canvas = GetComponent<Canvas>();

            this.minimapCameraController = gameMgr.GetService<IMinimapCameraController>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>(); 
        }
        #endregion

        #region Spawning/Despawning
        public void OnSpawn(BorderObjectSpawnInput input)
        {
            base.OnSpawn(input);

            minimapCameraController.WorldPointToLocalPointInMinimapCanvas(
                input.border.Building.transform.position, out Vector3 spawnPosition, height);

            transform.SetParent(minimapCameraController.MinimapCanvas.transform, true);

            transform.localPosition = spawnPosition;
            transform.localRotation = Quaternion.identity;

            // To get the distance between the center of the border and its circle edge from the real world to the canvas here
            // We get a random point on the circle edge of the border in world measure then convert that point's position to the local position
            // And finally calculate the distance between both points on the canvas to get the size of this UI element.
            Vector2 randomPointInCircle = Random.insideUnitCircle;
            randomPointInCircle.Normalize();
            randomPointInCircle *= input.border.Size * 0.25f;

            Vector3 randomEdgePoint = input.border.Building.transform.position
                + new Vector3(randomPointInCircle.x, input.border.Building.transform.position.y, randomPointInCircle.y);
            minimapCameraController.WorldPointToLocalPointInMinimapCanvas(
                randomEdgePoint, out Vector3 edgePosition, height);

            float distance = Vector3.Distance(edgePosition, spawnPosition);
            transform.localScale = distance * Vector3.one;

            float lastAlpha = image.color.a;
            Color nextColor = input.border.Building.SelectionColor;
            nextColor.a = lastAlpha;
            image.color = nextColor;

            if (canvas.IsValid())
                canvas.sortingOrder = input.border.SortingOrder;
        }

        public void Despawn()
        {
            // Make sure it has no parent object anymore.
            transform.SetParent(null, true);
            gameObject.SetActive(false);

            buildingMgr.Despawn(this);
        }
        #endregion
    }
}
