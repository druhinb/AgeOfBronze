using UnityEngine;

using RTSEngine.Utilities;

namespace RTSEngine.Minimap.Icons
{
    public class MinimapIcon : PoolableObject, IMinimapIcon
    {
        #region Attributes
        [SerializeField, Tooltip("Renderer to color and use as the minimap icon.")]
        private ColoredRenderer colorRenderer = new ColoredRenderer();
        #endregion

        #region Initializing/Terminating
        protected override void OnPoolableObjectInit()
        {
            logger.RequireValid(colorRenderer.renderer, 
                $"[{GetType().Name}] A {typeof(Renderer).Name} component must be attached to the minimap icon!");
        }
        #endregion

        #region Spawning/Despawning
        public void OnSpawn(MinimapIconSpawnInput input)
        {
            base.OnSpawn(new PoolableObjectSpawnInput(
                parent: input.sourceEntity.transform,

                useLocalTransform: false,
                spawnPosition: new Vector3(input.sourceEntity.transform.position.x, input.height, input.sourceEntity.transform.position.z),
                spawnRotation: input.spawnRotation
            ));

            colorRenderer.UpdateColor(input.sourceEntity.SelectionColor);
        }
        #endregion
    }
}