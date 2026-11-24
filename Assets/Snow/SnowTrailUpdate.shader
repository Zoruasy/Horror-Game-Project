// Interne shader: houdt de "footprint map" bij.
// Elke frame: oude waarde langzaam omlaag (sneeuw vult weer op) + nieuwe voetafdrukken erin stempelen.
// 0 = onaangeroerde sneeuw, 1 = volledig ingedeukt.
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
            float  _FadeAmount;   // hoeveel er deze frame "terugveert"
            float  _StampCount;   // aantal nieuwe voetafdrukken deze frame
            float4 _AreaSize;     // xy = grootte van het sneeuwgebied in meters
            float4 _StampA[MAX_STAMPS]; // xy = uv-positie, z = halve breedte (m), w = halve lengte (m)
            float4 _StampB[MAX_STAMPS]; // xy = kijkrichting (wereld XZ), z = diepte 0..1

            // Simpele schoenzool-vorm: grote ellips (voorvoet) + kleine ellips (hak).
            // p is in "voet-ruimte": x = zijwaarts, y = naar voren, ongeveer -1..1.
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
                    // afstand tot het midden van de voetafdruk, in meters
                    float2 d = (i.uv - _StampA[s].xy) * _AreaSize.xy;

                    // draai naar voet-ruimte
                    float2 f = _StampB[s].xy;          // vooruit
                    float2 r = float2(f.y, -f.x);      // rechts
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
