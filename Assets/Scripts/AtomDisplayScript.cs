using System.Collections.Generic;
using UnityEngine;

public class AtomDisplayScript : MonoBehaviour
{
    public Shader AtomShader;
    public Shader BondShader;
    public Shader TunnelShader;
    public Shader CompositeShader;
    public Shader DepthBlitShader;

    public ComputeShader CullAtoms;
    public ComputeShader FindBonds;

    private Material _atomMaterial;
    private Material _bondMaterial;
    private Material _tunnelMaterial;
    private Material _compositeMaterial;
    private Material _depthBlitMaterial;

    private RenderTexture _cameraDepthTexture;

    /****/
    
    [RangeAttribute(0, 1)]
    public float AtomRadius = 0.25f;
    
    [RangeAttribute(0, 1)]
    public float ContextAtomRadius = 0.25f;

    [RangeAttribute(0, 1)]
    public float StickRadius = 0.20f;

    [RangeAttribute(0, 1)]
    public float ContextStickRadius = 0.25f;

    void Start()
    {
        _atomMaterial = new Material(AtomShader) { hideFlags = HideFlags.HideAndDontSave };
        _bondMaterial = new Material(BondShader) { hideFlags = HideFlags.HideAndDontSave };
        _tunnelMaterial = new Material(TunnelShader) { hideFlags = HideFlags.HideAndDontSave };
        _compositeMaterial = new Material(CompositeShader) { hideFlags = HideFlags.HideAndDontSave };
        _depthBlitMaterial = new Material(DepthBlitShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnDestroy()
    {
        if (_atomMaterial != null) { DestroyImmediate(_atomMaterial); _atomMaterial = null; }
        if (_bondMaterial != null) { DestroyImmediate(_bondMaterial); _bondMaterial = null; }
        if (_tunnelMaterial != null) { DestroyImmediate(_tunnelMaterial); _tunnelMaterial = null; }
        if (_compositeMaterial != null) { DestroyImmediate(_compositeMaterial); _compositeMaterial = null; }
        if (_depthBlitMaterial != null) { DestroyImmediate(_depthBlitMaterial); _depthBlitMaterial = null; }
    }
    
    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (_cameraDepthTexture != null && (_cameraDepthTexture.width != src.width || _cameraDepthTexture.height != src.height)){ _cameraDepthTexture.Release(); _cameraDepthTexture = null; }
        if (_cameraDepthTexture == null) _cameraDepthTexture = new RenderTexture(src.width, dst.height, 24, RenderTextureFormat.Depth);

        // Clear depth buffer
        Graphics.SetRenderTarget(_cameraDepthTexture);
        GL.Clear(true, true, Color.white);
        //Graphics.Blit(src, _depthNormalsBlitMaterial, 0);

        CullAtoms.SetInt("_AtomCount", LogicScript.NumAtoms);
        CullAtoms.SetInt("_TunnelSphereCount", LogicScript.NumTunnelSpheres);
        CullAtoms.SetFloat("_AtomRadius", AtomRadius);
        CullAtoms.SetFloat("_Scale", LogicScript.GlobalScale / LogicScript.VolumeSize);
        CullAtoms.SetVector("_WorldSpaceCameraPos", gameObject.transform.position);
        CullAtoms.SetBuffer(0, "_AtomRadii", LogicScript._atomRadiiBuffer);
        CullAtoms.SetBuffer(0, "_AtomTypes", LogicScript._atomTypesBuffer);
        CullAtoms.SetBuffer(0, "_AtomPositions", LogicScript._atomDisplayPositionsBuffer);
        CullAtoms.SetBuffer(0, "_TunnelSphereRadii", LogicScript._tunnelRadiiBuffer);
        CullAtoms.SetBuffer(0, "_TunnelSpherePositions", LogicScript._tunnelPositionsBuffer);
        CullAtoms.SetBuffer(0, "_CulledAtoms", LogicScript._culledAtomsBuffer);
        
        // Cull atoms
        CullAtoms.Dispatch(0, (int)(Mathf.Ceil(LogicScript.NumAtoms / 64.0f)), 1, 1);
        
        _atomMaterial.SetFloat("_Scale", LogicScript.GlobalScale / LogicScript.VolumeSize);
        _atomMaterial.SetFloat("_AtomRadius", AtomRadius);
        _atomMaterial.SetFloat("_ContextAtomRadius", ContextAtomRadius);
        _atomMaterial.SetBuffer("atomTypes", LogicScript._atomTypesBuffer);
        _atomMaterial.SetBuffer("atomRadii", LogicScript._atomRadiiBuffer);
        _atomMaterial.SetBuffer("atomColors", LogicScript._atomColorsBuffer);
        _atomMaterial.SetBuffer("atomPositions", LogicScript._atomDisplayPositionsBuffer);
        _atomMaterial.SetBuffer("atomAminoAcidTypes", LogicScript._atomAminoAcidTypesBuffer);
        _atomMaterial.SetBuffer("aminoAcidColors", LogicScript._aminoAcidColorsBuffer);
        _atomMaterial.SetBuffer("_CulledAtoms", LogicScript._culledAtomsBuffer);
        _atomMaterial.SetBuffer("_FocusedAtoms", LogicScript._focusedAtomBuffer);

        // Draw atoms
        Graphics.SetRenderTarget(src.colorBuffer, _cameraDepthTexture.depthBuffer);
        _atomMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, LogicScript.NumAtoms);

        // Draw focused atoms
        Graphics.SetRenderTarget(src.colorBuffer, _cameraDepthTexture.depthBuffer);
        _atomMaterial.SetPass(1);
        Graphics.DrawProcedural(MeshTopology.Points, LogicScript.NumAtoms);
        
        _bondMaterial.SetFloat("_StickRadius", StickRadius);
        _bondMaterial.SetFloat("_ContextStickRadius", ContextStickRadius);
        _bondMaterial.SetFloat("_Scale", LogicScript.GlobalScale / LogicScript.VolumeSize);
        _bondMaterial.SetBuffer("_AtomBonds", LogicScript._atomBondsBuffer);
        _bondMaterial.SetBuffer("_AtomTypes", LogicScript._atomTypesBuffer);
        _bondMaterial.SetBuffer("_AtomPositions", LogicScript._atomDisplayPositionsBuffer);
        _bondMaterial.SetBuffer("_AtomAminoAcidIds", LogicScript._atomAminoAcidIdBuffer);
        _bondMaterial.SetBuffer("_AtomAminoAcidTypes", LogicScript._atomAminoAcidTypesBuffer);
        _bondMaterial.SetBuffer("_AminoAcidColors", LogicScript._aminoAcidColorsBuffer);
        _bondMaterial.SetBuffer("_CulledAtoms", LogicScript._culledAtomsBuffer);
        _bondMaterial.SetBuffer("_FocusedAtoms", LogicScript._focusedAtomBuffer);

        // Draw bonds
        Graphics.SetRenderTarget(src.colorBuffer, _cameraDepthTexture.depthBuffer);
        _bondMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, LogicScript.NumAtomBonds);

