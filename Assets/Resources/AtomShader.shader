Shader "Custom/AtomShader" 
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	CGINCLUDE

	#include "UnityCG.cginc"
															
	uniform float _Scale;		
	uniform float _AtomRadius;	
	uniform float _ContextAtomRadius;

	uniform	StructuredBuffer<int> atomTypes;
	uniform	StructuredBuffer<float> atomRadii;
	uniform	StructuredBuffer<float4> atomColors;	
	uniform	StructuredBuffer<float4> atomPositions;	
		
	uniform	StructuredBuffer<int> atomAminoAcidTypes;
	uniform	StructuredBuffer<float4> aminoAcidColors;
				
	uniform	StructuredBuffer<int> _CulledAtoms;
	uniform	StructuredBuffer<int> _FocusedAtoms;

	inline float EyeDepthToZBuffer( float z )
	{
		return 1.0 / (z * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;		
	}

	struct vs2gs
	{
	    int id : INT0;
    };        	
			
	struct gs2fs
	{
		float4 pos : SV_POSITION;
		float4 info : COLOR0;
		float2 uv : TEXCOORD0;			    
	};
				
	vs2gs VS(uint id : SV_VertexID)
	{
		vs2gs output;
		output.id = id;
		return output;
	}				
						
	[maxvertexcount(4)]
	void GS(point vs2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		if( _CulledAtoms[input[0].id] > 0) return;
		if( _FocusedAtoms[input[0].id] > 0) return;

		int atom_type = atomTypes[input[0].id];
		int atom_aa_type = atomAminoAcidTypes[input[0].id]; 
		float atom_radius = atomRadii[atom_type] * _Scale * _ContextAtomRadius;
		float3 atom_pos = atomPositions[input[0].id].xyz * _Scale; 
		
		float4 pos = mul(UNITY_MATRIX_MVP, float4(atom_pos, 1.0));        
		float4 offset = mul(UNITY_MATRIX_P, float4(atom_radius, atom_radius, 0, 1));
				
		gs2fs output;
		output.info = float4(atom_radius, atom_type, atom_aa_type, 0); 

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
			
	void FS (gs2fs input, out float4 color : SV_Target0 , out float depth : SV_Depth) 
	{	
		float lensqr = dot(input.uv, input.uv);    			
    	if(lensqr > 1.0) discard;			    
							
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));						

		float ndotl = max( 0.0, dot(float3(0,0,1), normal));
		/*float3 atomColor = atomColors[round(input.info.y)].rgb;
		float3 atomColor = aminoAcidColors[round(input.info.z)].rgb;*/
		float3 atomColor = float3(0.5, 0.5, 0.5);
		float3 finalColor = atomColor * pow(ndotl, 0.25);
		
		color =  float4( finalColor, 1);				
		depth = EyeDepthToZBuffer(LinearEyeDepth(input.pos.z) + input.info.x * -normal.z );	
	}

	/****/

	vs2gs VS_2(uint id : SV_VertexID)
	{
		vs2gs output;
		output.id = id;	    
		return output;
	}				
						
	[maxvertexcount(4)]
	void GS_2(point vs2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{		
		if( _FocusedAtoms[input[0].id] < 1) return;

		int atom_type = atomTypes[input[0].id];
		int atom_aa_type = atomAminoAcidTypes[input[0].id]; 
		float atom_radius = atomRadii[atom_type] * _Scale * _AtomRadius;
		float3 atom_pos = atomPositions[input[0].id].xyz * _Scale; 
		
		float4 pos = mul(UNITY_MATRIX_MVP, float4(atom_pos, 1.0));        
		float4 offset = mul(UNITY_MATRIX_P, float4(atom_radius, atom_radius, 0, 1));
				
		gs2fs output;
		output.info = float4(atom_radius, atom_type, atom_aa_type, 0); 

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
			
	void FS_2(gs2fs input, out float4 color : SV_Target0 , out float depth : SV_Depth) 
	{	
		float lensqr = dot(input.uv, input.uv);    			
    	if(lensqr > 1.0) discard;			    
							
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));						

		float ndotl = max( 0.0, dot(float3(0,0,1), normal));
		float3 atomColor = atomColors[round(input.info.y)].rgb;
		//float3 atomColor = aminoAcidColors[round(input.info.z)].rgb;
		float3 finalColor = atomColor * pow(ndotl, 0.9);
		
		color =  float4( finalColor, 1);				
		depth = EyeDepthToZBuffer(LinearEyeDepth(input.pos.z) + input.info.x * -normal.z );	
	}
						
	ENDCG	
	
	SubShader 	
	{	
		// First pass
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

		// Second pass
	    Pass 
	    {
			ZWrite On

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS_2		
			#pragma geometry GS_2					
			#pragma fragment FS_2		
		
			ENDCG
		}		
	}
	Fallback Off
}	