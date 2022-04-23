Shader "Unlit/TestUnlitShader"
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                //float2 depth : TEXCOORD0;
            };

            sampler2D _CameraDepthTexture;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                //UNITY_TRANSFER_DEPTH(o.depth);

                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the color texture
                fixed4 col = tex2D(_MainTex, i.uv);

                fixed4 depth_col = tex2D(_CameraDepthTexture, i.uv);
                
                return col+ depth_col;
            }

            /*half4 frag(v2f i) : SV_Target{
                UNITY_OUTPUT_DEPTH(i.depth);
            }*/
            ENDCG
        }
    }
}
