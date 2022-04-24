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

				o.view_dir =normalize(WorldSpaceViewDir(v.vertex));//  normalize(mul(unity_ObjectToWorld, v.vertex)); //

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

				// Constants
				float fog_start = 0;
				float fog_end = 10;

				// Red, Green, Blue
				fixed4 ocean_color = fixed4(0, 0.486,0.905,0);
				float ocean_surface = 20;
				
				if (depth >= 1.0f) {
					// Affect very far away regions like skybox
					float3 dir =normalize( i.view_dir);
					dir= mul(dir, ocean_surface - _WorldSpaceCameraPos.y);

					worldDepth = length(dir);
				}

				float fogVar = saturate(1.0 - (fog_end - worldDepth) / (fog_end - fog_start));

				if (worldDepth < 3.2015f) {
					return fixed4(0,0,0,0);
				}

				return lerp(col, ocean_color, fogVar);
				//return mul(ocean_color, 1-depth);
			}
			ENDCG
		}
	}
}
