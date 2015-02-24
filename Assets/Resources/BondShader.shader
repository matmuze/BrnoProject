Shader "Custom/BondShader" 
{
	CGINCLUDE

	#include "UnityCG.cginc"
															
	uniform float _Scale;										
	uniform float _StickRadius;										
	uniform float _ContextStickRadius;			
			
	uniform	StructuredBuffer<int> _AtomTypes;
	uniform	StructuredBuffer<int2> _AtomBonds;
	uniform	StructuredBuffer<float4> _AtomPositions;	

	uniform	StructuredBuffer<int> _AtomAminoAcidIds;	
	uniform	StructuredBuffer<int> _AtomAminoAcidTypes;
	uniform	StructuredBuffer<float4> _AminoAcidColors;

	uniform	StructuredBuffer<int> _CulledAtoms;
	uniform	StructuredBuffer<int> _FocusedAtoms;

	float EyeDepthToZBuffer( float z )
	{
		return 1.0 / (z * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;		
	}       	
	
	struct vs2gs
	{
	    int id : INT0;
    };   
			
	struct gs2fs
	{
		float4 pos : POSITION0;
		float2 uv : TEXCOORD0;	
		float delta_z : FLOAT0;
		int aa_type : INT0;
	};
				
	vs2gs VS(uint id : SV_VertexID)
	{
		vs2gs output;
		output.id = id;
		return output;
	}				
						
	uniform float4 _CameraForward;

	[maxvertexcount(4)]
	void GS(point vs2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		int2 bond = _AtomBonds[input[0].id];

		int atom_aa_id_1 = _AtomAminoAcidIds[bond.x];
		int atom_aa_id_2 = _AtomAminoAcidIds[bond.y];

		if(atom_aa_id_1 != atom_aa_id_2) return;
		
		int culled_atom_1 = _CulledAtoms[bond.x];
		int culled_atom_2 = _CulledAtoms[bond.y];

		if(culled_atom_1 > 0 || culled_atom_2 > 0) return;

		/****/

		int atom_type_1 = _AtomTypes[bond.x];
		int atom_type_2 = _AtomTypes[bond.y];

		int atom_aa_type_1 = _AtomAminoAcidTypes[bond.x];
		int atom_aa_type_2 = _AtomAminoAcidTypes[bond.y];

		float3 atom_pos_1 = _AtomPositions[bond.x].xyz * _Scale;
		float3 atom_pos_2 = _AtomPositions[bond.y].xyz * _Scale;
		
		float stick_radius = _Scale * _StickRadius;

		// Find vector perpendical to the screen and the stick
		float3 cross_pos = normalize(cross(atom_pos_2 - atom_pos_1, _WorldSpaceCameraPos - atom_pos_1));
		
		// Find stick radii
		float3 stick_corner_1 = atom_pos_2 + cross_pos * stick_radius;
		float3 stick_corner_2 = atom_pos_2 - cross_pos * stick_radius;
		float3 stick_corner_3 = atom_pos_1 + cross_pos * stick_radius;
		float3 stick_corner_4 = atom_pos_1 - cross_pos * stick_radius;
				
		// Find stick normal
		float3 stick_norm = normalize(cross(stick_corner_4 - stick_corner_2, stick_corner_3 - stick_corner_2));
				
		// Find depth offset
		float3 dd = normalize(_WorldSpaceCameraPos - atom_pos_1);
		float cos_a = dot(dd, stick_norm);		
		float delta_z = max(stick_radius, min((stick_radius) / cos_a, stick_radius * 1.0));		
				
		float4 pp_1 = mul(UNITY_MATRIX_MVP, float4(stick_corner_1, 1));
		float4 pp_2 = mul(UNITY_MATRIX_MVP, float4(stick_corner_2, 1));
		float4 pp_3 = mul(UNITY_MATRIX_MVP, float4(stick_corner_3, 1));
		float4 pp_4 = mul(UNITY_MATRIX_MVP, float4(stick_corner_4, 1));
		
		// Send vertices to render		
		gs2fs output;	
		output.delta_z = delta_z;	
		output.aa_type = atom_aa_type_1;

		output.pos = pp_1;
		output.uv = float2(1,1);
		triangleStream.Append(output);	

		output.pos = pp_2;
		output.uv = float2(1,-1);
		triangleStream.Append(output);	

		output.pos = pp_3;
		output.uv = float2(-1,1);
		triangleStream.Append(output);	

		output.pos = pp_4;
		output.uv = float2(-1,-1);
		triangleStream.Append(output);			
	}
			
	void FS (gs2fs input, out float4 color : COLOR0, out float depth : SV_Depth)  
	{
		// Find distance to the center line
		float y = sqrt(1 - (input.uv.y * input.uv.y));
		
		float3 atomColor = _AminoAcidColors[input.aa_type].rgb;

		color =  float4( atomColor * (y), 1);
		depth = EyeDepthToZBuffer(LinearEyeDepth(input.pos.z) - y * input.delta_z);				
	}

	/*****/

	[maxvertexcount(4)]
	void GS_2(point vs2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		int2 bond = _AtomBonds[input[0].id];

		int atom_aa_id_1 = _AtomAminoAcidIds[bond.x];
		int atom_aa_id_2 = _AtomAminoAcidIds[bond.y];

		if(atom_aa_id_1 != atom_aa_id_2) return;
		
		//int culled_atom_1 = _CulledAtoms[bond.x];
		//int culled_atom_2 = _CulledAtoms[bond.y];

		//if(culled_atom_1 < 1 && culled_atom_2 < 1) return;

		int focused_atom_1 = _FocusedAtoms[bond.x];
		int focused_atom_2 = _FocusedAtoms[bond.y];

		if(focused_atom_1 < 1 && focused_atom_1 < 1) return;

		/****/

		int atom_type_1 = _AtomTypes[bond.x];
		int atom_type_2 = _AtomTypes[bond.y];

		int atom_aa_type_1 = _AtomAminoAcidTypes[bond.x];
		int atom_aa_type_2 = _AtomAminoAcidTypes[bond.y];

		float3 atom_pos_1 = _AtomPositions[bond.x].xyz * _Scale;
		float3 atom_pos_2 = _AtomPositions[bond.y].xyz * _Scale;
		
		float stick_radius = _Scale * _StickRadius;

		// Find vector perpendical to the screen and the stick
		float3 cross_pos = normalize(cross(atom_pos_2 - atom_pos_1, _WorldSpaceCameraPos - atom_pos_1));
		
		// Find stick radii
		float3 stick_corner_1 = atom_pos_2 + cross_pos * stick_radius;
		float3 stick_corner_2 = atom_pos_2 - cross_pos * stick_radius;
		float3 stick_corner_3 = atom_pos_1 + cross_pos * stick_radius;
		float3 stick_corner_4 = atom_pos_1 - cross_pos * stick_radius;
				
		// Find stick normal
		float3 stick_norm = normalize(cross(stick_corner_4 - stick_corner_2, stick_corner_3 - stick_corner_2));
				
		// Find depth offset
		float3 dd = normalize(_WorldSpaceCameraPos - atom_pos_1);
		float cos_a = dot(dd, stick_norm);		
		float delta_z = max(stick_radius, min((stick_radius) / cos_a, stick_radius * 1.0));		
				
		float4 pp_1 = mul(UNITY_MATRIX_MVP, float4(stick_corner_1, 1));
		float4 pp_2 = mul(UNITY_MATRIX_MVP, float4(stick_corner_2, 1));
		float4 pp_3 = mul(UNITY_MATRIX_MVP, float4(stick_corner_3, 1));
		float4 pp_4 = mul(UNITY_MATRIX_MVP, float4(stick_corner_4, 1));
		
		// Send vertices to render		
		gs2fs output;	
		output.delta_z = delta_z;	
		output.aa_type = atom_aa_type_1;

		output.pos = pp_1;
		output.uv = float2(1,1);
		triangleStream.Append(output);	

		output.pos = pp_2;
		output.uv = float2(1,-1);
		triangleStream.Append(output);	

		output.pos = pp_3;
		output.uv = float2(-1,1);
		triangleStream.Append(output);	

		output.pos = pp_4;
		output.uv = float2(-1,-1);
		triangleStream.Append(output);			
	}
						
	ENDCG	
	
	SubShader 	
	{	
		// First pass
	    Pass 
	    {
			//Blend SrcAlpha OneMinusSrcAlpha
			//BlendOp Sub
			ZWrite On
			//Blend One One

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
			//Blend SrcAlpha OneMinusSrcAlpha
			//BlendOp Sub
			ZWrite On
			//Blend One One

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS				
			#pragma fragment FS
			#pragma geometry GS_2			
		
			ENDCG
		}		
	}
	Fallback Off
}	