// ============================================================
// Background.shader  –  Arrow Slide Puzzle
// Vertical indigo gradient + radial vignette.
// Used by BackgroundRenderer.cs.
// ============================================================

Shader "ArrowPuzzle/Background"
{
    Properties
    {
        _ColorBottom  ("Gradient Bottom", Color) = (0.165, 0.039, 0.620, 1)
        _ColorTop     ("Gradient Top",    Color) = (0.231, 0.078, 0.784, 1)
        _VigStrength  ("Vignette Strength", Range(0,1)) = 0.45
        _VigSoftness  ("Vignette Softness", Range(0,2)) = 1.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorBottom;
            fixed4 _ColorTop;
            float  _VigStrength;
            float  _VigSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ── Vertical gradient (bottom → top) ──────────
                fixed4 col = lerp(_ColorBottom, _ColorTop, i.uv.y);

                // ── Radial vignette ───────────────────────────
                // Distance from centre (0.5, 0.5); max ~0.707 at corners
                float2 fromCentre = i.uv - 0.5;
                float  dist       = length(fromCentre) * 1.414; // normalise to [0,1]
                // Smooth darkening towards the edges
                float  vignette   = 1.0 - smoothstep(0.0, _VigSoftness, dist * _VigStrength * 1.5);
                col.rgb *= vignette;

                return col;
            }
            ENDCG
        }
    }

    // Fallback for older hardware
    FallBack "Diffuse"
}

