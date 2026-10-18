using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Memory Block Tile")]
    public sealed class MemoryBlockTile : Tile
    {
        [Tooltip("开启：默认隐藏，增强怀念中显现；关闭：默认显现，增强怀念中隐藏。")]
        public bool visibleInMemory=true;
    }
}
