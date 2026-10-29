Shader "Fulgor/City/Surface"
{
    Properties
    {
        _BaseColor("Tint",Color)=(1,1,1,1)
        _DetailMap("Substance base colour",2D)="white"{}
        [Normal] _DetailNormal("Substance OpenGL normal",2D)="bump"{}
        _RoughnessMap("Substance roughness",2D)="white"{}
        _DetailScale("Repeats per metre",Float)=3
        _NormalStrength("Surface relief",Range(0,1))=.22
        _SurfaceKind("Plaster Stone Wood Tile Cloth",Float)=0
        _CityOrigin("City origin",Vector)=(0,0,0,0)
        _Smoothness("Fallback smoothness",Range(0,1))=.08
        _UseTexture("Use authored material",Float)=0
        _UseAtlas("Preserve fortress atlas",Float)=0
        _BaseMap("Fortress atlas",2D)="white"{}
        [Normal] _BumpMap("Fortress baked normal",2D)="bump"{}
        _MetallicGlossMap("Fortress metal and smoothness",2D)="black"{}
        _BumpScale("Baked relief",Float)=.7
        _HouseAO("Baked architectural occlusion",2D)="white"{}
        _UseHouseAO("Use architectural occlusion",Float)=0
        _UseVertexTint("Per piece variation",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            TEXTURE2D(_DetailMap);SAMPLER(sampler_DetailMap);
            TEXTURE2D(_DetailNormal);SAMPLER(sampler_DetailNormal);
            TEXTURE2D(_RoughnessMap);SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_MetallicGlossMap);SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_HouseAO);SAMPLER(sampler_HouseAO);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;float4 _CityOrigin;
                float _DetailScale,_SurfaceKind,_UseTexture,_UseAtlas,_UseHouseAO,_UseVertexTint;
                half _NormalStrength,_Smoothness,_BumpScale;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;half3 tint:TEXCOORD4;};
            V Vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.tangentWS=float4(TransformObjectToWorldDir(i.tangentOS.xyz),i.tangentOS.w*GetOddNegativeScale());o.uv=i.uv;o.tint=i.color.rgb;return o;}
            float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);}
            half4 Frag(V i):SV_Target
            {
                float3 p=i.positionWS-_CityOrigin.xyz,n=normalize(i.normalWS);
                float3 weights=pow(abs(n),6);weights/=max(dot(weights,1),.001);
                float3 q=p*_DetailScale;
                float2 ux=q.zy,uy=q.xz,uz=q.xy;
                half3 tx=SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,ux).rgb;
                half3 ty=SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,uy).rgb;
                half3 tz=SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,uz).rgb;
                half3 detail=tx*weights.x+ty*weights.y+tz*weights.z;
                half rough=SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,ux).r*weights.x+SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,uy).r*weights.y+SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,uz).r*weights.z;
                half3 nx=UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailNormal,ux));
                half3 ny=UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailNormal,uy));
                half3 nz=UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailNormal,uz));
                float3 delta=float3(0,nx.y,nx.x)*weights.x+float3(ny.x,0,ny.y)*weights.y+float3(nz.x,nz.y,0)*weights.z;
                half3 color=_BaseColor.rgb*lerp(half3(1,1,1),detail,_UseTexture);
                half metal=0,occlusion=1,smoothness=lerp(_Smoothness,1-rough,_UseTexture);
                float wear=(1-smoothstep(.02,.35,p.y))*(.55+.45*noise(p*float3(9,2,9)));
                if(_UseAtlas>.5){
                    half3 atlas=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                    half4 packed=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);
                    float maximum=max(atlas.r,max(atlas.g,atlas.b)),minimum=min(atlas.r,min(atlas.g,atlas.b));
                    float stone=(1-smoothstep(.20,.38,(maximum-minimum)/max(maximum,.001)))*smoothstep(.08,.2,maximum);
                    // Keep heraldry, joinery and individual masonry from the authored atlas.
                    color=atlas*_BaseColor.rgb*lerp(1,.90+dot(detail,half3(.3,.59,.11))*.30,stone);
                    metal=packed.r;smoothness=lerp(packed.a,1-rough,stone*.65);
                    half3 baked=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                    float3 t=normalize(i.tangentWS.xyz),b=cross(n,t)*i.tangentWS.w;
                    n=normalize(t*baked.x+b*baked.y+n*baked.z);delta*=stone;
                    wear*=stone;
                }
                color*=lerp(half3(1,1,1),half3(.68,.65,.55),wear*.35);
                color*=lerp(half3(1,1,1),i.tint*1.1,_UseVertexTint);
                occlusion=lerp(1,SAMPLE_TEXTURE2D(_HouseAO,sampler_HouseAO,i.uv).r,_UseHouseAO*.75);
                color*=lerp(.78,1,occlusion);
                if(_SurfaceKind<.5&&_UseAtlas<.5){float stain=noise(p*float3(14,1.1,14));color*=.94+noise(p*2.8)*.09;color=lerp(color,color*half3(.72,.70,.60),wear*.25*stain);}
                n=normalize(n+delta*_NormalStrength*_UseTexture);
                InputData lighting=(InputData)0;lighting.positionWS=i.positionWS;lighting.normalWS=n;lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);lighting.shadowCoord=TransformWorldToShadowCoord(i.positionWS);lighting.bakedGI=SampleSH(n);lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);lighting.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0;s.albedo=color;s.specular=half3(.04,.04,.04);s.normalTS=half3(0,0,1);s.metallic=metal;s.occlusion=occlusion;s.alpha=1;s.smoothness=saturate(smoothness);
                return UniversalFragmentPBR(lighting,s);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
