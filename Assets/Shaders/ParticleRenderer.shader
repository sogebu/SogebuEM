Shader "myShader/ParticleRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct Particle {
                float3 velocity;
                float3 position;
                float4 color;
                float scale;
                float4 ParticlePositionWorld4;
                float4 ParticleVelocityWorld4;
            };

            struct appdata
            {
                float4 vertex : POSITION;
            };

            float4x4 Lplayer;
            float4x4 R;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            StructuredBuffer<Particle> _ParticleDataBuffer;

            v2f vert (appdata v, uint instanceId : SV_InstanceID)
            {
                Particle p = _ParticleDataBuffer[instanceId];

                v2f o;

                float3 pos = (v.vertex.xyz * p.scale) + p.position;
                o.vertex = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
                o.color = p.color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }

            ENDCG
        }
    }
}