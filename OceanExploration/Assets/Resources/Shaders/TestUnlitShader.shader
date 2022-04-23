// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/TestUnlitShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			// From the docs I can see this will be automatically populated with the depth texture
			// BUT remeber to tell it to do so...in UniversalRenderPipelineAsset check the Depth Texture in General
			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				// sample the color texture
				/*fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 depth_col = tex2D(_CameraDepthTexture, i.uv);
				return col+ depth_col;*/

				// God response: https://answers.unity.com/questions/877170/render-scene-depth-to-a-texture.html
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
				depth = pow(Linear01Depth(depth), 0.5f);
				return depth;
			}
			ENDCG
		}
	}
}
