using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

struct TunnelSphere
{
    public float Radius;
    public Vector4 Position;
}

struct Atom
{
    public int Type;
    public Vector4 Position;
}

public class LogicScript : MonoBehaviour
{
    /*** static attributes ***/

    public static int VolumeSize = 128;
    public static float GlobalScale = 2.0f;

    public static int NumAtoms = 0;
    public static int NumAtomBonds = 0;
    public static int NumTunnelSpheres = 0;

    public static ComputeBuffer _culledAtomsBuffer;
    public static ComputeBuffer _focusedAtomBuffer;

    public static ComputeBuffer _atomBondsBuffer;
    public static ComputeBuffer _atomTypesBuffer;
    public static ComputeBuffer _atomRadiiBuffer;
    public static ComputeBuffer _atomColorsBuffer;
    public static ComputeBuffer _atomPositionsBuffer;
    public static ComputeBuffer _aminoAcidColorsBuffer;
    public static ComputeBuffer _atomAminoAcidIdBuffer;
    public static ComputeBuffer _atomAminoAcidTypesBuffer;
    public static ComputeBuffer _atomDisplayPositionsBuffer;
    
    public static ComputeBuffer _tunnelColorBuffer;
    public static ComputeBuffer _tunnelRadiiBuffer;
    public static ComputeBuffer _tunnelPositionsBuffer;
    
    /*** private const attributes ***/
    
    private const int NumAtomMax = 10000;
    private const int NumFieldsPerAtom = 6;
    private const int NumTunnelSphereMax = 1000;
    private const int AtomSize = NumFieldsPerAtom * sizeof(float);

    private const string AtomDataFilePath =  "/../Data/atoms/atom_data.bin";
    private const string TunnelDataFilePath = "/../Data/tunnels/tunnel_data.bin";
    private const string TunnelIndexFilePath = "/../Data/tunnels/tunnel_index.bin";

    /*** private molecule attributes ***/

    private int[] _atomBonds;
    private int[] _atomTypes = new int[NumAtomMax];
    private int[] _atomAminoAcidIds = new int[NumAtomMax];
    private int[] _atomAminoAcidsTypes = new int[NumAtomMax];

    private Vector4[] _atomPositions = new Vector4[NumAtomMax];
    private Vector4[] _atomDisplayPositions = new Vector4[NumAtomMax];

    /*** private punnel attributes ***/

    private float[] _tunnelRadii = new float[NumTunnelSphereMax];
    private float[] _tunnelDisplayRadii = new float[NumTunnelSphereMax];
    
    private Vector4[] _tunnelPositions = new Vector4[NumTunnelSphereMax];
    private Vector4[] _tunnelDisplayPositions = new Vector4[NumTunnelSphereMax];

    /*** misc private attributes ***/
    
    private bool _init = false;
    private bool _pause = true;
    private bool _resetDisplayPositions = false;
    
    private int _numFrames = 0;
    private int _molFrameSize = 0;
    private int _currentFrame = 0;
    private int _previousFrame = -1;
    private int _nextFrameCount = 0;
    private int _previousNumTunnelSpheres = 0; 

    private int[] _tunnelFrameSizes;
    private int[] _tunnelFrameOffsets;

    /*** inspector exposed attributes ***/

    public ComputeShader UpdateAtomsShader;

    [RangeAttribute(1, 10)]
    public int NextFrameDelay = 1;

    [RangeAttribute(1, 100)]
    public int TemporalResolution = 1;

    [RangeAttribute(0, 1)]
    public float SpeedReduction = 1;

    [RangeAttribute(0, 0.1f)] 
    public float SpeedReductionMin = 0;

    [RangeAttribute(1, 20)]
    public int CurrentTunnel = 1;

    /*** body ***/

