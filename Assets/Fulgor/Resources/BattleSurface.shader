Shader "Fulgor/Battle/Surface"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) _Weathering ("Variación", Range(0,1)) = .025 _Origin ("Origen", Vector) = (0,0,0,0) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Weathering;float4 _Origin;
            struct vertexInput { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; };
            struct fragmentInput { float4 pos:SV_POSITION; fixed4 color:COLOR; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            fragmentInput vert(vertexInput v)
            {
                fragmentInput o; o.pos=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color*_Color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz-_Origin.xyz;return o;
            }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            fixed4 frag(fragmentInput i):SV_Target
            {
                float light=saturate(dot(normalize(i.normal),normalize(float3(-.5,1,-.45))));
                float variation=(noise(i.world.xz*3.5)-.5)*_Weathering+(noise(i.world.xz*65)-.5)*_Weathering*.45;
                return fixed4(i.color.rgb*(.55+.55*light+variation),1);
            }
            ENDCG
        }
    }
}
