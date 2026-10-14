using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class LaserEmitterMap : MonoBehaviour
    {
        Tilemap map;bool dirty;
        readonly Dictionary<Vector3Int,GameObject> instances=new Dictionary<Vector3Int,GameObject>();
        void OnEnable(){map=GetComponent<Tilemap>();Tilemap.tilemapTileChanged+=Changed;dirty=true;}
        void Changed(Tilemap changed,Tilemap.SyncTile[] tiles){if(changed==map)dirty=true;}
        void FixedUpdate(){if(dirty)Rebuild();}
        public void Rebuild(){
            dirty=false;
            foreach(var instance in instances.Values)if(instance){instance.SetActive(false);Destroy(instance);}
            instances.Clear();
            foreach(var cell in map.cellBounds.allPositionsWithin){
                var tile=map.GetTile<LaserEmitterTile>(cell);if(!tile || !tile.emitterPrefab)continue;
                var go=Instantiate(tile.emitterPrefab,transform);go.name="Laser_"+cell;
                go.transform.localPosition=map.CellToLocalInterpolated((Vector3)cell+map.tileAnchor);
                // Strip the asset's visual rotation, retaining extra brush rotation/flip.
                var brush=map.GetTransformMatrix(cell)*tile.transform.inverse;
                go.transform.localPosition+=(Vector3)brush.GetColumn(3);
                go.transform.localRotation=brush.rotation;go.transform.localScale=brush.lossyScale;
                foreach(var renderer in go.GetComponentsInChildren<SpriteRenderer>())renderer.enabled=false;
                instances.Add(cell,go);
            }
        }
        void OnDisable(){Tilemap.tilemapTileChanged-=Changed;foreach(var go in instances.Values)if(go){go.SetActive(false);Destroy(go);}instances.Clear();}
    }
}
