using UnityEngine;

using RTSEngine.Attack;
using RTSEngine.Entities;

namespace RTSEngine.EntityComponent
{
    public class BuildingAttack : FactionEntityAttack
    {
        #region Attributes
        //'maxDistance' represents the attacking range for a building.
        public override AttackFormationSelector Formation => null;
        #endregion

        #region Engaging Target
        protected override bool MustStopProgress()
        {
            return base.MustStopProgress()
                || !IsTargetInRange(transform.position, Target)
                || LineOfSight.IsObstacleBlocked(transform.position, Target.instance.transform.position);
        }

        public override float GetProgressRange()
            => ProgressMaxDistance + Entity.Radius + Target.instance.Radius;

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            return Vector3.Distance(transform.position, RTSHelper.GetAttackTargetPosition(target)) <= ProgressMaxDistance + Entity.Radius + target.instance.Radius;
        }
        #endregion
    }
}
