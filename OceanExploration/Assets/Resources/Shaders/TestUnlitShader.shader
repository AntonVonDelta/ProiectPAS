// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

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
				float3 view_dir : TEXCOORD1;
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

				o.view_dir =WorldSpaceViewDir(v.vertex);
				//o.view_dir = normalize(mul(unity_ObjectToWorld, v.vertex));
				//o.view_dir = normalize(ObjSpaceViewDir(v.vertex));

				//float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
				//o.view_dir = worldPos;// -_WorldSpaceCameraPos;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				//fixed4 depth_col = tex2D(_CameraDepthTexture, i.uv);
				//return col+ depth_col;

				// Sample the color texture
				fixed4 col = tex2D(_MainTex, i.uv);

				// God response: https://answers.unity.com/questions/877170/render-scene-depth-to-a-texture.html
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
				float worldDepth = LinearEyeDepth(depth);	// Real z value away from camera
				depth = pow(Linear01Depth(depth), 1.0f);

				// Constants
				float fog_start = 0;
				float fog_end = 10;

				// Red, Green, Blue
				fixed4 ocean_color = fixed4(0, 0.486,0.905,0);
				float ocean_surface = 20;
				
				if (depth >= 1.0f) {
					// Affect very far away regions like skybox
					// Here dir.z is actually 0
					float3 dir = normalize( i.view_dir);

					if (dir.y == 0) dir.y = 0.0001;

					// Make the y component to be of size 1
					dir = mul(dir, 1.0f/dir.y);
					dir = mul(dir, ocean_surface - _WorldSpaceCameraPos.y);

					// pow for debug adjustments
					worldDepth = pow(length(dir),1);

					// For debugging
					fixed4 debug_color = fixed4(1,0,0,0);
					if (worldDepth < 5) {
						return debug_color * (worldDepth/5);
					}
					return debug_color;
				}
				float fogVar = saturate(1.0 - (fog_end - worldDepth) / (fog_end - fog_start));

				return lerp(col, ocean_color, fogVar);
			}
			ENDCG
		}
	}
}
