
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Determinism;

namespace RTSEngine.Health
{
    public class DamageOverTimeHandler
    {
        private readonly IEntityHealth health;

        public int RemainingCycles { private set; get; }
        private TimeModifiedTimer cycleTimer;
        public float CurrCycleTime => cycleTimer.CurrValue;

        public DamageOverTimeData Data { get; }
        public int Damage { get; }
        public IEntity Source { get; }

        public DamageOverTimeHandler(IEntityHealth health, DamageOverTimeData newData, int damage, IEntity source, float initialCycleDuration = 0.0f)
        {
            cycleTimer = new TimeModifiedTimer(initialCycleDuration);
            RemainingCycles = newData.cycles;
            this.health = health;
            Data = newData;
            Damage = damage;
            Source = source;
        }

        public bool Update()
        {
            if (cycleTimer.ModifiedDecrease())
            {
                health.Add(new HealthUpdateArgs(-Damage, Source));

                cycleTimer.Reload(Data.cycleDuration);
                if (!Data.infinite)
                {
                    RemainingCycles--;
                    if (RemainingCycles <= 0)
                        return false;
                }
            }

            return true;
        }
    }
}
