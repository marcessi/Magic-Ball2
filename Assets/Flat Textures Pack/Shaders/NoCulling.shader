Shader "Custom/InsideOutShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off // <- Esto es lo importante: no descartar caras traseras
        ZWrite Off
        Lighting Off
        Pass
        {
            SetTexture [_MainTex] { combine texture }
        }
    }
}
