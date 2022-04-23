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
			#pragma multi_compile_fog


			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				half fog : TEXCOORD1;
				float2 uv : TEXCOORD0;
			};

			// From the docs I can see this will be automatically populated with the depth texture
			// BUT remeber to tell it to do so...in UniversalRenderPipelineAsset check the Depth Texture in General
			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			uniform half4 unity_FogStart;
			uniform half4 unity_FogEnd;

			v2f vert(appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float diff = unity_FogEnd.x - unity_FogStart.x;
				float invDiff = 1.0f / diff;
				o.fog = clamp((unity_FogEnd.x - length(o.pos.xyz)) * invDiff, 0.0, 1.0);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				//fixed4 depth_col = tex2D(_CameraDepthTexture, i.uv);
				//return col+ depth_col;

				// Sample the color texture
				fixed4 col = tex2D(_MainTex, i.uv);
				
				// Test this one from here https://github.com/keijiro/unity-shaderfog-example/blob/master/Assets/Shaders/ShaderFog.shader
				//float zpos = mul(UNITY_MATRIX_MVP, v.vertex).z;

				// God response: https://answers.unity.com/questions/877170/render-scene-depth-to-a-texture.html
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
				float worldDepth = LinearEyeDepth(depth);	// Real z value away from camera
				depth = pow(Linear01Depth(depth), 1.0f);

				// Red, Green, Blue
				fixed4 ocean_color = fixed4(0, 0.486,0.905,0);

				float fogVar = saturate(1.0 - (10 - worldDepth) / (10 - 0));
				return lerp(col, ocean_color, fogVar);
				//return mul(ocean_color, 1-depth);
			}
			ENDCG
		}
	}
}
