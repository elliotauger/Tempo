Shader "Custom/FairwayStripes"
{
    Properties
    {
        _DarkColor ("Dark Stripe", Color) = (0.12, 0.38, 0.08, 1)
        _LightColor ("Light Stripe", Color) = (0.15, 0.45, 0.10, 1)
        _StripeWidth ("Stripe Width", Range(0.5, 10)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

        fixed4 _DarkColor;
        fixed4 _LightColor;
        float _StripeWidth;

        struct Input
        {
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            float stripe = sin(IN.worldPos.x / _StripeWidth * 6.2832) * 0.5 + 0.5;
            stripe = smoothstep(0.35, 0.65, stripe);
            fixed4 c = lerp(_DarkColor, _LightColor, stripe);
            o.Albedo = c.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
