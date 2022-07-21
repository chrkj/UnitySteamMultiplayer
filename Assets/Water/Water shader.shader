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
			float Smoothness;
			float3 DirToSun;
			float4 DeepColor;
			float4 ShallowColor;
			sampler2D _CameraDepthTexture;
			sampler2D WaveNormalA;
			sampler2D WaveNormalB;
			
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
				float3 worldPos : TEXCOORD4;
			};

			float calculate_specular(float3 normal, float3 viewDir, float smoothness)
			{
				float specularAngle = acos(dot(normalize(DirToSun - viewDir), normal));
				float specularExponent = specularAngle / smoothness;
				float specularHighlight = exp(-specularExponent * specularExponent);
				return specularHighlight;
			}

			float3 blend_rnm(float3 n1, float3 n2)
			{
				n1.z += 1;
				n2.xy = -n2.xy;
				return n1 * dot(n1, n2) / n1.z - n2;
			}

			float3 triplanarNormal(float3 vertPos, float3 normal, float3 scale, float2 offset, sampler2D normalMap) {
				float3 absNormal = abs(normal);

				// Calculate triplanar blend
				float3 blendWeight = saturate(pow(normal, 4));
				// Divide blend weight by the sum of its components. This will make x + y + z = 1
				blendWeight /= dot(blendWeight, 1);

				// Calculate triplanar coordinates
				float2 uvX = vertPos.zy * scale + offset;
				float2 uvY = vertPos.xz * scale + offset;
				float2 uvZ = vertPos.xy * scale + offset;

				// Sample tangent space normal maps
				// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
				float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
				float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
				float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

				// Swizzle normals to match tangent space and apply reoriented normal mapping blend
				tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
				tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
				tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

				// Apply input normal sign to tangent space Z
				float3 axisSign = sign(normal);
				tangentNormalX.z *= axisSign.x;
				tangentNormalY.z *= axisSign.y;
				tangentNormalZ.z *= axisSign.z;

				// Swizzle tangent normals to match input normal and blend together
				float3 outputNormal = normalize(
					tangentNormalX.zyx * blendWeight.x +
					tangentNormalY.xzy * blendWeight.y +
					tangentNormalZ.xyz * blendWeight.z
				);

				return outputNormal;
			}

			// The vertex shader
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				float3 viewVector = mul(unity_CameraInvProjection, float4((o.screenPos.xy / o.screenPos.w) * 2 - 1, 0, -1));
				o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
				o.worldNormal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
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

				// Specular highlight
				float waveSpeed = 0.35;
				float waveNormalScale = 0.05;
				float waveStrength = 0.4;

				float2 waveOffsetA = float2(_Time.x * waveSpeed, _Time.x * waveSpeed * 0.8);
				float2 waveOffsetB = float2(_Time.x * waveSpeed * - 0.8, _Time.x * waveSpeed * -0.3);
				float3 waveNormal1 = triplanarNormal(i.worldPos, i.worldNormal, waveNormalScale, waveOffsetA, WaveNormalA);
				float3 waveNormal = triplanarNormal(i.worldPos, waveNormal1, waveNormalScale, waveOffsetB, WaveNormalB);
				float3 specWaveNormal = normalize(lerp(i.worldNormal, waveNormal, waveStrength));
				float specularHighlight = calculate_specular(specWaveNormal, viewDir, Smoothness);

				float specThreshold = 0.7;
				float steppedSpecularHighlight = 0;
				steppedSpecularHighlight += (specularHighlight > specThreshold);
				steppedSpecularHighlight += (specularHighlight > specThreshold * 0.4) * 0.4;
				steppedSpecularHighlight += (specularHighlight > specThreshold * 0.2) * 0.2;
				specularHighlight = steppedSpecularHighlight;

				// -------- Lighting and colour output --------
				float lighting = saturate(dot(i.worldNormal, DirToSun));
				waterColor = saturate(waterColor * lighting + unity_AmbientSky) + specularHighlight;
				
				return fixed4(waterColor, waterAlpha);
			}
			ENDCG
		}
	}
}