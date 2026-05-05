Shader "Custom/VertexColorUnlit"
{
    // Unlit shader that renders per-vertex colours with alpha transparency.
    // Designed for procedural meshes (e.g. PlanetDonutFactionView) that drive
    // colour entirely through Mesh.SetColors().  No textures required.
    //
    // Compatible with URP + SRP Batcher.

    Properties
    {
        // A global tint; keep at (1,1,1,1) to show vertex colours unchanged.
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "VertexColorUnlit"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off          // Two-sided: flat ring is visible from either Z side

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return IN.color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
