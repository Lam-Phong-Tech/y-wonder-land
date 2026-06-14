Shader "Custom/StylizedWaterURP"
{
    Properties
    {
        _ShallowColor("Shallow Color (Màu vùng cạn)", Color) = (0.32, 0.8, 0.82, 0.7)
        _DeepColor("Deep Color (Màu vùng sâu)", Color) = (0.08, 0.4, 0.75, 0.95)
        _DepthDistance("Độ sâu để chuyển màu", Float) = 2.0
        
        _NormalMap("Normal Map (Bản đồ sóng)", 2D) = "bump" {}
        _NormalScale("Độ gồ ghề của sóng", Float) = 1.0
        _WaveScale("Kích thước vân sóng (Giảm để sóng to ra)", Float) = 0.1
        _WaveSpeed1("Tốc độ sóng 1", Vector) = (0.05, 0.05, 0, 0)
        _WaveSpeed2("Tốc độ sóng 2", Vector) = (-0.03, 0.04, 0, 0)
        
        _WaveHeight("Độ cao nhấp nhô 3D", Float) = 0.1
        _WaveFrequency("Độ dãn nhấp nhô", Float) = 1.0
        _FoamDistance("Độ dày bọt trắng ven bờ", Float) = 0.5
        
        _Smoothness("Độ bóng mặt nước", Range(0,1)) = 0.8
        _SpecularIntensity("Độ chói của nắng", Range(0, 5)) = 1.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 uv           : TEXCOORD1;
                float4 screenPos    : TEXCOORD4;
                float3 normalWS     : TEXCOORD5;
                float3 tangentWS    : TEXCOORD6;
                float3 bitangentWS  : TEXCOORD7;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _DepthDistance;
                float _NormalScale;
                float _WaveScale;
                float4 _WaveSpeed1;
                float4 _WaveSpeed2;
                float _WaveHeight;
                float _WaveFrequency;
                float _FoamDistance;
                float _Smoothness;
                float _SpecularIntensity;
            CBUFFER_END

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Vertex Animation (Làm mặt nước dập dềnh nhẹ)
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float wave = sin(posWS.x * _WaveFrequency + _Time.y * 2.0) * cos(posWS.z * _WaveFrequency + _Time.y * 2.0) * _WaveHeight;
                posWS.y += wave;

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = input.uv;
                
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calculate Depth (Tính toán độ sâu)
                float2 screenUv = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = SampleSceneDepth(screenUv);
                float linearSceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                float linearWaterDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                
                float depthDifference = linearSceneDepth - linearWaterDepth;
                float depthFactor = saturate(depthDifference / _DepthDistance);

                // 2. Base Color (Chuyển màu từ cạn sang sâu)
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

                // 3. Normal Map Panning (Tạo gợn sóng trôi liên tục)
                // Dùng positionWS.xz thay vì UV để khi Scale không bị giãn hình!
                float2 uv1 = input.positionWS.xz * _WaveScale + _Time.y * _WaveSpeed1.xy;
                float2 uv2 = input.positionWS.xz * _WaveScale + _Time.y * _WaveSpeed2.xy;
                
                half4 norm1 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1);
                half4 norm2 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2);
                
                float3 normalTS = UnpackNormalScale(norm1, _NormalScale);
                float3 normalTS2 = UnpackNormalScale(norm2, _NormalScale);
                
                // Blend normals
                float3 blendedNormalTS = normalize(float3(normalTS.xy + normalTS2.xy, normalTS.z * normalTS2.z));
                
                // Transform to World Space
                float3 normalWS = TransformTangentToWorld(blendedNormalTS, float3x3(input.tangentWS, input.bitangentWS, input.normalWS));
                normalWS = normalize(normalWS);

                // 4. Lighting (Phản chiếu ánh nắng mặt trời)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                // Blinn-Phong Specular
                float3 halfVector = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVector));
                float specularPower = exp2(10.0 * _Smoothness + 1.0);
                float specularTerm = pow(NdotH, specularPower) * _SpecularIntensity;
                
                half3 specularColor = mainLight.color * specularTerm;

                // 5. Foam line at intersection (Tạo vệt bọt trắng ven bờ)
                float foamFactor = 1.0 - saturate(depthDifference / _FoamDistance); // Phạm vi ven bờ
                foamFactor = pow(foamFactor, 2.0); // Làm mềm
                waterColor.rgb += foamFactor * 0.5; // Thêm màu trắng
                waterColor.a += foamFactor * 0.5; // Phần ven bờ đặc hơn để nhìn rõ bọt

                // Hợp nhất màu nước và ánh sáng
                half3 finalRGB = waterColor.rgb + specularColor;
                return half4(finalRGB, saturate(waterColor.a));
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/Diffuse"
}
