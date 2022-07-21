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
				float4 screen_pos : TEXCOORD1;
				float3 view_vector : TEXCOORD2;
				float3 world_normal : TEXCOORD3;
				float3 world_pos : TEXCOORD4;
			};

			float calculate_specular(float3 normal, float3 viewDir, float smoothness)
			{
				float specular_angle = acos(dot(normalize(DirToSun - viewDir), normal));
				float specular_exponent = specular_angle / smoothness;
				float specular_highlight = exp(-specular_exponent * specular_exponent);
				return specular_highlight;
			}

			float3 blend_rnm(float3 n1, float3 n2)
			{
				n1.z += 1;
				n2.xy = -n2.xy;
				return n1 * dot(n1, n2) / n1.z - n2;
			}

			float3 triplanar_normal(float3 vert_pos, float3 normal, float3 scale, float2 offset, sampler2D normalMap)
			{
				float3 absNormal = abs(normal);

				// Calculate triplanar blend
				float3 blend_weight = saturate(pow(normal, 4));
				// Divide blend weight by the sum of its components. This will make x + y + z = 1
				blend_weight /= dot(blend_weight, 1);

				// Calculate triplanar coordinates
				float2 uvX = vert_pos.zy * scale + offset;
				float2 uvY = vert_pos.xz * scale + offset;
				float2 uvZ = vert_pos.xy * scale + offset;

				// Sample tangent space normal maps
				// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
				float3 tangent_normal_x = UnpackNormal(tex2D(normalMap, uvX));
				float3 tangent_normal_y = UnpackNormal(tex2D(normalMap, uvY));
				float3 tangent_normal_z = UnpackNormal(tex2D(normalMap, uvZ));

				// Swizzle normals to match tangent space and apply reoriented normal mapping blend
				tangent_normal_x = blend_rnm(half3(normal.zy, absNormal.x), tangent_normal_x);
				tangent_normal_y = blend_rnm(half3(normal.xz, absNormal.y), tangent_normal_y);
				tangent_normal_z = blend_rnm(half3(normal.xy, absNormal.z), tangent_normal_z);

				// Apply input normal sign to tangent space Z
				float3 axis_sign = sign(normal);
				tangent_normal_x.z *= axis_sign.x;
				tangent_normal_y.z *= axis_sign.y;
				tangent_normal_z.z *= axis_sign.z;

				// Swizzle tangent normals to match input normal and blend together
				float3 output_normal = normalize(
					tangent_normal_x.zyx * blend_weight.x +
					tangent_normal_y.xzy * blend_weight.y +
					tangent_normal_z.xyz * blend_weight.z
				);

				return output_normal;
			}

			// The vertex shader
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screen_pos = ComputeScreenPos(o.vertex);
				float3 viewVector = mul(unity_CameraInvProjection, float4((o.screen_pos.xy / o.screen_pos.w) * 2 - 1, 0, -1));
				o.view_vector = mul(unity_CameraToWorld, float4(viewVector, 0));
				o.world_normal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);
				o.world_pos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			// The fragment shader
			fixed4 frag(v2f i) : SV_TARGET
			{
				// Water color
				const float non_linear_depth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screen_pos);
				const float dist_to_terrain = LinearEyeDepth(non_linear_depth);
				const float dist_to_water = i.screen_pos.w;
				const float water_view_depth = dist_to_terrain - dist_to_water;
				float3 water_color = lerp(ShallowColor, DeepColor, 1 - exp(-water_view_depth * ColorDepthFactor));

				// Water transparency
				const float3 view_dir = normalize(i.view_vector);
				float alpha_fresnel = 1 - saturate(pow(saturate(dot(-view_dir, i.world_normal)), FresnelPower));
				alpha_fresnel = max(MinAlpha, alpha_fresnel);
				const float alpha_edge = 1 - exp(-water_view_depth * ShorelineFadeStrength);
				float water_alpha = saturate(alpha_edge * alpha_fresnel);

				// Specular highlight
				float wave_speed = 0.35;
				float wave_normal_scale = 0.05;
				float wave_strength = 0.4;

				float2 wave_offset_A = float2(_Time.x * wave_speed, _Time.x * wave_speed * 0.8);
				float2 wave_offset_B = float2(_Time.x * wave_speed * - 0.8, _Time.x * wave_speed * -0.3);
				float3 wave_normal_1 = triplanar_normal(i.world_pos, i.world_normal, wave_normal_scale, wave_offset_A, WaveNormalA);
				float3 wave_normal = triplanar_normal(i.world_pos, wave_normal_1, wave_normal_scale, wave_offset_B, WaveNormalB);
				float3 spec_wave_normal = normalize(lerp(i.world_normal, wave_normal, wave_strength));
				float specular_highlight = calculate_specular(spec_wave_normal, view_dir, Smoothness);

				float spec_threshold = 0.7;
				float stepped_specular_highlight = 0;
				stepped_specular_highlight += (specular_highlight > spec_threshold);
				stepped_specular_highlight += (specular_highlight > spec_threshold * 0.4) * 0.4;
				stepped_specular_highlight += (specular_highlight > spec_threshold * 0.2) * 0.2;
				specular_highlight = stepped_specular_highlight;

				// -------- Lighting and colour output --------
				float lighting = saturate(dot(i.world_normal, DirToSun));
				water_color = saturate(water_color * lighting + unity_AmbientSky) + specular_highlight;
				
				return fixed4(water_color, water_alpha);
			}
			ENDCG
		}
	}
}