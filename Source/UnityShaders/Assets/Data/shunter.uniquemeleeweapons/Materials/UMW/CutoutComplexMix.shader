// UMW/CutoutComplexMix: a drop-in replacement for vanilla Custom/CutoutComplex (RimWorld 1.6,
// Unity 2022.3.35f1) that MIXES its two mask tints instead of stacking them.
//
//   vanilla:  col = tex * lerp(1, one, m.r) * lerp(1, two, m.g)
//   here:     col = tex * (r*one + g*two + (1 - r - g)),  (r, g) = (m.r, m.g) / max(1, m.r + m.g)
//
// where one = vertexColour * _Color and two = _ColorTwo, on all four channels. Pure-red,
// pure-green and black mask texels give identical results in both (only one term is non-zero),
// so existing art renders unchanged; a texel carrying both red and green (a red->green
// crossfade, or a hard red|green edge once texture filtering blends it) now renders the weighted
// mix the art is painted for instead of a lighter product.
//
// Everything else mirrors the compiled vanilla shader (disassembled from resources.assets, "Custom
// shader"): property names and defaults, tags, pass state (SrcAlpha/OneMinusSrcAlpha, ZWrite On,
// ZTest LEqual, Cull Back), vertex inputs POSITION/COLOR/TEXCOORD0 and the clip(a - 0.5) cutoff.
// Colour one arrives as _Color on a normal draw but as the VERTEX colour on the static-atlas print
// path (Graphic.Print moves it there and sets _Color to white), so, as in vanilla, the two are
// multiplied together.
Shader "UMW/CutoutComplexMix"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
        _MaskTex ("Mask texture", 2D) = "black" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ColorTwo ("Color Two", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Lighting Off
        Cull Back
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UMWCutoutComplexMix.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.texcoord);
                fixed4 mask = tex2D(_MaskTex, i.texcoord);
                fixed4 col = UMW_Mix(tex, mask, i.color * _Color);
                clip(col.a - UMW_ALPHA_CUTOFF);
                return col;
            }
            ENDCG
        }
    }
}