        // Draw focused bonds
        Graphics.SetRenderTarget(src.colorBuffer, _cameraDepthTexture.depthBuffer);
        _bondMaterial.SetPass(1);
        Graphics.DrawProcedural(MeshTopology.Points, LogicScript.NumAtomBonds);

        // Declare render texture and clear
        var tunnelAlphaTexture = RenderTexture.GetTemporary(src.width, dst.height, 24, RenderTextureFormat.ARGB32);
        Graphics.SetRenderTarget(tunnelAlphaTexture.colorBuffer, _cameraDepthTexture.depthBuffer);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        
        _tunnelMaterial.SetFloat("scale", LogicScript.GlobalScale / LogicScript.VolumeSize);
        _tunnelMaterial.SetBuffer("tunnelRadii", LogicScript._tunnelRadiiBuffer);
        _tunnelMaterial.SetBuffer("tunnelPositions", LogicScript._tunnelPositionsBuffer);

        // Draw tunnel
        Graphics.SetRenderTarget(tunnelAlphaTexture.colorBuffer, _cameraDepthTexture.depthBuffer);
        _tunnelMaterial.SetPass(1);
        Graphics.DrawProcedural(MeshTopology.Points, LogicScript.NumTunnelSpheres);

        // Set depth global
        Shader.SetGlobalTexture("_CameraDepthTexture", _cameraDepthTexture);

        // Composite pass
        _compositeMaterial.SetTexture("_BlendTex", tunnelAlphaTexture);
        Graphics.Blit(src, dst, _compositeMaterial);
        RenderTexture.ReleaseTemporary(tunnelAlphaTexture);

        //Graphics.Blit(src, dst);
    }
}