﻿Shader "Custom/RayMarchVolume" 
{	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma target 5.0
	
	sampler2D _CubeBackTex;
	sampler3D _VolumeTex;

	sampler2D_float _CameraDepthTexture;

	int _VolumeSize;	

	float _Opacity;	
	float _StepSize;		
	float _OffsetDist;	
	float _IntensityThreshold;		

	float4 _SurfaceColor;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 worldPos : COLOR0;
	};

	v2f vert(appdata_base v)  
	{
		v2f output;
		output.pos = mul (UNITY_MATRIX_MVP, v.vertex);
		output.worldPos = v.vertex + 0.5;
        return output;
    }

	// TODO: Correct depth
	float get_depth( float3 current_pos )
	{
		float4 pos = mul (UNITY_MATRIX_MVP, float4(current_pos - 0.5, 1));
		return (pos.z / pos.w) ;
	}

	float sample_volume( float3 p )
	{
		return tex3Dlod(_VolumeTex, float4(p,0)).r;	
	}

	float3 get_normal(float3 position, float3 dataStep)
	{
		float dx = sample_volume(position + float3(dataStep.x, 0, 0)) - sample_volume(position+float3(-dataStep.x, 0, 0)); 
		float dy = sample_volume(position + float3(0, dataStep.y, 0)) - sample_volume(position+float3(0, -dataStep.y, 0));			
		float dz = sample_volume(position + float3(0, 0, dataStep.z)) - sample_volume(position+float3(0, 0, -dataStep.z));		

		return normalize(float3(dx,dy,dz));
	}
	
	// Raycast surface only
	void frag_surf(v2f i, out float4 color : COLOR0, out float4 depth_normal : COLOR1, out float depth : DEPTH) 
	{
		depth = 1;
		color = float4(0,0,0,0.2);
		depth_normal = EncodeDepthNormal(1, float3(0,0,0));

		float2 uv = i.pos.xy / _ScreenParams.xy;	
		
		float3 front_pos = i.worldPos.xyz;						
		float3 back_pos = tex2D(_CubeBackTex, uv).xyz;	
		float3 current_pos = front_pos;

		float3 dir = back_pos - front_pos;
		float3 delta_dir = normalize(dir) * _StepSize;	
		
		float delta_dir_len = length(delta_dir);
		float length_max = length(dir.xyz);
		float length_acc = 0;	
			
		bool seek_depth = true;
		bool seek_first = true;
		bool found_lastpos = false;

		float3 last_pos = float3(0,0,0);

		float current_intensity = 0;	
		float previous_intensity = 0;	

		[loop]
		[allow_uav_condition]
		for( uint i = 0; i < 512 ; i++ )
		{
			current_intensity = tex3Dlod(_VolumeTex, float4(current_pos, 0)).r / 200.0f;
			
			if(seek_depth && current_intensity >= _IntensityThreshold) 
			{
				depth = get_depth(current_pos);				
				depth_normal = EncodeDepthNormal(depth, mul (UNITY_MATRIX_MVP, get_normal(current_pos, 1.0/ _VolumeSize)));
				seek_depth = false;
				// Do depth/normal too
			}

			// If the ray enters the volume
			if( seek_first && current_intensity >= _IntensityThreshold && previous_intensity < _IntensityThreshold )
			{
				float3 world_normal = get_normal(current_pos, 1.0/ _VolumeSize);
				float3 light_dir = normalize(-_WorldSpaceCameraPos);
				float ndotl = max( 0.0, dot(light_dir, world_normal));

				color.rgb += (1.0 - color.a) * _SurfaceColor.rgb * _Opacity * pow(ndotl,2);
				color.a += (1.0 - color.a) * _Opacity;

				// Only upper layer
				//break;

				if(_Opacity >= 1) break;	

				//seek_first = false;						
			}				
			else if( current_intensity < _IntensityThreshold && previous_intensity >= _IntensityThreshold )
			{
				if(tex2D(_CameraDepthTexture, uv).r < get_depth(current_pos)) continue;
				float3 world_normal = get_normal(current_pos, 1.0/ _VolumeSize);
				float3 light_dir = normalize(-_WorldSpaceCameraPos);
				float ndotl = max( 0.0, dot(light_dir, -world_normal));

				color.rgb += (1.0 - color.a) * _SurfaceColor.rgb * _Opacity * ndotl;
				color.a += (1.0 - color.a) * _Opacity;

				//last_pos = current_pos;
				//found_lastpos = true;
			}			
			
			previous_intensity = current_intensity;

			current_pos += delta_dir;
			length_acc += delta_dir_len;
			
			if(length_acc >= length_max) break;								 		
		}

		//if(found_lastpos)
		//{
		//	float3 world_normal = get_normal(last_pos, 1.0/ _VolumeSize);
		//	float3 light_dir = normalize(-_WorldSpaceCameraPos);
		//	float ndotl = max( 0.0, dot(light_dir, -world_normal));

		//	color.rgb += (1.0 - color.a) * _SurfaceColor.rgb * _Opacity * ndotl;
		//	color.a += (1.0 - color.a) * _Opacity;
		//}	
	}	

	ENDCG
	
Subshader 
{
	ZTest Always 	
	ZWrite On
	Cull Back 	

	Pass 
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_surf		
		ENDCG
	}				
}

Fallback off
	
} // shader