    void CreateBuffers()
    {
        if (_tunnelRadiiBuffer == null) _tunnelRadiiBuffer = new ComputeBuffer(NumTunnelSphereMax, 4);
        if (_tunnelColorBuffer == null) _tunnelColorBuffer = new ComputeBuffer(NumTunnelSphereMax, 16);
        if (_tunnelPositionsBuffer == null) _tunnelPositionsBuffer = new ComputeBuffer(NumTunnelSphereMax, 16);

        if (_atomTypesBuffer == null) _atomTypesBuffer = new ComputeBuffer(NumAtomMax, 4);
        if (_culledAtomsBuffer == null) _culledAtomsBuffer = new ComputeBuffer(NumAtomMax, 4);
        if (_focusedAtomBuffer == null) _focusedAtomBuffer = new ComputeBuffer(NumAtomMax, 4);
        if (_atomPositionsBuffer == null) _atomPositionsBuffer = new ComputeBuffer(NumAtomMax, 16);
        if (_atomAminoAcidIdBuffer == null) _atomAminoAcidIdBuffer = new ComputeBuffer(NumAtomMax, 4);
        if (_atomRadiiBuffer == null) _atomRadiiBuffer = new ComputeBuffer(PdbReader.AtomSymbols.Length, 4);
        if (_atomAminoAcidTypesBuffer == null) _atomAminoAcidTypesBuffer = new ComputeBuffer(NumAtomMax, 4);
        if (_atomColorsBuffer == null) _atomColorsBuffer = new ComputeBuffer(PdbReader.AtomSymbols.Length, 16);
        if (_atomDisplayPositionsBuffer == null) _atomDisplayPositionsBuffer = new ComputeBuffer(NumAtomMax, 16);
        if (_aminoAcidColorsBuffer == null) _aminoAcidColorsBuffer = new ComputeBuffer(PdbReader.AminoAcidColors.Length, 16);
        
        _atomRadiiBuffer.SetData(PdbReader.AtomRadii);
        _atomColorsBuffer.SetData(PdbReader.AtomColors);
        _aminoAcidColorsBuffer.SetData(PdbReader.AminoAcidColors);
    }

    void DestroyBuffers()
    {
        if (_tunnelPositionsBuffer != null) _tunnelPositionsBuffer.Release();
        if (_tunnelColorBuffer != null) _tunnelColorBuffer.Release();
        if (_tunnelRadiiBuffer != null) _tunnelRadiiBuffer.Release();

        if (_atomDisplayPositionsBuffer != null) _atomDisplayPositionsBuffer.Release();
        if (_atomPositionsBuffer != null) _atomPositionsBuffer.Release();
        if (_atomAminoAcidTypesBuffer != null) _atomAminoAcidTypesBuffer.Release();
        if (_atomAminoAcidIdBuffer != null) _atomAminoAcidIdBuffer.Release();
        if (_atomTypesBuffer != null) _atomTypesBuffer.Release();
        if (_atomRadiiBuffer != null) _atomRadiiBuffer.Release();
        if (_atomColorsBuffer != null) _atomColorsBuffer.Release();
        if (_aminoAcidColorsBuffer != null) _aminoAcidColorsBuffer.Release();
        
        if (_atomBondsBuffer != null) _atomBondsBuffer.Release();
        if (_focusedAtomBuffer != null) _focusedAtomBuffer.Release();
        if (_culledAtomsBuffer != null) _culledAtomsBuffer.Release();
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        Debug.Log("Create Compute Buffers");
        CreateBuffers();
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;
        Debug.Log("Destroy Compute Buffers");
        DestroyBuffers();
    }

    void Start()
    {
        if (!File.Exists(Application.dataPath + AtomDataFilePath)) throw new Exception("No file found at: " + AtomDataFilePath);
        if (!File.Exists(Application.dataPath + TunnelDataFilePath)) throw new Exception("No file found at: " + TunnelDataFilePath);
        if (!File.Exists(Application.dataPath + TunnelIndexFilePath)) throw new Exception("No file found at: " + TunnelIndexFilePath);

        _numFrames = 2000;
        NumAtoms = (int)(new FileInfo(Application.dataPath + AtomDataFilePath).Length / _numFrames / AtomSize);
        _molFrameSize = NumAtoms * AtomSize;

        // Read tunnel frame sizes
        _tunnelFrameSizes = new int[_numFrames];
        var tempBuffer = File.ReadAllBytes(Application.dataPath + TunnelIndexFilePath);

        Buffer.BlockCopy(tempBuffer, 0, _tunnelFrameSizes, 0, tempBuffer.Length);

        // Find tunnel frame offsets
        _tunnelFrameOffsets = new int[_numFrames];
        _tunnelFrameOffsets[0] = 0;

        for (int i = 1; i < _numFrames; i++) _tunnelFrameOffsets[i] = _tunnelFrameOffsets[i - 1] + _tunnelFrameSizes[i - 1];
    }

    void OnGUI()
    {
        //GUI.contentColor = Color.black;
        //GUILayout.Label("Current frame: " + currentFrame);

        var progress = (float)_currentFrame / (float)_numFrames;
        var newProgress = GUI.HorizontalSlider(new Rect(25, Screen.height - 25, Screen.width - 50, 30), progress, 0.0f, 1.0f);

        if (progress != newProgress)
        {
            _currentFrame = (int)(((float)_numFrames - 1.0f) * newProgress);
            _resetDisplayPositions = true;
        }
    }
    
