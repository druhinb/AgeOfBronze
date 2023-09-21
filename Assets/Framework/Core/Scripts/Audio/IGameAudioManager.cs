using RTSEngine.Entities;
using RTSEngine.Game;
using UnityEngine;

namespace RTSEngine.Audio
{
    public interface IGameAudioManager : IAudioManager, IPreRunGameService { 
        void PlaySFX(IEntity entity, AudioClip clip, bool loop = false);
        void PlaySFX(IEntity entity, AudioClipFetcher clip, bool loop = false);
    }
}