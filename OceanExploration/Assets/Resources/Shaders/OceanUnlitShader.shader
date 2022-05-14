Shader "Unlit/OceanUnlitShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_OceanTex("Ocean Texture", 2D) = "white" {}
		_OceanSurface("Ocean Surface", float) = 20
		_OceanDeepColor("Ocean deep color", Color) = (0, 0.486,0.905,0)
		_OceanShallowColor("Ocean deep color", Color) = (0, 0.686,0.905,0)


		_NoiseScale("Noise scale", float) = 1
		_NoiseFrequency("Noise frequency", float) = 1
		_NoiseSpeed("Noise Speed", float) = 1
		_PixelOffset("Pixel Offset", float) = 0.005
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			Cull Off ZWrite Off ZTest Always

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "noiseSimplex.cginc"
				#define M_PI 3.14159265359f

				struct appdata {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				sampler2D _OceanTex;
				float _OceanSurface;
				fixed4 _OceanDeepColor, _OceanShallowColor;
				float _NoiseScale, _NoiseFrequency, _NoiseSpeed, _PixelOffset;

				// From the docs I can see this will be automatically populated with the depth texture
				// BUT remeber to tell it to do so...in UniversalRenderPipelineAsset check the Depth Texture in General
				sampler2D _CameraDepthTexture;
				sampler2D _MainTex;
				float4 _MainTex_ST;


				// https://gist.github.com/bgolus/a07ed65602c009d5e2f753826e8078a0
				float getRawDepth(float2 uv) { return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv); }

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
					// This is the pixel direction in the view space meaning the magnitude of the
					// vector does not extend over 1 (meaning the far plane)
					// All values are withing the camera frustum
					float3 viewPixelPos = viewSpacePosAtScreenUV(i.uv);
					// This is the pixel direction but in world space. This is invariant to camera position
					float3 worldPixelPos = mul(unity_CameraToWorld, float4(viewPixelPos.xy, -viewPixelPos.z, 1.0)).xyz;
					// This is the pixel direction relative to camera
					float3 localCameraPixelPos = worldPixelPos - _WorldSpaceCameraPos;

					// God response: https://answers.unity.com/questions/877170/render-scene-depth-to-a-texture.html
					float logarithmic_depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
					float depth = Linear01Depth(logarithmic_depth);
					float worldDepth = LinearEyeDepth(logarithmic_depth);	// Real z value away from camera
					float3 dir = normalize(localCameraPixelPos);

					// Constants
					float fog_start = 10;
					float fog_end = 30;
					float minimum_surface_fog = 0.0f;	// can't get a clear picture of the sky underwater

					// We are above water
					if (_WorldSpaceCameraPos.y >= _OceanSurface) {
						if (dir.y < 0) {
							dir = mul(dir, 1.0f / dir.y);
							dir = mul(dir, _OceanSurface - worldPixelPos.y);

							float underwater_depth = length(dir);
							//if (unitViewDistance>0.15f) return fixed4(1,0,0,0);

							// This checks if the object sampled is outside the water/above surface
							if (underwater_depth < 0) return col;

							// Superimpose fog
							fog_start = 0;
							fog_end = 10;
							float fog_var = saturate(1.0 - (fog_end - underwater_depth) / (fog_end - fog_start));
							fixed4 fog_color = lerp(_OceanShallowColor, _OceanDeepColor, fog_var);

							return fog_color;
						}

						return col;
					}
					return col;
				}

					//	// Run only for skybox and for upward vectors
					//	// Should not be run for downward vectors because we get a "band" at the horizon...also the 
					//	// surface is up not down
					//	if (depth==1.0f && dir.y>0) {
					//		float3 worldDir = normalize(worldPixelPos);

					//		if (dir.y == 0) dir.y = 0.0001;
					//		if (worldDir.y == 0) worldDir.y = 0.0001;

					//		// Make the y component to be of size 1
					//		dir = mul(dir, 1.0f/dir.y);
					//		worldDir = mul(worldDir, 1.0f / worldDir.y);

					//		// Multiply the "unit" y axis by the distance to the surface.
					//		// This will also extend the other components
					//		dir = mul(dir, _OceanSurface - _WorldSpaceCameraPos.y);
					//		worldDir = mul(worldDir, _OceanSurface);

					//		// Pixel position on ocean surface
					//		float2 oceanPos = worldDir.xz;

					//		// pow for debug adjustments
					//		worldDepth = pow(length(dir),1);

					//		// Get angle of horizontal plane pixel vector in order to get
					//		// angle with the surface normal
					//		float horizontal_angle = atan2(dir.y, sqrt(pow(dir.x,2) + pow(dir.z, 2)));
					//		float angle_from_normal = M_PI/2 - horizontal_angle;
					//		
					//		// Apply refraction in order to reduce light further from camera with larger angles from the normal
					//		float n = 1.332f;		// nWater/nAir refraction indexes
					//		// Calculate cos(Beta) where nAir*sin(beta)=nWater*sin(alpha=angle_from_normal)
					//		float cos_beta_squared = 1 - pow(n, 2) * pow(sin(angle_from_normal), 2);
					//		float transmitance = 0;
					//		if (cos_beta_squared >= 0) transmitance = sqrt(cos_beta_squared);

					//		// Surface texture sampling


					//		fixed4 transmitted_color= lerp(_OceanShallowColor, col, transmitance);

					//		// Superimpose fog
					//		float fog_var = saturate(1.0 - (fog_end - worldDepth) / (fog_end - fog_start));
					//		fixed4 fog_color = lerp(transmitted_color, _OceanDeepColor, fog_var);

					//		return fog_color;
					//	}

					//	// Superimpose fog
					//	float fog_var = saturate(1.0 - (fog_end - worldDepth) / (fog_end - fog_start));
					//	fixed4 fog_color = lerp(col, _OceanDeepColor, fog_var);

					//	return fog_color;
					//}
					ENDCG
				}

		}
}
