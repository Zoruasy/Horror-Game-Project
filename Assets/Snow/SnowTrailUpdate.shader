// Internal shader: maintains the "footprint map".
// Every frame: old values slowly go down (the snow fills back in) + new footprints are stamped in.
// 0 = untouched snow, 1 = fully pressed down.
Shader "Hidden/SnowTrailUpdate"
{
    Properties
    {
        _MainTex ("Previous", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_STAMPS 32

            sampler2D _MainTex;
            float  _FadeAmount;   // how much "springs back" this frame
            float  _StampCount;   // number of new footprints this frame
            float4 _AreaSize;     // xy = size of the snow area in meters
            float4 _StampA[MAX_STAMPS]; // xy = uv position, z = half width (m), w = half length (m)
            float4 _StampB[MAX_STAMPS]; // xy = facing direction (world XZ), z = depth 0..1

            // Simple shoe-sole shape: big ellipse (forefoot) + small ellipse (heel).
            // p is in "foot space": x = sideways, y = forward, roughly -1..1.
            float FootprintShape(float2 p)
            {
                float sole = length(float2(p.x / 1.0, (p.y - 0.25) / 0.70));
                float heel = length(float2(p.x / 0.8, (p.y + 0.60) / 0.38));
                float d = min(sole, heel);
                return 1.0 - smoothstep(0.75, 1.0, d);
            }

            float4 frag (v2f_img i) : SV_Target
            {
                float prev = tex2D(_MainTex, i.uv).r;
                float v = max(prev - _FadeAmount, 0.0);

                int count = (int)_StampCount;
                [loop]
                for (int s = 0; s < count; s++)
                {
                    // distance to the center of the footprint, in meters
                    float2 d = (i.uv - _StampA[s].xy) * _AreaSize.xy;

                    // rotate into foot space
                    float2 f = _StampB[s].xy;          // forward
                    float2 r = float2(f.y, -f.x);      // right
                    float2 local = float2(dot(d, r), dot(d, f));
                    local /= _StampA[s].zw;

                    v = max(v, FootprintShape(local) * _StampB[s].z);
                }

                return float4(v, 0, 0, 1);
            }
            ENDCG
        }
    }
}
