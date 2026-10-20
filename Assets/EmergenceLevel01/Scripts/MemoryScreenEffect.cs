using UnityEngine;
namespace Emergence.Level01
{
    // Retained for existing scene references. Emotion-entry whitening is removed.
    // White memory scene transitions are controlled separately by LevelFlowController.
    [DisallowMultipleComponent]
    public sealed class MemoryScreenEffect : MonoBehaviour
    {
        public float Opacity=>0;
    }
}
