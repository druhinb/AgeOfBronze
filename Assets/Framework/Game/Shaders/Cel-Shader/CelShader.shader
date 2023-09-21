Shader "Stylized/CelShader"
{
	Properties
	{
		//The colour of the object
		_Color("Color", Color) = (0.5, 0.65, 1, 1)

		//The texture of the object (plain white by default)
		_MainTex("Main Texture", 2D) = "white" {}	

		//Represents the ambient light of the environment that bounces off surfaces
		[HDR]
		_AmbientColor("Ambient Color", Color) = (0.6, 0.6, 0.6, 1)

		_SpecularColor("Specular Color", Color) = (0.9, 0.9, 0.9, 1)
		_Glossiness("Glossiness", int) = 50

		_RimColor("RimColor", Color) = (1, 1, 1, 1)
		_RimAmount("RimAmount", Range(0, 1)) = 0.651        
		_RimThreshold("RimThreshold", Range(0, 1)) = 0.158
	}
	SubShader
	{
		Pass
		{
			//Requests only main directional light data to be passed to the shader
			Tags
			{
				"LightMode" = "ForwardBase"
				"PassFlags" = "OnlyDirectional"
			}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			//triangle properties
			struct appdata
			{
				float4 vertex : POSITION;				
				float4 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			//vertex properties 
			struct v2f
			{
				SHADOW_COORDS(2)

				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal: NORMAL;
				float3 viewDir : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				
				TRANSFER_SHADOW(o)
				
				return o;
			}
			
			//colour of the object and the ambient lighting colour 
			float4 _Color;
			float4 _AmbientColor;

			float _Glossiness;
			float4 _SpecularColor;

			float4 _RimColor;
			float _RimAmount;
			float _RimThreshold;

			float4 frag (v2f i) : SV_Target
			{
				float3 viewDir = normalize(i.viewDir);
				float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);


				

				//sample the texture at the given uv coordinate
				float4 sample = tex2D(_MainTex, i.uv);

				//fetch the normal of the surface
				float3 normal = normalize(i.worldNormal);

				//find dot between directional light and surface normal
				float NdotL = dot(_WorldSpaceLightPos0, normal);				
				float NdotH = dot(normal, halfVector);
				
				float shadow = SHADOW_ATTENUATION(i);

				//if surface and light normals are aligned, light the surface, else don't light it
				float lightIntensity = smoothstep(0, 0.01, NdotL * shadow);
				float specularIntensity = smoothstep(0.005, 0.01, pow(NdotH * lightIntensity, _Glossiness * _Glossiness));

				float4 light = lightIntensity * _LightColor0;
				float4 specular = specularIntensity * _SpecularColor;

				float4 rimDot = 1 - dot(viewDir, normal);
				float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
					rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
				
				float4 rim = rimIntensity * _RimColor;
				//final shading takes into account ambient lighting and whether the surface is lit or
				return _Color * sample * (_AmbientColor + light + specular + rim);
			}
			ENDCG
		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}