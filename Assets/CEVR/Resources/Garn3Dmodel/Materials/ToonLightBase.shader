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
        _OutlineWidth       ("OutlineWidth", Range(0.0, 1.0))     = 0.15
        _OutlineColor       ("OutlineColor", Color)               = (0.0, 0.0, 0.0, 1)
        _OutlineScreenBlend ("Outline Constant Screen Width (0 = scales with model)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        // Shared code for all passes (Built-in Render Pipeline)
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        sampler2D _BaseMap;
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
        float _OutlineScreenBlend;

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv     : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            float4 pos      : SV_POSITION;
            float2 uv       : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float3 posWS    : TEXCOORD2;
            LIGHTING_COORDS(3, 4)
            UNITY_FOG_COORDS(5)
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert(appdata v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_OUTPUT(v2f, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
            o.normalWS = UnityObjectToWorldNormal(v.normal);
            o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;

            TRANSFER_VERTEX_TO_FRAGMENT(o);
            UNITY_TRANSFER_FOG(o, o.pos);
            return o;
        }

        // Toon lighting for a single light
        float3 ToonLight(float3 N, float3 V, float3 L, float3 lightColor, float atten, float3 albedo, out float3 specular)
        {
            float3 H = normalize(V + L);
            float NH = dot(N, H);
            float NL = dot(N, L) * 0.5 + 0.5;

            float specularNH = smoothstep(
                (1 - _SpecularStep * 0.05) - _SpecularStepSmooth * 0.05,
                (1 - _SpecularStep * 0.05) + _SpecularStepSmooth * 0.05,
                NH);
            float shadowNL = smoothstep(_ShadowStep - _ShadowStepSmooth, _ShadowStep + _ShadowStepSmooth, NL);

            specular = _SpecularColor.rgb * lightColor * atten * shadowNL * specularNH;
            return lightColor * albedo * atten * shadowNL;
        }
        ENDCG

        // ------------------------------------------------------------
        // Pass 1: main directional light + ambient + rim
        // ------------------------------------------------------------
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBase
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            fixed4 fragBase(v2f i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(UnityWorldSpaceViewDir(i.posWS));
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float NV = dot(N, V);

                float3 albedo = tex2D(_BaseMap, i.uv).rgb;

                UNITY_LIGHT_ATTENUATION(atten, i, i.posWS);

                float3 specular;
                float3 diffuse = ToonLight(N, V, L, _LightColor0.rgb, atten, albedo, specular);

                float rim = smoothstep((1 - _RimStep) - _RimStepSmooth * 0.5, (1 - _RimStep) + _RimStepSmooth * 0.5, 0.5 - NV);

                float3 ambient = rim * _RimColor.rgb + ShadeSH9(float4(N, 1)) * albedo;

                fixed4 col = fixed4(diffuse + ambient + specular, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        // ------------------------------------------------------------
        // Pass 2: additional lights (point / spot / extra directional)
        // ------------------------------------------------------------
        Pass
        {
            Name "FORWARD_ADD"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAdd
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog

            fixed4 fragAdd(v2f i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(UnityWorldSpaceViewDir(i.posWS));
                float3 L = normalize(UnityWorldSpaceLightDir(i.posWS));

                float3 albedo = tex2D(_BaseMap, i.uv).rgb;

                UNITY_LIGHT_ATTENUATION(atten, i, i.posWS);

                float3 specular;
                float3 diffuse = ToonLight(N, V, L, _LightColor0.rgb, atten, albedo, specular);

                fixed4 col = fixed4(diffuse + specular, 1);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0));
                return col;
            }
            ENDCG
        }

        // ------------------------------------------------------------
        // Pass 3: outline (constant screen-space width)
        // ------------------------------------------------------------
        Pass
        {
            Name "OUTLINE"
            Cull Front

            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma multi_compile_fog

            struct v2fOutline
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2fOutline vertOutline(appdata v)
            {
                v2fOutline o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2fOutline, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 normalWS = normalize(UnityObjectToWorldNormal(v.normal));

                // Distance of this vertex from the camera (clip-space w)
                float4 posCS = mul(UNITY_MATRIX_VP, float4(posWS, 1.0));

                // Default: fixed width in world units, so the outline keeps the
                // same proportion to the model at any distance.
                // _OutlineScreenBlend > 0 mixes in constant on-screen width
                // (matches the world-space width at about 5 meters).
                float widthScale = lerp(1.0, posCS.w * 0.2, _OutlineScreenBlend);

                posWS += normalWS * _OutlineWidth * 0.1 * widthScale;

                o.pos = mul(UNITY_MATRIX_VP, float4(posWS, 1.0));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 fragOutline(v2fOutline i) : SV_Target
            {
                fixed4 col = fixed4(_OutlineColor.rgb, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        // Shadow casting
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
