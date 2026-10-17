using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    public sealed class CyclicVanishingBlock : MonoBehaviour
    {
        [Min(0)] public float visibleSeconds=2;
        [Min(0)] public float initialDelay;
        EmotionBlock block;EmotionBlockMap blocks;Tilemap map;float timer;bool wasVanishing;
        sealed class CellTimer {public float elapsed;public bool wasVanishing;}
        readonly Dictionary<Vector3Int,CellTimer> timers=new Dictionary<Vector3Int,CellTimer>();
        void Awake(){map=GetComponent<Tilemap>();if(map){blocks=GetComponent<EmotionBlockMap>();if(!blocks)blocks=gameObject.AddComponent<EmotionBlockMap>();RefreshCells();}else{block=GetComponent<EmotionBlock>();if(!block){block=gameObject.AddComponent<EmotionBlock>();block.kind=EmotionBlockKind.Vanishing;}}timer=-initialDelay;}
        public void RefreshCells(){if(!map)map=GetComponent<Tilemap>();if(!map)return;timers.Clear();foreach(var cell in map.cellBounds.allPositionsWithin){var tile=map.GetTile(cell) as EmotionBlockTile;if(tile && tile.kind==EmotionBlockKind.Vanishing)timers[cell]=new CellTimer{elapsed=-initialDelay};}}
        void Update(){
            if(map){foreach(var pair in timers){var cell=pair.Key;var state=pair.Value;if(!map.HasTile(cell) || blocks.IsFrozenCell(cell))continue;if(blocks.IsVanishing(cell)){state.wasVanishing=true;continue;}if(state.wasVanishing){state.elapsed=0;state.wasVanishing=false;}state.elapsed+=Time.deltaTime;if(state.elapsed>=visibleSeconds && blocks.BeginVanish(cell)){state.elapsed=0;state.wasVanishing=true;}}return;}
            if(!block || block.Removed || LocalTimeTarget.IsStopped(this))return;
            if(block.IsVanishing){wasVanishing=true;return;}if(wasVanishing){timer=0;wasVanishing=false;}timer+=Time.deltaTime;
            if(timer>=visibleSeconds && block.BeginVanish()){timer=0;wasVanishing=true;}
        }
    }
}
