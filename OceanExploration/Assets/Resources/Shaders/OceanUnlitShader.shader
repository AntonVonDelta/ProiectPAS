Shader "Unlit/OceanUnlitShader"
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


			// https://gist.github.com/bgolus/a07ed65602c009d5e2f753826e8078a0
			float getRawDepth(float2 uv) { return SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0.0, 0.0)); }

			// inspired by keijiro's depth inverse projection
			// https://github.com/keijiro/DepthInverseProjection
			// constructs view space ray at the far clip plane from the screen uv
			// then multiplies that ray by the linear 01 depth
			float3 viewSpacePosAtScreenUV(float2 uv) {
				float3 viewSpaceRay = mul(unity_CameraInvProjection, float4(uv * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z);
				float rawDepth = getRawDepth(uv);
				return viewSpaceRay * Linear01Depth(rawDepth);
			}


			v2f vert(appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				//fixed4 depth_col = tex2D(_CameraDepthTexture, i.uv);
				//return col+ depth_col;

				// Sample the color texture
				fixed4 col = tex2D(_MainTex, i.uv);

				// Get view direction and position
				float3 viewPos = viewSpacePosAtScreenUV(i.uv);
				float3 worldPos = mul(unity_CameraToWorld, float4(viewPos.xy, -viewPos.z, 1.0)).xyz;


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
				float3 dir = normalize(worldPos);

				if (_WorldSpaceCameraPos.y >= ocean_surface) {
					return fixed4(0,0,0,0);
				}

				// Run only for skybox
				//if (depth>0.0f) {
					if (dir.y == 0) dir.y = 0.0001;

					// Make the y component to be of size 1
					dir = mul(dir, 1.0f/dir.y);

					// Multiply the "unit" y axis by the distance to the surface.
					// This will also extend the other components
					dir = mul(dir, ocean_surface - _WorldSpaceCameraPos.y);

					// pow for debug adjustments
					worldDepth = pow(length(dir),1);

					// For debugging
					fixed4 debug_color = fixed4(1,0,0,0);
					if (worldDepth < 5) {
						return debug_color * (worldDepth/5);
					}
					return debug_color;
				//}

				float fogVar = saturate(1.0 - (fog_end - worldDepth) / (fog_end - fog_start));
				return lerp(col, ocean_color, fogVar);
			}
			ENDCG
		}

	}
}
