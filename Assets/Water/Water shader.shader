Shader "Custom/Water Shader"
{
	// Show values to edit in inspector
	Properties
	{
	}

	SubShader
	{
		//ZWrite On 
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			// Include useful shader functions
			#include "UnityCG.cginc"

			// Define vertex and fragment shader
			#pragma vertex vert
			#pragma fragment frag

			// Define variables
			float FresnelPower;
			float ColorDepthFactor;
			float MinAlpha;
			float ShorelineFadeStrength;
			float3 DirToSun;
			float4 DeepColor;
			float4 ShallowColor;
			sampler2D _CameraDepthTexture;
			
			// The object data that's put into the vertex shader
			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL; 
				float2 uv : TEXCOORD0;
			};

			// Data from the vertex shader passed to the fragment shader
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
				float3 viewVector : TEXCOORD2;
				float3 worldNormal : TEXCOORD3;
			};

			// The vertex shader
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				float3 viewVector = mul(unity_CameraInvProjection, float4((o.screenPos.xy / o.screenPos.w) * 2 - 1, 0, -1));
				o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
				o.worldNormal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);
				return o;
			}

			// The fragment shader
			fixed4 frag(v2f i) : SV_TARGET
			{
				// Water color
				const float nonLinearDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screenPos);
				const float distToTerrain = LinearEyeDepth(nonLinearDepth);
				const float distToWater = i.screenPos.w;
				const float waterViewDepth = distToTerrain - distToWater;
				float3 waterColor = lerp(ShallowColor, DeepColor, 1 - exp(-waterViewDepth * ColorDepthFactor));

				// Water transparency
				const float3 viewDir = normalize(i.viewVector);
				float alphaFresnel = 1 - saturate(pow(saturate(dot(-viewDir, i.worldNormal)), FresnelPower));
				alphaFresnel = max(MinAlpha, alphaFresnel);
				const float alphaEdge = 1 - exp(-waterViewDepth * ShorelineFadeStrength);
				float waterAlpha = saturate(alphaEdge * alphaFresnel);
				
				return fixed4(waterColor, waterAlpha);
			}
			ENDCG
		}
	}
}