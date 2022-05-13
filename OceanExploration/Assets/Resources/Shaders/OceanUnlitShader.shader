Shader "Unlit/OceanUnlitShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_NoiseScale("Noise scale", float)=1
		_NoiseFrequency ("Noise frequency", float) = 1
		_NoiseSpeed("Noise Speed", float)=1
		_PixelOffset("Pixel Offset", float)=0.005
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

			uniform float _NoiseScale, _NoiseFrequency, _NoiseSpeed, _PixelOffset;

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
				// This is the pixel direction but in world space. This is invariant to camera position or rotation
				float3 worldPixelPos = mul(unity_CameraToWorld, float4(viewPixelPos.xy, -viewPixelPos.z, 1.0)).xyz;
				// This is the pixel direction relative to camera
				float3 localCameraPixelPos = worldPixelPos - _WorldSpaceCameraPos;

				// God response: https://answers.unity.com/questions/877170/render-scene-depth-to-a-texture.html
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
				float worldDepth = LinearEyeDepth(depth);	// Real z value away from camera
				depth= pow(Linear01Depth(depth), 1.0f);
				float3 dir = normalize(localCameraPixelPos);

				// Constants
				float fog_start = 10;
				float fog_end = 20;
				float minimum_surface_fog = 0.0f;	// can't get a clear picture of the sky underwater

				// Red, Green, Blue
				fixed4 ocean_color = fixed4(0, 0.486,0.905,0);
				float ocean_surface = 20;

				if (_WorldSpaceCameraPos.y >= ocean_surface) {
					return fixed4(0,0,0,0);
				}

				// Run only for skybox and for upward vectors
				// Should not be run for downward vectors because we get a "band" at the horizon...also the 
				// surface is up not down
				if (depth==1.0f && dir.y>0) {
					float3 worldDir = normalize(worldPixelPos);

					if (dir.y == 0) dir.y = 0.0001;
					if (worldDir.y == 0) worldDir.y = 0.0001;

					// Make the y component to be of size 1
					dir = mul(dir, 1.0f/dir.y);
					worldDir = mul(worldDir, 1.0f / worldDir.y);

					// Multiply the "unit" y axis by the distance to the surface.
					// This will also extend the other components
					dir = mul(dir, ocean_surface - _WorldSpaceCameraPos.y);
					worldDir = mul(worldDir, ocean_surface);

					// pow for debug adjustments
					worldDepth = pow(length(dir),1);

					// https://www.youtube.com/watch?v=yXu55U_rRLw
					// Wave animation
					float2 oceanPos = worldDir.xz;
					float3 spos = float3(i.uv.x, i.uv.y, 0) * _NoiseFrequency;
					spos.z += _Time.x * _NoiseSpeed;
					float noise = _NoiseScale * ((snoise(spos)+1)/2);
					float4 noiseDirection = float4(cos(noise*M_PI*2),sin(noise*M_PI*2),0,0);
					fixed4 col = tex2D(_MainTex, i.uv + normalize(noiseDirection)*_PixelOffset );
				}

				float fogVar = saturate(1.0 - (fog_end - worldDepth) / (fog_end - fog_start));
				return lerp(col, ocean_color, fogVar);
			}
			ENDCG
		}

	}
}
