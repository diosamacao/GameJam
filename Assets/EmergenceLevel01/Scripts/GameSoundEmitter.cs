using UnityEngine;
namespace Emergence.Level01
{
    public sealed class GameSoundEmitter : MonoBehaviour
    {
        public GameSound sound=GameSound.AssemblyPlace;
        public void Play(){GameAudio.Play(sound,transform.position);}
    }
}
