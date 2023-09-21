namespace RTSEngine.Service
{
    public interface IServicePublisher<T> : IMonoBehaviour
    {
        E GetService<E>() where E : T;
    }
}
