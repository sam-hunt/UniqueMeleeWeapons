// UMW/CutoutComplexMixUI: the UI twin of UMW/CutoutComplexMix, mirroring vanilla
// Custom/CutoutComplexUI. ShaderTypeDef.uiShaderPath points here, and Widgets.ThingIcon swaps it in
// through ShaderDatabase.TryGetUIShader for the info card, inventory and trade rows.
//
// Same mix as the map shader (shared UMWCutoutComplexMix.cginc) inside the structure of Unity's
// built-in Internal-GUITextureClip, which vanilla's UI twin follows too (disassembled from
// resources.assets): the _GUIClipTexture mask sampled through
// unity_GUIClipTextureMatrix so scrolling views clip the icon; _ManualTex2SRGB (an IMGUI global,
// set when the GUI renders into a linear-space target) converting the TEXTURE sample to gamma
// before any tinting; no alpha cutoff, only blending; ZWrite Off, ZTest Always, Cull Off; and two
// SubShaders that differ only in how alpha blends (the first writes additive alpha, the second is
// the plain fallback). GUI.color reaches us as the vertex colour, passed unhalved because the
// shader declares _MaskTex (GenUI.DrawTextureWithMaterial), and as in vanilla it only acts through
// the red term.
Shader "UMW/CutoutComplexMixUI"
{
    Properties
    {
        _MainTex ("Texture", any) = "white" {}
        _MaskTex ("Mask texture", 2D) = "black" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ColorTwo ("Color Two", Color) = (1,1,1,1)
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UMWCutoutComplexMix.cginc"

    sampler2D _GUIClipTexture;
    uniform bool _ManualTex2SRGB;
    uniform float4x4 unity_GUIClipTextureMatrix;

    struct appdata_t
    {
        float4 vertex : POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        float2 clipUV : TEXCOORD1;
    };

    v2f vert (appdata_t v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        float3 eyePos = UnityObjectToViewPos(v.vertex);
        o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
    }

    fixed4 frag (v2f i) : SV_Target
    {
        fixed4 tex = tex2D(_MainTex, i.texcoord);
        if (_ManualTex2SRGB)
            tex.rgb = LinearToGammaSpace(tex.rgb);
        fixed4 mask = tex2D(_MaskTex, i.texcoord);
        fixed4 col = UMW_Mix(tex, mask, i.color * _Color);
        col.a *= tex2D(_GUIClipTexture, i.clipUV).a;
        return col;
    }
    ENDCG

    SubShader
    {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha, One One
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            ENDCG
        }
    }

    SubShader
    {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            ENDCG
        }
    }
}
