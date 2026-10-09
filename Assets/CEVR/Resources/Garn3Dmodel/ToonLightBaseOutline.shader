Shader "Lpk/LightModel/ToonLightBase"
{
    Properties
    {
        _BaseMap            ("Texture", 2D)                       = "white" {}
        
        [Space]
        _ShadowStep         ("ShadowStep", Range(0, 1))           = 0.5
        _ShadowStepSmooth   ("ShadowStepSmooth", Range(0, 1))     = 0.04
        
        [Space] 
        _SpecularStep       ("SpecularStep", Range(0, 1))         = 0.6
        _SpecularStepSmooth ("SpecularStepSmooth", Range(0, 1))   = 0.05
        [HDR]_SpecularColor ("SpecularColor", Color)              = (1,1,1,1)
        
        [Space]
        _RimStep            ("RimStep", Range(0, 1))              = 0.65
        _RimStepSmooth      ("RimStepSmooth",Range(0,1))          = 0.4
        _RimColor           ("RimColor", Color)                   = (1,1,1,1)
        
        [Space]   
        _OutlineWidth      ("OutlineWidth", Range(0.0, 1.0))      = 0.15
        _OutlineColor      ("OutlineColor", Color)                = (0.0, 0.0, 0.0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // Shared by all passes (keeps the SRP Batcher happy)
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float _ShadowStep;
            float _ShadowStepSmooth;
            float _SpecularStep;
            float _SpecularStepSmooth;
            float4 _SpecularColor;
            float _RimStep;
            float _RimStepSmooth;
            float4 _RimColor;
            float _OutlineWidth;
            float4 _OutlineColor;
        CBUFFER_END
        ENDHLSL
        
        Pass
        {
            Name "UniversalForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

            // Main light shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            // Additional lights (point / spot / extra directional)
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP          // Forward+
            #pragma multi_compile _ _ADDITIONAL_LIGHTS           // Forward
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile_fog
            #pragma multi_compile_instancing
             
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {     
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            }; 

            struct Varyings
            {
                float2 uv            : TEXCOORD0;
                float3 normalWS      : TEXCOORD1;
                float3 viewDirWS     : TEXCOORD2;
                float3 positionWS    : TEXCOORD3;
                float  fogCoord      : TEXCOORD4;
                float4 positionCS    : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                    
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            // Toon lighting for one light (main, additional, or extra directional)
            float3 ToonLighting(Light light, float3 N, float3 V, float3 albedo, out float3 specular)
            {
                float3 L = normalize(light.direction);
                float3 H = normalize(V + L);

                float NH = dot(N, H);
                float NL = dot(N, L) * 0.5 + 0.5;

                float specularNH = smoothstep(
                    (1 - _SpecularStep * 0.05) - _SpecularStepSmooth * 0.05,
                    (1 - _SpecularStep * 0.05) + _SpecularStepSmooth * 0.05,
                    NH);
                float shadowNL = smoothstep(_ShadowStep - _ShadowStepSmooth, _ShadowStep + _ShadowStepSmooth, NL);

                // distanceAttenuation = point/spot falloff (1 for directional)
                // shadowAttenuation   = realtime shadows
                float atten = light.distanceAttenuation * light.shadowAttenuation;

                specular = _SpecularColor.rgb * light.color * atten * shadowNL * specularNH;
                return light.color * albedo * atten * shadowNL;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float NV = dot(N, V);

                float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;

                // InputData is required by the Forward+ light loop
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                half4 shadowMask = half4(1, 1, 1, 1);

                // ---------- Main light ----------
                Light mainLight = GetMainLight(inputData.shadowCoord);

                float3 totalSpecular;
                float3 diffuse = ToonLighting(mainLight, N, V, albedo, totalSpecular);

                // ---------- Additional lights ----------
                #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP
                    uint pixelLightCount = GetAdditionalLightsCount();

                    // Forward+: extra directional lights are handled separately
                    #if USE_CLUSTER_LIGHT_LOOP
                    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        Light dirLight = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
                        float3 s;
                        diffuse += ToonLighting(dirLight, N, V, albedo, s);
                        totalSpecular += s;
                    }
                    #endif

                    // Point / spot lights
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
                        float3 addSpecular;
                        diffuse += ToonLighting(addLight, N, V, albedo, addSpecular);
                        totalSpecular += addSpecular;
                    LIGHT_LOOP_END
                #endif
                
                // ---------- Rim ----------
                float rim = smoothstep((1 - _RimStep) - _RimStepSmooth * 0.5, (1 - _RimStep) + _RimStepSmooth * 0.5, 0.5 - NV);
                
                // ---------- Ambient ----------
                float3 ambient = rim * _RimColor.rgb + SampleSH(N) * albedo;
            
                float3 finalColor = diffuse + ambient + totalSpecular;
                finalColor = MixFog(finalColor, input.fogCoord);
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        // Outline
        Pass
        {
            Name "Outline"
            Cull Front
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float  fogCoord : TEXCOORD0;    
            };
            
            v2f vert(appdata v)
            {
                v2f o;

                float4 posCS = TransformObjectToHClip(v.vertex.xyz);

                // Push the vertex along its normal in screen space,
                // so the outline has the same pixel width at any distance or object scale
                float3 normalWS = TransformObjectToWorldNormal(v.normal);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);
                float2 dir = normalCS.xy;
                dir = dir / max(length(dir), 0.0001);

                // Fix aspect ratio so the width is even horizontally and vertically
                dir.x *= _ScreenParams.y / _ScreenParams.x;

                // * posCS.w cancels perspective division -> constant screen width
                posCS.xy += dir * _OutlineWidth * 0.03 * posCS.w;

                o.pos = posCS;
                o.fogCoord = ComputeFogFactor(o.pos.z);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 finalColor = MixFog(_OutlineColor.rgb, i.fogCoord);
                return float4(finalColor, 1.0);
            }
            
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