    // Create in/out function
    void LoadMoleculeFrame(int frame)
    {
        var fs = new FileStream(Application.dataPath + AtomDataFilePath, FileMode.Open);
        var offset = (long)frame * _molFrameSize;
        var frameBytes = new byte[_molFrameSize];
        var frameData = new float[_molFrameSize / sizeof(float)];

        fs.Seek(offset, SeekOrigin.Begin);
        fs.Read(frameBytes, 0, _molFrameSize);
        fs.Close();

        Buffer.BlockCopy(frameBytes, 0, frameData, 0, _molFrameSize);

        // ...
        for (var i = 0; i < NumAtoms; i++)
        {
            _atomPositions[i].Set(frameData[i * NumFieldsPerAtom + 3], frameData[i * NumFieldsPerAtom + 4], frameData[i * NumFieldsPerAtom + 5], 1);
            
            if (_init) continue;

            _atomTypes[i] = (int)frameData[i * NumFieldsPerAtom];
            _atomAminoAcidIds[i] = (int)frameData[i * NumFieldsPerAtom + 1];
            _atomAminoAcidsTypes[i] = (int)frameData[i * NumFieldsPerAtom + 2];
        }

        _atomPositionsBuffer.SetData(_atomPositions);

        if (!_init)
        {
            // Set buffer data
            _atomTypesBuffer.SetData(_atomTypes);
            _atomAminoAcidIdBuffer.SetData(_atomAminoAcidIds);
            _atomAminoAcidTypesBuffer.SetData(_atomAminoAcidsTypes);

            // Find bonds
            _atomBonds = PdbReader.GetAtomBonds(_atomPositions, _atomTypes, NumAtoms);
            NumAtomBonds = _atomBonds.Length / 2;

            _atomBondsBuffer = new ComputeBuffer(NumAtomBonds, 8);
            _atomBondsBuffer.SetData(_atomBonds);

            // Find focused atoms
            var aaa = new List<int>();
            for (var i = 0; i < NumAtoms; i++)
            {
                var ap = _atomPositions[i];

                if (aaa.Count > 0 && aaa.Last() == _atomAminoAcidIds[i]) continue;

                for (var j = 0; j < NumTunnelSpheres; j++)
                {
                    var tsp = _tunnelPositions[j];

                    if (!(Vector3.Distance(ap, tsp) < 5)) continue;
                    aaa.Add(_atomAminoAcidIds[i]);
                    break;
                }
            }

            var aaaa = new int[NumAtoms];
            for (var i = 0; i < NumAtoms; i++)
            {
               aaaa[i] = Convert.ToInt32(aaa.Contains(_atomAminoAcidIds[i]));
            }
            _focusedAtomBuffer.SetData(aaaa);

            _init = true;
        }
    }
    
    // Create in/out function
    void LoadTunnelFrame(int frame)
    {
        var frameSize = _tunnelFrameSizes[frame];
        if (frameSize == 0)
        {
            //NumTunnelSpheres = 0;
            return;
        }

        var offset = (long)_tunnelFrameOffsets[frame];
        var frameBytes = new byte[frameSize];
        var frameData = new float[frameSize / sizeof(float)];
        var numSpheres = frameData.Length / 5;

        var fs = new FileStream(Application.dataPath + TunnelDataFilePath, FileMode.Open);
        fs.Seek(offset, SeekOrigin.Begin);
        fs.Read(frameBytes, 0, frameSize);
        fs.Close();

        Buffer.BlockCopy(frameBytes, 0, frameData, 0, frameSize);

        // Build tunnels data structure
        var tunnels = new SortedDictionary<int, List<TunnelSphere>>();
        for (int i = 0; i < numSpheres; i++)
        {
            int tunnelId = (int)frameData[i * 5 + 4];
            if (!tunnels.ContainsKey(tunnelId)) tunnels[tunnelId] = new List<TunnelSphere>();
            tunnels[tunnelId].Add(new TunnelSphere()
            {
                Position = new Vector4(frameData[i * 5 + 0], frameData[i * 5 + 1], frameData[i * 5 + 2], 1),
                Radius = frameData[i * 5 + 3]
            });
        }

        // Only display one tunnel 
        var displayTunnelSpheres = tunnels.First().Value.ToArray();
        for (int i = 0; i < displayTunnelSpheres.Length; i++)
        {
            _tunnelPositions[i] = displayTunnelSpheres[i].Position;
            _tunnelRadii[i] = displayTunnelSpheres[i].Radius;
        }

        _previousNumTunnelSpheres = NumTunnelSpheres;
        NumTunnelSpheres = displayTunnelSpheres.Length;

        //Debug.Log(NumTunnelSpheres);
    }
    
