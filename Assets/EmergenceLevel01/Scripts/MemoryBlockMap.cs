using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Tilemap),typeof(TilemapCollider2D))]
    public sealed class MemoryBlockMap : MonoBehaviour
    {
        sealed class Cell {public MemoryBlockTile tile;public Color color;public TileFlags flags;public Tile.ColliderType collider;public bool visible;}
        readonly Dictionary<Vector3Int,Cell> cells=new Dictionary<Vector3Int,Cell>();
        Tilemap map;bool dirty;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Tilemap.tilemapTileChanged-=Painted;Tilemap.tilemapTileChanged+=Painted;}
        static void Loaded(Scene scene,LoadSceneMode mode){foreach(var root in scene.GetRootGameObjects())foreach(var candidate in root.GetComponentsInChildren<Tilemap>(true)){foreach(var pos in candidate.cellBounds.allPositionsWithin)if(candidate.GetTile(pos) is MemoryBlockTile){Ensure(candidate);break;}}}
        static void Painted(Tilemap candidate,Tilemap.SyncTile[] tiles){if(!Application.isPlaying || !candidate || !candidate.gameObject.scene.IsValid())return;foreach(var item in tiles)if(candidate.GetTile(item.position) is MemoryBlockTile){Ensure(candidate);break;}}
        static void Ensure(Tilemap candidate){if(!candidate.GetComponent<MemoryBlockMap>())candidate.gameObject.AddComponent<MemoryBlockMap>();}
        void OnEnable(){map=GetComponent<Tilemap>();Tilemap.tilemapTileChanged+=Changed;RefreshCells();}
        void Changed(Tilemap candidate,Tilemap.SyncTile[] tiles){if(candidate!=map)return;foreach(var item in tiles)if(!cells.TryGetValue(item.position,out var old) || map.GetTile(item.position)!=old.tile){dirty=true;break;}}
        public void RefreshCells(){
            if(!map)map=GetComponent<Tilemap>();dirty=false;
            foreach(var pos in new List<Vector3Int>(cells.Keys))if(map.GetTile(pos)!=cells[pos].tile)cells.Remove(pos);
            foreach(var pos in map.cellBounds.allPositionsWithin){var tile=map.GetTile<MemoryBlockTile>(pos);if(!tile || cells.ContainsKey(pos))continue;
                var cell=new Cell{tile=tile,color=map.GetColor(pos),flags=map.GetTileFlags(pos),collider=map.GetColliderType(pos),visible=!tile.visibleInMemory};cells[pos]=cell;Apply(pos,cell);
            }
            RefreshCollision();Evaluate();
        }
        void Update(){if(dirty)RefreshCells();else Evaluate();}
        void FixedUpdate(){if(dirty)RefreshCells();else Evaluate();}
        void Evaluate(){bool changed=false;foreach(var pair in cells){var pos=pair.Key;var cell=pair.Value;if(map.GetTile(pos)!=cell.tile)continue;var center=map.GetCellCenterWorld(pos);
                if(EmotionResonance.InEnhancedRange(center,RobotEmotion.Sadness))continue;
                bool visible=EmotionResonance.InEnhancedRange(center,RobotEmotion.Memory)==cell.tile.visibleInMemory;
                if(cell.visible==visible)continue;cell.visible=visible;Apply(pos,cell);changed=true;
            }if(changed)RefreshCollision();}
        void Apply(Vector3Int pos,Cell cell){map.SetTileFlags(pos,cell.flags & ~TileFlags.LockColor);var color=cell.color;color.a=cell.visible?cell.color.a:0;map.SetColor(pos,color);map.SetColliderType(pos,cell.visible?Tile.ColliderType.Grid:Tile.ColliderType.None);}
        void RefreshCollision(){var collider=GetComponent<TilemapCollider2D>();if(collider)collider.ProcessTilemapChanges();}
        void OnDisable(){Tilemap.tilemapTileChanged-=Changed;if(!map)return;foreach(var pair in cells)if(map.GetTile(pair.Key)==pair.Value.tile){map.SetTileFlags(pair.Key,TileFlags.None);map.SetColor(pair.Key,pair.Value.color);map.SetColliderType(pair.Key,pair.Value.collider);map.SetTileFlags(pair.Key,pair.Value.flags);}cells.Clear();RefreshCollision();}
    }
}
