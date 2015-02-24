using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SurfaceDisplayScript : MonoBehaviour
{
    public Shader compositeShader;
    public Shader renderBackDepthShader;
    public Shader rayMarchSurfaceShader;
    public Shader DepthNormalsBlitShader;

    public ComputeShader ClearVolume;
    public ComputeShader FillVolume;
    public ComputeShader BlitVolume;

    public Mesh CubeMesh;
    public Color SurfaceColor;
    
    [Range(0, 1)]
    public float Opacity = 1;

    [Range(32, 512)]
    public int NumSteps = 256;

    [Range(0.5f, 10)]
    public float SurfaceSmoothness = 0.8f;

    [Range(0.0f, 1)]
    public float IntensityThreshold = 0.8f;

    private Material _rayMarchMaterial;
    private Material _compositeMaterial;
    private Material _backDepthMaterial;
    private Material _depthNormalsBlitMaterial;

    private ComputeBuffer _voxelBuffer;
    private RenderTexture _volumeTexture;

    private void Start()
    {
        _compositeMaterial = new Material(compositeShader) { hideFlags = HideFlags.HideAndDontSave };
        _rayMarchMaterial = new Material(rayMarchSurfaceShader) { hideFlags = HideFlags.HideAndDontSave };
        _backDepthMaterial = new Material(renderBackDepthShader) { hideFlags = HideFlags.HideAndDontSave };
        _depthNormalsBlitMaterial = new Material(DepthNormalsBlitShader) { hideFlags = HideFlags.HideAndDontSave };

        _voxelBuffer = new ComputeBuffer(LogicScript.VolumeSize * LogicScript.VolumeSize * LogicScript.VolumeSize, sizeof(float), ComputeBufferType.Default);

        _volumeTexture = new RenderTexture(LogicScript.VolumeSize, LogicScript.VolumeSize, 0, RenderTextureFormat.RFloat);
        _volumeTexture.volumeDepth = LogicScript.VolumeSize;
        _volumeTexture.isVolume = true;
        _volumeTexture.enableRandomWrite = true;
        _volumeTexture.filterMode = FilterMode.Trilinear;
        _volumeTexture.Create();
    }

    private void OnDestroy()
    {
        if (_voxelBuffer != null) _voxelBuffer.Release(); _voxelBuffer = null;
        if (_volumeTexture != null) _volumeTexture.Release(); _volumeTexture = null;
    }

    //[ImageEffectOpaque]
    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // Init the volume data with zeros 
        ClearVolume.SetInt("_VolumeSize", LogicScript.VolumeSize);
        ClearVolume.SetFloat("_ClearValue", 0);
        ClearVolume.SetBuffer(0, "_VoxelBuffer", _voxelBuffer);
        ClearVolume.Dispatch(0, LogicScript.VolumeSize / 8, LogicScript.VolumeSize / 8, LogicScript.VolumeSize / 8);

        // Fill the volume data with atom values
        FillVolume.SetInt("_VolumeSize", LogicScript.VolumeSize);
        FillVolume.SetInt("_AtomCount", LogicScript.NumAtoms);
        FillVolume.SetFloat("_Scale", LogicScript.GlobalScale);
        FillVolume.SetFloat("_SurfaceSmoothness", SurfaceSmoothness);
        FillVolume.SetBuffer(0, "_AtomTypesBuffer", LogicScript._atomTypesBuffer);
        FillVolume.SetBuffer(0, "_AtomRadiiBuffer", LogicScript._atomRadiiBuffer);
        FillVolume.SetBuffer(0, "_AtomPositionsBuffer", LogicScript._atomDisplayPositionsBuffer);
        FillVolume.SetBuffer(0, "_VoxelBuffer", _voxelBuffer);
        FillVolume.Dispatch(0, (int)Mathf.Ceil((LogicScript.NumAtoms) / 64.0f), 1, 1);

        // Blit linear buffer in 3D texture
        BlitVolume.SetInt("_VolumeSize", LogicScript.VolumeSize);
        BlitVolume.SetBuffer(0, "_VoxelBuffer", _voxelBuffer);
        BlitVolume.SetTexture(0, "_VolumeTexture", _volumeTexture);
        BlitVolume.Dispatch(0, LogicScript.VolumeSize / 8, LogicScript.VolumeSize / 8, LogicScript.VolumeSize / 8);

        var backDepth = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGBFloat);
        var volumeTarget = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        var cameraDepthBuffer = RenderTexture.GetTemporary(src.width, dst.height, 24, RenderTextureFormat.Depth);
        var cameraDepthNormalBuffer = RenderTexture.GetTemporary(src.width, dst.height, 24, RenderTextureFormat.ARGB32);
                
        // Draw cube back depth
        _backDepthMaterial.SetPass(0);
        Graphics.SetRenderTarget(backDepth);        
        Graphics.DrawMeshNow(CubeMesh, Vector3.zero, Quaternion.identity);

        // Fetch Unity's depth/normals
        Graphics.SetRenderTarget(cameraDepthNormalBuffer.colorBuffer, cameraDepthBuffer.depthBuffer);
        Graphics.Blit(src, _depthNormalsBlitMaterial, 0);

        Graphics.SetRenderTarget(volumeTarget);
        GL.Clear(true, true, new Color(0,0,0,0));

        // Render volume
        Graphics.SetRenderTarget(new []{volumeTarget.colorBuffer, cameraDepthNormalBuffer.colorBuffer}, cameraDepthBuffer.depthBuffer);
        _rayMarchMaterial.SetInt("_VolumeSize", LogicScript.VolumeSize);
        _rayMarchMaterial.SetFloat("_Opacity", Opacity);
        _rayMarchMaterial.SetFloat("_StepSize", 1.0f / NumSteps);
        _rayMarchMaterial.SetFloat("_IntensityThreshold", IntensityThreshold);
        _rayMarchMaterial.SetColor("_SurfaceColor", SurfaceColor);
        _rayMarchMaterial.SetTexture("_CubeBackTex", backDepth);
        _rayMarchMaterial.SetTexture("_VolumeTex", _volumeTexture);
        _rayMarchMaterial.SetPass(0);

        Graphics.DrawMeshNow(CubeMesh, Vector3.zero, Quaternion.identity);

        // Change unity's camera depth texture with our own depth buffer
        //Shader.SetGlobalTexture("_CameraDepthTexture", cameraDepthBuffer);
        //Shader.SetGlobalTexture("_CameraDepthNormalsTexture", cameraDepthNormalBuffer);

        //// Composite pass
        _compositeMaterial.SetTexture("_BlendTex", volumeTarget);
        Graphics.Blit(src, dst, _compositeMaterial);

        RenderTexture.ReleaseTemporary(cameraDepthNormalBuffer);
        RenderTexture.ReleaseTemporary(cameraDepthBuffer);
        RenderTexture.ReleaseTemporary(volumeTarget);
        RenderTexture.ReleaseTemporary(backDepth);
    }
}