using System;
using CustomAttributes;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class CylinderRingsController : MonoBehaviour
{
    [Header("Cylinder Settings")] 
    public float scaleOneHeight = 2f;
    
    [Header("Wave Settings (World Space)")]
    public float waveLength = 1f;
    public float waveSpeed = 2f;

    [Header("Thickness Mask")]
    [CurveRange(0,0,1,1)]public AnimationCurve thicknessCurve = AnimationCurve.Linear(0, 0.5f, 1, 0.0f);
    public int textureResolution = 64;

    private Renderer _renderer;
    private MeshFilter _meshFilter;
    private MaterialPropertyBlock _propBlock;
    private Texture2D _curveTexture;

    Vector3 _lastScale = Vector3.one;
    
    private static readonly int RingsCountId = Shader.PropertyToID("_RingsCount");
    private static readonly int SpeedUVId = Shader.PropertyToID("_SpeedUV");
    private static readonly int CurveTexId = Shader.PropertyToID("_CurveTex");

    private void OnEnable() => Initialize();
    private void OnValidate() => Initialize();

    private void Initialize()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

        
        
        BakeCurveTexture();
        UpdateMaterialProperties();
    }

    private void BakeCurveTexture()
    {
        if (_curveTexture == null || _curveTexture.width != textureResolution)
        {
            _curveTexture = new Texture2D(textureResolution, 1, TextureFormat.R8, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        for (int i = 0; i < textureResolution; i++)
        {
            float t = i / (float)(textureResolution - 1);
            float val = Mathf.Clamp01(thicknessCurve.Evaluate(t));
            _curveTexture.SetPixel(i, 0, new Color(val, 0, 0, 0));
        }
        _curveTexture.Apply();
    }

    private void UpdateMaterialProperties()
    {
        if (_renderer == null) return;

        float safeWaveLength = Mathf.Max(0.001f, waveLength);
        
        float worldHeight = scaleOneHeight * transform.lossyScale.y;

        float ringsCount = worldHeight / safeWaveLength; 
        
        float phaseSpeed = waveSpeed / safeWaveLength;         

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetFloat(RingsCountId, ringsCount);
        _propBlock.SetFloat(SpeedUVId, phaseSpeed);

        if (_curveTexture != null)
        {
            _propBlock.SetTexture(CurveTexId, _curveTexture);
        }

        _renderer.SetPropertyBlock(_propBlock);
    }

    private void Update()
    {
        if (transform.lossyScale != _lastScale)
        {
            Initialize();
            _lastScale = transform.lossyScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.aquamarine;
        var size = Vector3.up * scaleOneHeight;
        size.x = 0.3f;
        size.z = 0.3f;
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
