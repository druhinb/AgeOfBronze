using UnityEngine;

using RTSEngine.Utilities;

namespace RTSEngine.BuildingExtension
{
    public class BorderObject : PoolableObject, IBorderObject
    {
        #region Attributes
        [SerializeField, Tooltip("What parts of the border model will be colored with the faction colors?")]
        private ColoredRenderer[] coloredRenderers = new ColoredRenderer[0];

        [SerializeField, Tooltip("The height at which the border object will be created.")]
        private float height = 20.0f;

        [SerializeField, Tooltip("The border object's scale will be equal to the size (chosen in the Border component) multiplied by this value."), Min(0.0f)]
        private float sizeMultiplier = 2.0f;

        protected IBuildingManager buildingMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPoolableObjectInit()
        {
            this.buildingMgr = gameMgr.GetService<IBuildingManager>(); 
        }
        #endregion

        #region Spawning/Despawning 
        public void OnSpawn(BorderObjectSpawnInput input)
        {
            base.OnSpawn(input);

            Vector3 buildingPosition = input.border.Building.transform.position;
            transform.position = new Vector3(buildingPosition.x, height, buildingPosition.z);

            transform.SetParent(input.border.Building.transform, true);

            foreach (ColoredRenderer cr in coloredRenderers)
            {
                cr.UpdateColor(input.border.Building.SelectionColor);
                cr.renderer.sortingOrder = input.border.SortingOrder;
            }

            Vector3 nextScale = Vector3.one * input.border.Size * sizeMultiplier;
            transform.localScale = new Vector3 (nextScale.x, transform.localScale.y, nextScale.z);
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
