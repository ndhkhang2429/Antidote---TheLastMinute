// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "AE/Grunge"
{
	Properties
	{
		_Base_Color("Base_Color", 2D) = "white" {}
		_Metallic_Smoothness("Metallic_Smoothness", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_AmbientOcclusion("Ambient Occlusion", 2D) = "white" {}
		_Grunge_Texture("Grunge_Texture", 2D) = "white" {}
		_Grunge_Mask("Grunge_Mask", 2D) = "white" {}
		_Tiling_Main_Texture("Tiling_Main_Texture", Vector) = (1,1,0,0)
		_Tint("Tint", Color) = (1,1,1,0)
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.2765529
		_Normal_Scale("Normal_Scale", Range( 0 , 15)) = 1
		[Toggle(_GRUNGE_ON_ON)] _Grunge_ON("Grunge_ON", Float) = 1
		_GrungeScale("GrungeScale", Range( 1 , 64)) = 1
		_Grunge_Tint("Grunge_Tint", Color) = (0.5566038,0.5566038,0.5566038,0)
		[KeywordEnum(R,G,B)] _GrungeChannel("GrungeChannel", Float) = 1
		_Grunge_Opacity("Grunge_Opacity", Range( 0 , 1)) = 0.6238509
		_Grunge_Max("Grunge_Max", Range( 0 , 1)) = 0.3000374
		_Grunge_Min("Grunge_Min", Range( 0 , 1)) = 0
		_Grunge_Smoothness("Grunge_Smoothness", Range( -1 , 1)) = 0.2765529
		_Grunge_Normal("Grunge_Normal", Range( 0 , 1.45)) = 5
		[Toggle(_GRUNGE_TEXTURE_ON_ON)] _Grunge_Texture_ON("Grunge_Texture_ON", Float) = 0
		_Grunge_Texture_Scale("Grunge_Texture_Scale", Range( 1 , 64)) = 5.898302
		_Grunge_Texture_Opacity("Grunge_Texture_Opacity", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _GRUNGE_ON_ON
		#pragma shader_feature_local _GRUNGECHANNEL_R _GRUNGECHANNEL_G _GRUNGECHANNEL_B
		#pragma shader_feature_local _GRUNGE_TEXTURE_ON_ON
		#define ASE_VERSION 19801
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform sampler2D _Normal;
		uniform float2 _Tiling_Main_Texture;
		uniform float _Normal_Scale;
		uniform float _Grunge_Normal;
		uniform float _Grunge_Opacity;
		uniform sampler2D _Grunge_Mask;
		uniform float _GrungeScale;
		uniform float _Grunge_Min;
		uniform float _Grunge_Max;
		uniform float4 _Tint;
		uniform sampler2D _Base_Color;
		uniform float4 _Grunge_Tint;
		uniform sampler2D _Grunge_Texture;
		uniform float _Grunge_Texture_Scale;
		uniform float _Grunge_Texture_Opacity;
		uniform sampler2D _Metallic_Smoothness;
		uniform float _Smoothness;
		uniform float _Grunge_Smoothness;
		uniform sampler2D _AmbientOcclusion;


		inline float4 TriplanarSampling68( sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			xNorm = tex2D( topTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm = tex2D( topTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
		}


		inline float4 TriplanarSampling78( sampler2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			xNorm = tex2D( topTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm = tex2D( topTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_TexCoord79 = i.uv_texcoord * _Tiling_Main_Texture;
			float2 temp_cast_0 = (( 2.0 / _GrungeScale )).xx;
			float3 ase_positionWS = i.worldPos;
			float3 ase_normalWS = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float4 triplanar68 = TriplanarSampling68( _Grunge_Mask, ase_positionWS, ase_normalWS, 1.0, temp_cast_0, 1.0, 0 );
			#if defined( _GRUNGECHANNEL_R )
				float staticSwitch72 = triplanar68.x;
			#elif defined( _GRUNGECHANNEL_G )
				float staticSwitch72 = triplanar68.g;
			#elif defined( _GRUNGECHANNEL_B )
				float staticSwitch72 = triplanar68.b;
			#else
				float staticSwitch72 = triplanar68.g;
			#endif
			float clampResult80 = clamp( (0.0 + (staticSwitch72 - _Grunge_Min) * (1.0 - 0.0) / (_Grunge_Max - _Grunge_Min)) , 0.0 , 1.0 );
			#ifdef _GRUNGE_ON_ON
				float staticSwitch107 = ( _Grunge_Opacity * clampResult80 );
			#else
				float staticSwitch107 = 0.0;
			#endif
			float3 lerpResult103 = lerp( UnpackScaleNormal( tex2D( _Normal, uv_TexCoord79 ), _Normal_Scale ) , float3(0,0,1) , ( _Grunge_Normal * staticSwitch107 ));
			o.Normal = lerpResult103;
			float4 temp_cast_1 = (1.0).xxxx;
			float2 temp_cast_2 = (( 2.0 / _Grunge_Texture_Scale )).xx;
			float4 triplanar78 = TriplanarSampling78( _Grunge_Texture, ase_positionWS, ase_normalWS, 1.0, temp_cast_2, 1.0, 0 );
			float temp_output_77_0 = ( 1.0 - _Grunge_Texture_Opacity );
			float4 temp_cast_3 = (temp_output_77_0).xxxx;
			float4 lerpResult83 = lerp( triplanar78 , temp_cast_3 , temp_output_77_0);
			#ifdef _GRUNGE_TEXTURE_ON_ON
				float4 staticSwitch93 = lerpResult83;
			#else
				float4 staticSwitch93 = _Grunge_Tint;
			#endif
			float4 lerpResult106 = lerp( temp_cast_1 , staticSwitch93 , staticSwitch107);
			o.Albedo = ( ( _Tint * tex2D( _Base_Color, uv_TexCoord79 ) ) * lerpResult106 ).rgb;
			float4 tex2DNode99 = tex2D( _Metallic_Smoothness, uv_TexCoord79 );
			o.Metallic = tex2DNode99.r;
			float lerpResult101 = lerp( ( _Smoothness * tex2DNode99.a ) , _Grunge_Smoothness , staticSwitch107);
			o.Smoothness = lerpResult101;
			o.Occlusion = tex2D( _AmbientOcclusion, uv_TexCoord79 ).r;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.RangedFloatNode;64;-1618.125,114.8305;Inherit;False;Property;_GrungeScale;GrungeScale;11;0;Create;True;0;0;0;False;0;False;1;43;1;64;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;65;-1490.125,-77.16949;Inherit;False;Constant;_Float0;Float 0;2;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;66;-1522.125,-493.1695;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleDivideOpNode;67;-1250.125,-157.1695;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;92;-1586.125,-269.1695;Inherit;True;Property;_Grunge_Mask;Grunge_Mask;5;0;Create;True;0;0;0;False;0;False;8086b626d4688f84faef7403c4eec05e;8086b626d4688f84faef7403c4eec05e;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TriplanarNode;68;-1090.125,-381.1696;Inherit;True;Spherical;World;False;Top Texture 0;_TopTexture0;white;-1;None;Mid Texture 0;_MidTexture0;white;-1;None;Bot Texture 0;_BotTexture0;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;70;-738.125,-669.1695;Inherit;False;Property;_Grunge_Min;Grunge_Min;16;0;Create;True;0;0;0;False;0;False;0;0.022;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;71;-738.125,-573.1695;Inherit;False;Property;_Grunge_Max;Grunge_Max;15;0;Create;True;0;0;0;False;0;False;0.3000374;0.617;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;72;-658.125,-349.1695;Inherit;False;Property;_GrungeChannel;GrungeChannel;13;0;Create;True;0;0;0;False;0;False;0;1;1;True;;KeywordEnum;3;R;G;B;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;69;-1506.125,-1133.17;Inherit;False;Constant;_Float2;Float 0;2;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;97;-1634.125,-941.1696;Inherit;False;Property;_Grunge_Texture_Scale;Grunge_Texture_Scale;20;0;Create;True;0;0;0;False;0;False;5.898302;12.6;1;64;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;76;-338.125,-381.1696;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;73;-626.125,-1133.17;Inherit;False;Property;_Grunge_Texture_Opacity;Grunge_Texture_Opacity;21;0;Create;True;0;0;0;False;0;False;1;0.469;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;74;-1266.125,-1213.17;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;94;-1395.718,-1416.022;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TexturePropertyNode;96;-1250.125,-1597.169;Inherit;True;Property;_Grunge_Texture;Grunge_Texture;4;0;Create;True;0;0;0;False;0;False;892df4ee12c65c743bef91e8b7a47532;892df4ee12c65c743bef91e8b7a47532;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ClampOpNode;80;-130.125,-381.1696;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;81;-274.125,-493.1695;Inherit;False;Property;_Grunge_Opacity;Grunge_Opacity;14;0;Create;True;0;0;0;False;0;False;0.6238509;0.69;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;75;-1634.125,290.8305;Inherit;False;Property;_Tiling_Main_Texture;Tiling_Main_Texture;6;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.OneMinusNode;77;-370.125,-1213.17;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;78;-914.125,-1469.169;Inherit;True;Spherical;World;False;Top Texture 1;_TopTexture1;white;-1;None;Mid Texture 1;_MidTexture1;white;-1;None;Bot Texture 1;_BotTexture1;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT;1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;104;92.28198,-472.0223;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;79;-1346.125,258.8305;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;82;-402.125,-973.1696;Inherit;False;Property;_Grunge_Tint;Grunge_Tint;12;0;Create;True;0;0;0;False;0;False;0.5566038,0.5566038,0.5566038,0;0.1465378,0.2395391,0.3490565,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;83;-162.125,-1261.17;Inherit;True;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StaticSwitch;107;380.282,-456.0223;Inherit;True;Property;_Grunge_ON;Grunge_ON;10;0;Create;True;0;0;0;False;0;False;0;1;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;84;-930.125,-157.1695;Inherit;False;Property;_Tint;Tint;7;0;Create;True;0;0;0;False;0;False;1,1,1,0;0.7264151,0.7264151,0.7264151,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;85;-258.125,-605.1695;Inherit;False;Constant;_Float1;Float 1;12;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;89;-610.125,178.8305;Inherit;False;Property;_Smoothness;Smoothness;8;0;Create;True;0;0;0;False;0;False;0.2765529;0.66;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;98;-994.125,82.83051;Inherit;True;Property;_Base_Color;Base_Color;0;0;Create;True;0;0;0;False;0;False;-1;4814fa37e4b2bdf4f8c893b57b2a05b6;4814fa37e4b2bdf4f8c893b57b2a05b6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;99;-1010.125,290.8305;Inherit;True;Property;_Metallic_Smoothness;Metallic_Smoothness;1;0;Create;True;0;0;0;False;0;False;-1;370c6a8f759f7c1409e438d8e9cdb247;370c6a8f759f7c1409e438d8e9cdb247;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.StaticSwitch;93;141.875,-1085.17;Inherit;True;Property;_Grunge_Texture_ON;Grunge_Texture_ON;19;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;95;-1315.718,551.9777;Inherit;False;Property;_Normal_Scale;Normal_Scale;9;0;Create;True;0;0;0;False;0;False;1;0.97;0;15;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;135;144,128;Inherit;True;Property;_Grunge_Normal;Grunge_Normal;18;0;Create;True;0;0;0;False;0;False;5;1.45;0;1.45;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;86;-562.125,-45.16949;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-131.718,23.97769;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;106;508.282,-760.0223;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;91;-67.71802,-72.02231;Inherit;False;Property;_Grunge_Smoothness;Grunge_Smoothness;17;0;Create;True;0;0;0;False;0;False;0.2765529;0.907;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;87;-1008,496;Inherit;True;Property;_Normal;Normal;2;0;Create;True;0;0;0;False;0;False;-1;f62c523b81b614147b075badd08f49a2;101988d3209fab6489afa98fa66e2e81;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;136;512,128;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;137;599.8186,447.9825;Inherit;True;Constant;_Vector0;Vector 0;22;0;Create;True;0;0;0;False;0;False;0,0,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.BreakToComponentsNode;62;-517.094,-381.1208;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.BreakToComponentsNode;63;-764.7917,-434.0926;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SamplerNode;100;-1011.718,711.9777;Inherit;True;Property;_AmbientOcclusion;Ambient Occlusion;3;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;796.282,-696.0223;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;101;640,-112;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;103;816,128;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1360,-480;Float;False;True;-1;2;AmplifyShaderEditor.MaterialInspector;0;0;Standard;AE/Grunge;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;67;0;65;0
WireConnection;67;1;64;0
WireConnection;68;0;92;0
WireConnection;68;9;66;0
WireConnection;68;3;67;0
WireConnection;72;1;68;1
WireConnection;72;0;68;2
WireConnection;72;2;68;3
WireConnection;76;0;72;0
WireConnection;76;1;70;0
WireConnection;76;2;71;0
WireConnection;74;0;69;0
WireConnection;74;1;97;0
WireConnection;80;0;76;0
WireConnection;77;0;73;0
WireConnection;78;0;96;0
WireConnection;78;9;94;0
WireConnection;78;3;74;0
WireConnection;104;0;81;0
WireConnection;104;1;80;0
WireConnection;79;0;75;0
WireConnection;83;0;78;0
WireConnection;83;1;77;0
WireConnection;83;2;77;0
WireConnection;107;0;104;0
WireConnection;98;1;79;0
WireConnection;99;1;79;0
WireConnection;93;1;82;0
WireConnection;93;0;83;0
WireConnection;86;0;84;0
WireConnection;86;1;98;0
WireConnection;88;0;89;0
WireConnection;88;1;99;4
WireConnection;106;0;85;0
WireConnection;106;1;93;0
WireConnection;106;2;107;0
WireConnection;87;1;79;0
WireConnection;87;5;95;0
WireConnection;136;0;135;0
WireConnection;136;1;107;0
WireConnection;100;1;79;0
WireConnection;105;0;86;0
WireConnection;105;1;106;0
WireConnection;101;0;88;0
WireConnection;101;1;91;0
WireConnection;101;2;107;0
WireConnection;103;0;87;0
WireConnection;103;1;137;0
WireConnection;103;2;136;0
WireConnection;0;0;105;0
WireConnection;0;1;103;0
WireConnection;0;3;99;1
WireConnection;0;4;101;0
WireConnection;0;5;100;0
ASEEND*/
//CHKSM=805658D01C6A3460F4D20F9A20D80D9CF2875678