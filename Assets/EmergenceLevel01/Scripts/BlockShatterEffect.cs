using UnityEngine;
namespace Emergence.Level01
{
    public sealed class BlockShatterEffect : MonoBehaviour
    {
        SpriteRenderer[] pieces;Sprite[] sprites;Vector3[] velocities;float age;Color tint;
        const float Lifetime=.55f;
        public static void Spawn(Sprite sprite,Matrix4x4 matrix,Material material,int layer,int order,Color color)
        {
            if(!sprite)return;
            var root=new GameObject("BlockShatter");var effect=root.AddComponent<BlockShatterEffect>();effect.tint=color;
            effect.pieces=new SpriteRenderer[16];effect.sprites=new Sprite[16];effect.velocities=new Vector3[16];
            var right=matrix.MultiplyVector(Vector3.right);var up=matrix.MultiplyVector(Vector3.up);
            float angle=Mathf.Atan2(right.y,right.x)*Mathf.Rad2Deg;
            for(int y=0;y<4;y++)for(int x=0;x<4;x++){
                int i=y*4+x;float w=sprite.rect.width/4,h=sprite.rect.height/4;
                var part=Sprite.Create(sprite.texture,new Rect(sprite.rect.x+x*w,sprite.rect.y+y*h,w,h),new Vector2(.5f,.5f),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
                effect.sprites[i]=part;var go=new GameObject("Shard");go.transform.SetParent(root.transform,false);
                var local=new Vector3(((x+.5f)*w-sprite.pivot.x)/sprite.pixelsPerUnit,((y+.5f)*h-sprite.pivot.y)/sprite.pixelsPerUnit,0);
                go.transform.position=matrix.MultiplyPoint3x4(local);go.transform.rotation=Quaternion.Euler(0,0,angle);go.transform.localScale=new Vector3(right.magnitude,up.magnitude,1);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=part;renderer.sharedMaterial=material;renderer.sortingLayerID=layer;renderer.sortingOrder=order+1;renderer.color=new Color(1,.6f,.5f,color.a);effect.pieces[i]=renderer;
                effect.velocities[i]=matrix.MultiplyVector(new Vector3((x-1.5f)*1.5f,(y-1.5f)*1.3f+1.4f,0));
            }
        }
        void Update(){
            age+=Time.deltaTime;if(age>=Lifetime){Destroy(gameObject);return;}
            for(int i=0;i<pieces.Length;i++){
                velocities[i]+=Vector3.down*(7*Time.deltaTime);pieces[i].transform.position+=velocities[i]*Time.deltaTime;
                pieces[i].transform.Rotate(0,0,(i%2==0?1:-1)*180*Time.deltaTime);
                var c=Color.Lerp(new Color(1,.6f,.5f),tint,Mathf.Clamp01(age/.08f));c.a=tint.a*(1-age/Lifetime);pieces[i].color=c;
            }
        }
        void OnDestroy(){if(sprites!=null)foreach(var sprite in sprites)if(sprite)Destroy(sprite);}
    }
}