    void Update()
    {
        if (_currentFrame != _previousFrame)
        {
            LoadTunnelFrame(_currentFrame);
            LoadMoleculeFrame(_currentFrame);
            
            if (_currentFrame == 0 || _resetDisplayPositions)
            {
                Array.Copy(_atomPositions, _atomDisplayPositions, NumAtoms);
                Array.Copy(_tunnelPositions, _tunnelDisplayPositions, NumTunnelSphereMax);

                _previousNumTunnelSpheres = NumTunnelSpheres;
                _resetDisplayPositions = false;
            }

            _previousFrame = _currentFrame;
        }

        UpdateAtomDisplayPositions();
        UpdateTunnelDisplayPositions();

        if (Input.GetKeyDown(KeyCode.Space)) _pause = !_pause;

        if (!_pause)
        {
            _nextFrameCount++;

            if (_nextFrameCount >= NextFrameDelay)
            {
                _currentFrame += TemporalResolution;
                _nextFrameCount = 0;
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _currentFrame += TemporalResolution;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _currentFrame -= TemporalResolution;
        }

        if (_currentFrame > _numFrames - 1) _currentFrame = 0;
        if (_currentFrame < 0) _currentFrame = _numFrames - 1;
    }
    
    void UpdateAtomDisplayPositions()
    {
        //for (int i = 0; i < NumAtoms; i++)
        //{
            //float minDist = 100;
            //float maxDist = 10;

            //for (int j = 0; j < NumTunnelSpheres; j++)
            //{
            //    float dist = Mathf.Min(Vector3.Distance(_atomPositions[i], _tunnelPositions[j]), minDist);
            //    if (dist < minDist)
            //    {
            //        minDist = dist;
            //    }	
            //}

            //float speedReduction = 1 - (Mathf.Min(minDist, maxDist) / maxDist);

            //speedReduction *= SpeedReduction;
            //speedReduction += SpeedReductionMin;

            //_atomDisplayPositions[i] += (_atomPositions[i] - _atomDisplayPositions[i]) * Mathf.Min(speedReduction, 1);
        //}
        
        UpdateAtomsShader.SetInt("_NumAtoms", NumAtoms);
        UpdateAtomsShader.SetInt("_NumTunnelSpheres", NumTunnelSpheres);
        UpdateAtomsShader.SetFloat("_SpeedReduction", SpeedReduction);
        UpdateAtomsShader.SetFloat("_SpeedReductionMin", SpeedReductionMin);
        UpdateAtomsShader.SetBuffer(0, "_TunnelRadii", _tunnelRadiiBuffer);
        UpdateAtomsShader.SetBuffer(0, "_AtomPositions", _atomPositionsBuffer);
        UpdateAtomsShader.SetBuffer(0, "_TunnelPositions", _tunnelPositionsBuffer);
        UpdateAtomsShader.SetBuffer(0, "_AtomDisplayPositions", _atomDisplayPositionsBuffer);

        UpdateAtomsShader.Dispatch(0, (int)(Mathf.Ceil(NumAtoms / 64.0f)), 1, 1);
    }

    void UpdateTunnelDisplayPositions()
    {
        float speedReduction = 1;

        speedReduction *= SpeedReduction;
        speedReduction += SpeedReductionMin;

        for (int i = 0; i < NumTunnelSpheres; i++)
        {
            if (i < _previousNumTunnelSpheres)
            {
                _tunnelDisplayPositions[i] += (_tunnelPositions[i] - _tunnelDisplayPositions[i]) * speedReduction;
                _tunnelDisplayRadii[i] += (_tunnelRadii[i] - _tunnelDisplayRadii[i]) * speedReduction;
            }
            else
            {
                _tunnelDisplayPositions[i] = _tunnelDisplayPositions[i - 1];
                _tunnelDisplayPositions[i] += (_tunnelPositions[i] - _tunnelDisplayPositions[i]) * speedReduction;

                _tunnelDisplayRadii[i] = _tunnelDisplayRadii[i - 1];
                _tunnelDisplayRadii[i] += (_tunnelRadii[i] - _tunnelDisplayRadii[i]) * speedReduction;
            }
        }
        
        _tunnelRadiiBuffer.SetData(_tunnelDisplayRadii);
        _tunnelPositionsBuffer.SetData(_tunnelDisplayPositions);
    }
}
