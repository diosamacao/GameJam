Shader "Emergence/DoctorPixel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Opaque silhouette cutoff", Range(0,1)) = 0.5
        _LogicalSize ("Pixel grid", Vector) = (32,48,0,0)
        _Pivot ("Foot pivot", Vector) = (0.5,0,0,0)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment PixelFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _LogicalSize, _Pivot;
            float _Cutoff;
            fixed4 PixelFrag(v2f input) : SV_Target
            {
                float2 grid=max(_LogicalSize.xy,float2(1,1));
                float2 uv=(floor((input.texcoord-_Pivot.xy)*grid)+0.5)/grid+_Pivot.xy;
                fixed4 color=SampleSpriteTexture(uv);
                clip(color.a-_Cutoff);
                return fixed4(color.rgb*input.color.rgb,input.color.a);
            }
            ENDCG
        }
    }
}
