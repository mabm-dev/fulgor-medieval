Shader "Fulgor/ArtReview/ConnectedWater"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _WaveStrength("Suavidad de ondas", Range(0,0.08)) = 0.025
        _WaveScale("Escala de ondas", Float) = 18
        _WaveSpeed("Velocidad de ondas", Float) = 0.45
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _WaveStrength;
            float _WaveScale;
            float _WaveSpeed;
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _FORWARD_PLUS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 color:COLOR; float distance:TEXCOORD2; float fog:TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS; output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color; output.distance = input.uv.x;
                output.fog = ComputeFogFactor(output.positionCS.z); return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half water = saturate(input.color.a);
                float time = _Time.y * _WaveSpeed;
                float2 p = input.positionWS.xz;
                float3 normal = normalize(input.normalWS + water * _WaveStrength * float3(
                    sin(p.x * _WaveScale + p.y * 9 + time), 0,
                    cos(p.y * _WaveScale * .83 - p.x * 6 + time * .8)));
                half3 color = input.color.rgb * _BaseColor.rgb;
                half ripple = sin(p.x * 21 + sin(p.y * 13) + time) * cos(p.y * 17 - time * .7);
                color *= 1 + water * ripple * .025;
                half foam = (1 - smoothstep(.005, .045, abs(input.distance + .025))) * water;
                color = lerp(color, half3(.55,.66,.62), foam * (.06 + .035 * ripple));
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS; lighting.normalWS = normal;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(normal); lighting.fogCoord = input.fog;
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = color; surface.metallic = 0; surface.specular = half3(.04,.04,.04);
                surface.smoothness = lerp(.12, .72, water); surface.normalTS = half3(0,0,1);
                surface.occlusion = 1; surface.alpha = 1;
                half4 result = UniversalFragmentPBR(lighting, surface);
                result.rgb = MixFog(result.rgb, input.fog); return result;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            struct DepthAttributes { float4 positionOS:POSITION; };
            float4 DepthVert(DepthAttributes input):SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half4 DepthFrag():SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
