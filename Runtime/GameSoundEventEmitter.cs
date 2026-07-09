using System;
using UnityEngine;

namespace GameSound.Unity
{
    [Serializable]
    public enum GameSoundEmitterTrigger
    {
        None,
        ObjectStart,
        ObjectEnable,
        ObjectDisable,
        ObjectDestroy,
        TriggerEnter,
        TriggerExit,
        TriggerEnter2D,
        TriggerExit2D,
        CollisionEnter,
        CollisionExit,
        CollisionEnter2D,
        CollisionExit2D,
        MouseEnter,
        MouseExit,
        MouseDown,
        MouseUp,
        Manual
    }

    [Serializable]
    public enum GameSoundStopMode
    {
        Immediate,
        FadeOut,
        None
    }

    [AddComponentMenu("GameSound/GameSound Event Emitter")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameSoundEventEmitter : GameSoundAudioSource
    {
    }
}
