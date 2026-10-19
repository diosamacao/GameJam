using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class LaserEmitterMap : MonoBehaviour
    {
        sealed class Entry {public GameObject instance,prefab;public LaserEmitterTile tile;public Matrix4x4 matrix;}
        Tilemap map;
        readonly Dictionary<Vector3Int,Entry> instances=new Dictionary<Vector3Int,Entry>();
        readonly HashSet<Vector3Int> changedCells=new HashSet<Vector3Int>();
        readonly List<Vector3Int> pending=new List<Vector3Int>();
        void OnEnable(){map=GetComponent<Tilemap>();Tilemap.tilemapTileChanged+=Changed;Rebuild();}
        void Changed(Tilemap changed,Tilemap.SyncTile[] tiles){if(changed!=map)return;foreach(var tile in tiles)changedCells.Add(tile.position);}
        void FixedUpdate(){
            if(changedCells.Count==0)return;
            pending.Clear();pending.AddRange(changedCells);changedCells.Clear();
            foreach(var cell in pending)RefreshCell(cell);
        }
        // Explicit rescan preserves unchanged emitters, including their frozen beam state.
        public void Rebuild(){
            if(!map)map=GetComponent<Tilemap>();
            pending.Clear();pending.AddRange(instances.Keys);
            foreach(var cell in pending)RefreshCell(cell);
            foreach(var cell in map.cellBounds.allPositionsWithin)
                if(map.GetTile(cell) is LaserEmitterTile)RefreshCell(cell);
            changedCells.Clear();
        }
        void RefreshCell(Vector3Int cell){
            var tile=map.GetTile<LaserEmitterTile>(cell);
            instances.TryGetValue(cell,out var entry);
            if(!tile || !tile.emitterPrefab){Remove(cell,entry);return;}
            var matrix=map.GetTransformMatrix(cell)*tile.transform.inverse;
            if(entry!=null && entry.instance && entry.tile==tile && entry.prefab==tile.emitterPrefab){
                if(entry.matrix!=matrix){entry.matrix=matrix;Place(entry.instance,cell,matrix);}return;
            }
            Remove(cell,entry);
            var go=Instantiate(tile.emitterPrefab,transform);go.name="Laser_"+cell;Place(go,cell,matrix);
            foreach(var renderer in go.GetComponentsInChildren<SpriteRenderer>())renderer.enabled=false;
            instances[cell]=new Entry{instance=go,prefab=tile.emitterPrefab,tile=tile,matrix=matrix};
        }
        void Place(GameObject go,Vector3Int cell,Matrix4x4 brush){
            go.transform.localPosition=map.CellToLocalInterpolated((Vector3)cell+map.tileAnchor)+(Vector3)brush.GetColumn(3);
            go.transform.localRotation=brush.rotation;go.transform.localScale=brush.lossyScale;
        }
        void Remove(Vector3Int cell,Entry entry){if(entry!=null && entry.instance){entry.instance.SetActive(false);Destroy(entry.instance);}instances.Remove(cell);}
        void OnDisable(){Tilemap.tilemapTileChanged-=Changed;foreach(var entry in instances.Values)if(entry.instance){entry.instance.SetActive(false);Destroy(entry.instance);}instances.Clear();changedCells.Clear();pending.Clear();}
    }
}
