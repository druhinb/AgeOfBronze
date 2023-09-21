namespace RTSEngine.Animation
{
    /// <summary>
    /// Defines all the possible states for an animator controller.
    /// </summary>
    public enum AnimatorState { 
        invalid = -1,

        idle,

        takeDamage, dead,

        moving, movingState,

        inProgress,
    } 
}
