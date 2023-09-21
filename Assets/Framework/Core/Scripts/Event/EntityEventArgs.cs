using System;

using RTSEngine.Entities;

namespace RTSEngine.Event
{
    public class EntityEventArgs<T> : EventArgs where T : IEntity
    {
        public T Entity { private set; get; }

        public EntityEventArgs(T entity)
        {
            this.Entity = entity;
        }
    }
}
