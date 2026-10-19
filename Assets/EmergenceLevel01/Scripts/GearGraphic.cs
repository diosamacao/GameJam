using UnityEngine;
using UnityEngine.UI;
namespace Emergence.Level01
{
    public sealed class GearGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh){
            vh.Clear();var center=rectTransform.rect.center;float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.48f;
            for(int i=0;i<32;i++){
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                float outer=(i%4==1 || i%4==2)?radius:radius*.76f;
                int start=vh.currentVertCount;
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.35f,color,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*outer,color,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*outer,color,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*.35f,color,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
