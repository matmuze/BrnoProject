Shader "Custom/TunnelShader" 
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	CGINCLUDE

	#include "UnityCG.cginc"
															
	uniform	StructuredBuffer<float4> tunnelPositions;
	//uniform	StructuredBuffer<float4> tunnelColors;
	uniform	StructuredBuffer<float> tunnelRadii;	
	
	uniform float scale;	
	
	struct vs2gs
	{
	    float4 pos : SV_POSITION;	
	    float4 info : COLOR1;
    };        	
			
	struct gs2fs
	{
		float4 pos : SV_POSITION;
		float4 info : COLOR1;
		float2 uv : TEXCOORD0;			    
	};
				
	vs2gs VS(uint id : SV_VertexID)
	{
		vs2gs output;			
		output.pos = tunnelPositions[id]; 
		//output.pos.y *= -1;		
		output.info = float4(tunnelRadii[id], 0, 0, 0); 	    
		return output;
	}	

	uniform float distortion;
			
	[maxvertexcount(4)]
	void GS_mask(point vs2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		if( round(input[0].info.x) <  0) return;
				       
		
		float tunnel_radius = input[0].info.x * scale;
		float sphere_radius = tunnel_radius * (1+ (distortion *4));
		float3 sphere_pos = input[0].pos.xyz * scale;
		sphere_pos += normalize(_WorldSpaceCameraPos - sphere_pos) * (sphere_radius - tunnel_radius)  ;
		
		
		float4 pos = mul(UNITY_MATRIX_MVP, float4(sphere_pos, 1.0));
        float4 offset = mul(UNITY_MATRIX_P, float4(sphere_radius, sphere_radius, 0, 1));

		gs2fs output;					
		output.info = float4(sphere_radius, 0, 0, 0); 	   

		//*****//

		output.uv = float2(1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);	
								
		output.uv = float2(-1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(-1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);	
	}

	inline float EyeDepthToZBuffer( float z )
	{
		return 1.0 / (z * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;		
	}

	void FS_mask (gs2fs input, out float4 color : COLOR0, out float depth : DEPTH) 
	{							
		float lensqr = dot(input.uv, input.uv);
    			
    	if(lensqr > 1.0) discard;			    
							
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));					
		float ndotl = max( 0.0, dot(float3(0,0,1), normal));	
		float3 atomColor = float3(0,0,1);
		float3 finalColor = atomColor * pow(ndotl, 0.1);
		//	float3 finalColor = atomColor * pow(ndotl, 1);
		color =  float4( finalColor, 1);			
		depth = EyeDepthToZBuffer(LinearEyeDepth(input.pos.z) + input.info.x * normal.z);		
	}
						
	[maxvertexcount(4)]
	void GS(point vs2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		if( round(input[0].info.x) <  0) return;
				
        float radius = scale * input[0].info.x * 1;
		float4 pos = mul(UNITY_MATRIX_MVP, float4(input[0].pos.xyz * scale, 1.0));
        float4 offset = mul(UNITY_MATRIX_P, float4(radius, radius, 0, 1));

		gs2fs output;					
		output.info = float4(radius, 0, 0, 0); 	   

		//*****//

		output.uv = float2(1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);	
								
		output.uv = float2(-1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(-1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);	
	}	
			
	void FS (gs2fs input, out float4 color : COLOR0, out float depth : DEPTH) 
	{								
		float lensqr = dot(input.uv, input.uv);
    			
    	if(lensqr > 1.0) discard;			    
							
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));					
		float ndotl = max( 0.0, dot(float3(0,0,1), normal));	
		float3 atomColor = float3(0,0,1);
		float3 finalColor = atomColor * pow(ndotl, 1);
		//	float3 finalColor = atomColor * pow(ndotl, 1);
		color =  float4( finalColor, 0.5);			
		depth = EyeDepthToZBuffer(LinearEyeDepth(input.pos.z) + input.info.x * -normal.z);		
	}

	ENDCG
	
	SubShader 
	{	
		// Mask pass
	    Pass 
	    {
			ZTest GEqual  
			ZWrite On

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS				
			#pragma fragment FS_mask
			#pragma geometry GS_mask						
						
			ENDCG
		}	

		// Render pass
	    Pass 
	    {
			
			ZWrite On

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS				
			#pragma fragment FS
			#pragma geometry GS						
						
			ENDCG
		}	
	}
	Fallback Off
}	