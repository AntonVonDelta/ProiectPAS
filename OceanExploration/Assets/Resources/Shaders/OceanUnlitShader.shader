Shader "Unlit/OceanUnlitShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_OceanTex("Ocean Texture", 2D) = "white" {}
		_OceanSurface("Ocean Surface", float) = 20
		_OceanDeepColor("Ocean deep color", Color) = (0, 0.486,0.905,0)
		_OceanShallowColor("Ocean shallow color", Color) = (0, 0.686,0.905,0)

		_OceanUVScale("Ocean UV Scale", float) = 0.02
		_OceanWaveSpeed("Ocean wave speed", float) = 0.2

		_WaveNoiseScale("Wave Noise scale", float) = 1
		_WaveNoiseFrequency("Wave Noise frequency", float) = 1
		_WaveNoiseSpeed("Wave Noise Speed", float) = 1

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
				float _OceanUVScale, _OceanWaveSpeed;
				float _WaveNoiseScale, _WaveNoiseFrequency, _WaveNoiseSpeed;

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

				float noiseAtUV(float2 uv, float frequency, float speed) {
					float3 spos = float3(uv.x, uv.y, 0) * frequency;
					spos.z += _Time.x * speed;
					float noise = _NoiseScale * ((snoise(spos) + 1) / 2);
					return noise;
				}
				float2 valueToDirection(float val) {
					float2 direction = float2(cos(val * M_PI * 2), sin(val * M_PI * 2));
					return normalize(direction);
				}

				float2 texture1DMovement(float2 uv, float speed, float scale) {
					float2 time_offset = _Time.x * speed;
					return uv*scale + time_offset;
				}



				v2f vert(appdata v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target{
					// Sample the color texture
					fixed4 col = tex2D(_MainTex, i.uv);

					// Get view direction and position
					// This is the pixel direction in the view space meaning the magnitude of the
					// vector is between 0 and far plane. All 0 Z values are at the center of camera and not on the near plant
					// Not quite the distance to the object. Read https://forum.unity.com/threads/understanding-worldspaceviewdir-incorrect-weird-values.1272374/#post-8128955
					float3 viewPixelPos = viewSpacePosAtScreenUV(i.uv);
					// This is the pixel direction but in world space. This is invariant to camera position
					float3 worldPixelPos = mul(unity_CameraToWorld, float4(viewPixelPos.xy, -viewPixelPos.z, 1.0)).xyz;
					// This is the pixel direction relative to camera
					float3 localCameraPixelPos = worldPixelPos - _WorldSpaceCameraPos;

					// God response: https://answers.unity.com/questions/877170/render-scene-depth-to-a-texture.html
					float logarithmic_depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
					float depth = Linear01Depth(logarithmic_depth);
					float worldDepth = -viewPixelPos.z;	// Not quite z value away from camera. Z=0 means camera center
					float3 dir = normalize(localCameraPixelPos);

					// Constants
					float fog_start = 10;
					float fog_end = 30;
					float minimum_surface_fog = 0.0f;	// can't get a clear picture of the sky underwater

					// Make sure dir.y is not 0
					if (dir.y == 0) dir.y = 0.0001;

					// We are above water
					if (_WorldSpaceCameraPos.y >= _OceanSurface) {
						if (dir.y < 0) {
							// The same as length( mul( mul(dir,1/dir.y),_OceanSurface - worldPixelPos.y))
							float underwater_depth = (_OceanSurface - worldPixelPos.y) / (-dir.y);

							// This checks if the object sampled is outside the water/above surface
							if (underwater_depth < 0) return col;

							// Create white foam around objects near to the surface
							if (_OceanSurface - worldPixelPos.y < 0.1f) {
								float distance = _OceanSurface - worldPixelPos.y;
								float3 worldDir = normalize(worldPixelPos);

								if (worldDir.y == 0) worldDir.y = 0.0001;
								// Make the y component to be of size 1
								worldDir = mul(worldDir, 1.0f / worldDir.y);
								// Multiply the "unit" y axis by the distance to the surface.
								// This will also extend the other components
								worldDir = mul(worldDir, _OceanSurface);

								// Pixel position on ocean surface
								float2 oceanPos = worldDir.xz;

								float2 oceanTexUV = texture1DMovement(oceanPos, 3, 1);
								float noise = noiseAtUV(oceanTexUV, 3.0f, 20.0f);
								float cutoff = lerp();
								if (noise >0.9f) {
									return fixed4(1,1,1,0);
								}

							}

							// Superimpose fog
							fog_start = 0;
							fog_end = 20;
							float fog_var = saturate(1.0 - (fog_end - underwater_depth) / (fog_end - fog_start));
							fixed4 fog_color = lerp( _OceanShallowColor, _OceanDeepColor, fog_var);

							return fog_color;
						}

						return col;
					}

					// Run only for skybox and for upward vectors
					// Should not be run for downward vectors because we get a "band" at the horizon...also the 
					// surface is up not down
					if ((depth == 1.0f || worldPixelPos.y>= _OceanSurface) && dir.y > 0) {
						float3 worldDir = normalize(worldPixelPos);

						if (worldDir.y == 0) worldDir.y = 0.0001;
						// Make the y component to be of size 1
						worldDir = mul(worldDir, 1.0f / worldDir.y);
						// Multiply the "unit" y axis by the distance to the surface.
						// This will also extend the other components
						worldDir = mul(worldDir, _OceanSurface);

						// Pixel position on ocean surface
						float2 oceanPos = worldDir.xz;


						// Calculate distance to the surface
						// The same as length( mul( mul(dir,1/dir.y),_OceanSurface - worldPixelPos.y))
						float underwater_depth = (_OceanSurface - _WorldSpaceCameraPos.y) / (dir.y);

						// Get angle of horizontal plane pixel vector in order to get
						// angle with the surface normal
						float horizontal_angle = atan2(dir.y, sqrt(pow(dir.x,2) + pow(dir.z, 2)));
						float angle_from_normal = M_PI / 2 - horizontal_angle;

						// Apply refraction in order to reduce light further from camera with larger angles from the normal
						float n = 1.3f;		// nWater/nAir refraction indexes
						// Calculate cos(Beta) where nAir*sin(beta)=nWater*sin(alpha=angle_from_normal)
						float cos_beta_squared = 1 - pow(n, 2) * pow(sin(angle_from_normal), 2);
						float transmitance = 0.07;	// Have minimum sky lighting in the distance. Full opaque not realistic
						if (cos_beta_squared >= 0) transmitance += sqrt(cos_beta_squared);
						transmitance = saturate(transmitance);

						// Surface texture sampling
						float3 time_spos = float3(1, 1, 0) * _WaveNoiseFrequency;
						time_spos.z += _Time.x * _WaveNoiseSpeed;
						float time_noise = _WaveNoiseScale * ((snoise(time_spos * _WaveNoiseFrequency) + 1) / 2);
						float2 time_noiseDirection = normalize(float2(cos(time_noise * M_PI * 2), sin(time_noise * M_PI * 2)));


						float2 oceanTexUV = texture1DMovement(oceanPos, _OceanWaveSpeed, 0.01f + _OceanUVScale / 5 * underwater_depth);
						float2 oceanTexOppositeUV = texture1DMovement(oceanPos, -_OceanWaveSpeed, _OceanUVScale / 20 * underwater_depth);

						float2 noiseDirection = valueToDirection(noiseAtUV(oceanTexUV, _NoiseFrequency, _NoiseSpeed));
						float2 pixelUVCoords = oceanTexUV + noiseDirection * _PixelOffset;

						fixed4 wave_col1 = tex2D(_OceanTex, pixelUVCoords);
						fixed4 wave_col2 = tex2D(_OceanTex, oceanTexOppositeUV);
						fixed4 wave_col = (wave_col1 + wave_col2) / 2;

						// Apply waves texture to surface
						fixed4 ocean_color = col;
						if (wave_col.r > 0.2) ocean_color = _OceanShallowColor;
						if (wave_col.r > 0.4) ocean_color = fixed4(1, 1, 1, 0);

						// Calculate color based on transmitance
						fixed4 transmitted_color = lerp(_OceanShallowColor, ocean_color, transmitance);

						// Superimpose fog
						float fog_var = saturate(1.0 - (fog_end - underwater_depth) / (fog_end - fog_start));
						fixed4 fog_color = lerp(transmitted_color, _OceanDeepColor, fog_var);

						return fog_color;
					}

					// Superimpose fog
					float fog_var = saturate(1.0 - (fog_end - worldDepth) / (fog_end - fog_start));
					fixed4 fog_color = lerp(col, _OceanDeepColor, fog_var);

					return fog_color;
				}
			ENDCG
			}
		}
}
