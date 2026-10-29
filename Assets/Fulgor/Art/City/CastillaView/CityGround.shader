Shader "Fulgor/City/Ground"
{
    Properties{
        _BaseColor("Grass tint",Color)=(1,1,1,1)
        _SoilColor("Earth tint",Color)=(1,1,1,1)
        _GrassMap("Substance grass",2D)="white"{}
        _SoilMap("Substance earth",2D)="white"{}
        [Normal] _SoilNormal("Earth normal",2D)="bump"{}
        _SoilRoughness("Earth roughness",2D)="white"{}
        _GroundScale("Ground size",Float)=100
    }
    SubShader{
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass{
            Tags{"LightMode"="UniversalForward"}
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
            TEXTURE2D(_GrassMap);SAMPLER(sampler_GrassMap);TEXTURE2D(_SoilMap);SAMPLER(sampler_SoilMap);
            TEXTURE2D(_SoilNormal);SAMPLER(sampler_SoilNormal);TEXTURE2D(_SoilRoughness);SAMPLER(sampler_SoilRoughness);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor,_SoilColor;float _GroundScale;int _BuildingCount,_PathCount;float _MarketBuilt;
                float4 _BuildingFootprints[16],_StreetPaths[16];
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 ground:TEXCOORD2;};
            V Vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.ground=i.positionOS.xz*_GroundScale;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            float segmentDistance(float2 p,float2 a,float2 b){float2 v=b-a;return length(p-a-v*saturate(dot(p-a,v)/max(dot(v,v),.001)));}
            half4 Frag(V i):SV_Target{
                float2 p=i.ground;float broad=noise(p*.9),soil=noise(p*6.5);float edge=(noise(p*12)-.5)*.13;
                float road=1-smoothstep(.28+edge,.52+edge,segmentDistance(p,float2(0,-80),float2(0,-.3)));
                for(int j=0;j<min(_PathCount,16);j++)road=max(road,1-smoothstep(.18+edge,.36+edge,segmentDistance(p,_StreetPaths[j].xy,_StreetPaths[j].zw)));
                float plaza=(1-smoothstep(1.55,2.10,length((p+float2(0,2.2))*float2(1,.8))))*_MarketBuilt;
                float contact=0,footwear=0;
                for(int j=0;j<min(_BuildingCount,16);j++){
                    float4 box=_BuildingFootprints[j];float2 d=abs(p-box.xy)-box.zw;float distance=length(max(d,0));
                    contact=max(contact,1-smoothstep(.01,.14,distance));footwear=max(footwear,1-smoothstep(.02,.27+soil*.10,distance));
                }
                float dirt=saturate(max(road,max(plaza*.85,footwear*.85))+(broad-.5)*.15);
                half3 grass=SAMPLE_TEXTURE2D(_GrassMap,sampler_GrassMap,p*.42).rgb*_BaseColor.rgb;
                half3 earth=SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,p*2.3).rgb*_SoilColor.rgb;
                grass*=.96+noise(p*18)*.08;
                half3 color=lerp(grass,earth,dirt)*(.92+broad*.13);
                // Two worn wheel lines follow the entrance; weeds survive in the centre.
                float track=1-smoothstep(.025,.075,min(abs(p.x-.18),abs(p.x+.18)));
                track*=road*(1-smoothstep(-1.3,-.9,p.y));color*=1-track*.09;
                color*=1-contact*.17;
                float fade=smoothstep(8,24,length(p));color=lerp(color,half3(.23,.25,.22),fade*.58);
                half3 tangent=UnpackNormal(SAMPLE_TEXTURE2D(_SoilNormal,sampler_SoilNormal,p*2.3));
                half3 n=normalize(i.normalWS+half3(tangent.x,0,tangent.y)*(.18*dirt));
                InputData lighting=(InputData)0;lighting.positionWS=i.positionWS;lighting.normalWS=n;lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);lighting.shadowCoord=TransformWorldToShadowCoord(i.positionWS);lighting.bakedGI=SampleSH(n);lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);lighting.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0;s.albedo=color;s.specular=half3(.04,.04,.04);s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;s.smoothness=lerp(.035,1-SAMPLE_TEXTURE2D(_SoilRoughness,sampler_SoilRoughness,p*2.3).r,dirt);
                return UniversalFragmentPBR(lighting,s);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
