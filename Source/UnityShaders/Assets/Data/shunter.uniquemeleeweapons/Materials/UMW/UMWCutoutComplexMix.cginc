// Shared by UMW/CutoutComplexMix and its UI twin: uniforms, the mask mix and the alpha cutoff.
// Both shaders must agree on the formula, or the info card would disagree with the map.
//
// Vanilla Custom/CutoutComplex (disassembled from resources.assets) computes, on all
// four channels,
//     col = tex * lerp(1, vertexColour * _Color, mask.r) * lerp(1, _ColorTwo, mask.g)
// so the vertex colour only ever enters through the red term (where mask.r is 0 it has no effect,
// on RGB or on alpha). We keep that shape and replace the product of the two lerps with a weighted
// mix. Mask blue and alpha are unused, as in vanilla, and the mask is sampled at the _MainTex UV
// (there is no _MaskTex_ST).
#ifndef UMW_CUTOUT_COMPLEX_MIX_INCLUDED
#define UMW_CUTOUT_COMPLEX_MIX_INCLUDED

// Vanilla's map cutoff is the literal -0.5 in `clip(col.a - 0.5)` (0xBF000000 in the bytecode):
// alpha below 0.5 is discarded, exactly 0.5 is kept. The UI twin has no cutoff at all.
#define UMW_ALPHA_CUTOFF 0.5

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _MaskTex;
fixed4 _Color;
fixed4 _ColorTwo;

// colourOne is vertexColour * _Color, matching where vanilla multiplies them in.
fixed4 UMW_Mix(fixed4 tex, fixed4 mask, fixed4 colourOne)
{
    // Red weights colour one, green weights colour two, the remainder stays untinted. Weights
    // summing to more than one are renormalised so an over-saturated texel can never brighten.
    float r = mask.r;
    float g = mask.g;
    float s = max(1.0, r + g);
    r /= s;
    g /= s;
    fixed4 tint = r * colourOne + g * _ColorTwo + (1.0 - r - g);
    return tex * tint;
}

#endif
