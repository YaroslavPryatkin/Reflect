using CustomAttributes;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class SimpleElectricArc : MonoBehaviour
{
    [Header("Arc Endpoints")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Arc Displacement Amplitudes")]
    public float amplitudeX = 0.5f;
    public float amplitudeY = 0.5f;
    public float amplitudeZ = 0.5f;
    
    [Header("Scale amplitudes with distance between points")]
    public bool scaleX = false;
    public bool scaleY = false;
    public bool scaleZ = true;
    
    [Header("Arc statics")] 
    [CurveRange(0f,-1f,1f,1f)] public AnimationCurve horizontalShiftX = new (new Keyframe(0, 0), new Keyframe(1, 0));
    [CurveRange(0f,-1f,1f,1f)] public AnimationCurve horizontalShiftY = new (new Keyframe(0, 0), new Keyframe(1, 0));
    [CurveRange(0f,-1f,1f,1f)] public AnimationCurve horizontalShiftZ = new (new Keyframe(0, 0), new Keyframe(1, 0));
    
    [Header("Arc Dynamics")]
    [Header("Loop times")]
    public float loopDurationX = 30f;
    public float loopDurationY = 30f;
    public float loopDurationZ = 30f;
    
    [Header("Perlin noise cylinder parameters")]
    [Header("X")]
    public float radiusX = 400f;
    public float heightX = 4f;
    public float verticalShiftX = 0f;
    [Header("Y")]
    public float radiusY = 400f;
    public float heightY = 4f;
    public float verticalShiftY = 100f;
    [Header("Z")]
    public float radiusZ = 400f;
    public float heightZ = 4f;
    public float verticalShiftZ = 200f;
    
    [Header("Dynamic multipliers")]
    [Tooltip("Does not affect static displacements")]
    [Header("X")]
    public bool useSinusMultiplierX = true;
    [EnableIf("useSinusMultiplierX")] public float sinusMultiplierPowerX = 0.5f;
    [EnableIf("!useSinusMultiplierX"), CurveRange(0f, 0f, 1f, 1f)] public AnimationCurve multiplierCurveX =
        new (new Keyframe(0, 0), new Keyframe(1f,0));
    [Header("Y")]
    public bool useSinusMultiplierY = true;
    [EnableIf("useSinusMultiplierY")] public float sinusMultiplierPowerY = 0.5f;
    [EnableIf("!useSinusMultiplierY"), CurveRange(0f, 0f, 1f, 1f)] public AnimationCurve multiplierCurveY =
        new (new Keyframe(0, 0), new Keyframe(1f,0));
    [Header("Z")]    
    public bool useSinusMultiplierZ = false;
    [EnableIf("useSinusMultiplierZ")] public float sinusMultiplierPowerZ = 0.5f;
    [EnableIf("!useSinusMultiplierZ"), CurveRange(0f, 0f, 1f, 1f)] public AnimationCurve multiplierCurveZ =
        new (new Keyframe(0, 0), new Keyframe(1f,0));
    
    [Header("Particles Integration")]
    [Tooltip("Emission and Shape must be disabled, Simulation Space = World")]
    public ParticleSystem arcParticles;
    public float particlesPerSecondPerMeter = 50f;
    
    [Header("Texture Generation (Cylinder)")]
    [Range(16, 512)] public int textureResolution = 256;

    private Renderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Texture2D generatedTex;
    private Color[] _pixelCache;
    private int _seed = 37;
    
    private float _particlesToEmit = 0f;
    private bool _initialized = false;
    
    private static readonly int StartPosID = Shader.PropertyToID("_StartPos");
    private static readonly int DirAndInvLenID = Shader.PropertyToID("_DirAndInvLen");
    private static readonly int UID = Shader.PropertyToID("_U");
    private static readonly int VID = Shader.PropertyToID("_V");
    private static readonly int ZID = Shader.PropertyToID("_Z");
    private static readonly int TimeXYZID = Shader.PropertyToID("_TimeXYZ");
    private static readonly int AmplitudesID = Shader.PropertyToID("_Amplitudes");
    private static readonly int ArcDataTexID = Shader.PropertyToID("_ArcDataTex");
    
    private void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        _seed = Random.Range(int.MinValue, int.MaxValue);
        _initialized = false;
        sinusMultiplierPowerX = Mathf.Max(0f, sinusMultiplierPowerX);
        sinusMultiplierPowerY = Mathf.Max(0f, sinusMultiplierPowerY);
        sinusMultiplierPowerZ = Mathf.Max(0f, sinusMultiplierPowerZ);
    }

    private void OnValidate()
    {
        meshRenderer = GetComponent<Renderer>();
        _initialized = false;
    }

    private void Update()
    {
        if (startPoint && endPoint && meshRenderer)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            
            meshRenderer.GetPropertyBlock(propertyBlock);
            
            var start = startPoint.position;
            var end = endPoint.position;
            var dir = end - start;
            var distance = dir.magnitude;
            var lenSq = dir.sqrMagnitude;
            
            if (distance < 0.001f) return;

            if (!_initialized)
            {
                _initialized = true;
                GenerateCylinderNoiseTexture(distance);
                propertyBlock.SetTexture(ArcDataTexID, generatedTex);
            }

            var L = dir / distance;
            var dotUp = Vector3.Dot(L, transform.up);
            var refUp = Mathf.Abs(dotUp) > 0.99f ? transform.forward : transform.up;
            var U = Vector3.Normalize(Vector3.Cross(refUp, L));
            var V = Vector3.Cross(L, U);
            var Z = Vector3.Cross(U, V).normalized;

            var time = Application.isPlaying ? Time.timeSinceLevelLoad : Time.realtimeSinceStartup; 
            var t_x = Mathf.Repeat(time / Mathf.Max(loopDurationX, 0.001f), 1f);
            var t_y = Mathf.Repeat(time / Mathf.Max(loopDurationY, 0.001f), 1f);
            var t_z = Mathf.Repeat(time / Mathf.Max(loopDurationZ, 0.001f), 1f);
            
            var invLenSq = 1f / lenSq;

            propertyBlock.SetVector(StartPosID, start);
            propertyBlock.SetVector(DirAndInvLenID, new Vector4(dir.x, dir.y, dir.z, invLenSq));
            propertyBlock.SetVector(UID, U);
            propertyBlock.SetVector(VID, V);
            propertyBlock.SetVector(ZID, Z);
            propertyBlock.SetVector(TimeXYZID, new Vector4(t_x, t_y, t_z, 0));
            
            var scaledAmplitudeX = scaleX ? distance * amplitudeX : amplitudeX;
            var scaledAmplitudeY = scaleY ? distance * amplitudeY : amplitudeY;
            var scaledAmplitudeZ = scaleZ ? distance * amplitudeZ : amplitudeZ;
            propertyBlock.SetVector(AmplitudesID, new Vector4(scaledAmplitudeX, scaledAmplitudeY, scaledAmplitudeZ, 0));

            meshRenderer.SetPropertyBlock(propertyBlock);

            if (Application.isPlaying && arcParticles && _pixelCache != null)
            {
                EmitParticles(start, end, dir, distance, U, V, Z, t_x, t_y, t_z, scaledAmplitudeX, scaledAmplitudeY, scaledAmplitudeZ);
            }
            
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }

    private void EmitParticles(Vector3 start, Vector3 end, Vector3 dir, float distance, Vector3 U, Vector3 V, Vector3 Z, float t_x, float t_y, float t_z, float scaledAmplitudeX, float scaledAmplitudeY, float scaledAmplitudeZ)
    {
        float currentParticlesPerSec = particlesPerSecondPerMeter * distance;
        _particlesToEmit += Time.deltaTime * currentParticlesPerSec;

        if (_particlesToEmit < 1f) return;

        var emitParams = new ParticleSystem.EmitParams();
        var startSpeedCurve = arcParticles.main.startSpeed;
        bool isSpeedRandom = startSpeedCurve.mode == ParticleSystemCurveMode.TwoConstants;
        Vector3 L = dir / distance;

        while (_particlesToEmit >= 1f)
        {
            _particlesToEmit -= 1f;
            float h = Random.value; 
            
            
            float k = GetCachedBilinear(t_x, h, 0); 
            float g = GetCachedBilinear(t_y, h, 1);
            float b = GetCachedBilinear(t_z, h, 2);
            
            float offsetX = (k - 0.5f) * scaledAmplitudeX;
            float offsetY = (g - 0.5f) * scaledAmplitudeY;
            float offsetZ = (b - 0.5f) * scaledAmplitudeZ;

            Vector3 basePos = Vector3.LerpUnclamped(start, end, h);
            emitParams.position = basePos + (U * offsetX + V * offsetY + Z * offsetZ);
            
            float randomAngle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 outwardDir = (U * Mathf.Cos(randomAngle) + V * Mathf.Sin(randomAngle)).normalized;
            
            float speed = isSpeedRandom 
                ? Random.Range(startSpeedCurve.constantMin, startSpeedCurve.constantMax) 
                : startSpeedCurve.constant;
            
            emitParams.velocity = outwardDir * speed;
            emitParams.rotation3D = Quaternion.LookRotation(outwardDir, L).eulerAngles;
            
            arcParticles.Emit(emitParams, 1);
        }
    }

    private float GetCachedBilinear(float u, float v, int channel)
    {
        u = Mathf.Repeat(u, 1f);
        v = Mathf.Clamp01(v);

        float x = u * (textureResolution - 1);
        float y = v * (textureResolution - 1);

        int x0 = (int)x;
        int x1 = (x0 + 1) % textureResolution; 
        int y0 = (int)y;
        int y1 = Mathf.Min(y0 + 1, textureResolution - 1); 

        float tx = x - x0;
        float ty = y - y0;

        float c00 = GetChannel(_pixelCache[y0 * textureResolution + x0], channel);
        float c10 = GetChannel(_pixelCache[y0 * textureResolution + x1], channel);
        float c01 = GetChannel(_pixelCache[y1 * textureResolution + x0], channel);
        float c11 = GetChannel(_pixelCache[y1 * textureResolution + x1], channel);

        float cx0 = Mathf.Lerp(c00, c10, tx);
        float cx1 = Mathf.Lerp(c01, c11, tx);

        return Mathf.Lerp(cx0, cx1, ty);
    }

    private float GetChannel(Color c, int channel) => channel == 0 ? c.r : (channel == 1 ? c.g : c.b);

    [ContextMenu("Force Regenerate Texture")]
    private void GenerateCylinderNoiseTexture(float distance)
    {
        InitSimplexNoise();

        if (generatedTex == null || generatedTex.width != textureResolution)
        {
            generatedTex = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBAFloat, false, true)
            {
                name = "ArcDataTexture",
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        var resR = GetOneDimensionValues(radiusX, distance * heightX, verticalShiftX, horizontalShiftX, useSinusMultiplierX, sinusMultiplierPowerX, multiplierCurveX);
        var resG = GetOneDimensionValues(radiusY, distance * heightY, verticalShiftY, horizontalShiftY, useSinusMultiplierY, sinusMultiplierPowerY, multiplierCurveY);
        var resB = GetOneDimensionValues(radiusZ, distance * heightZ, verticalShiftZ, horizontalShiftZ, useSinusMultiplierZ, sinusMultiplierPowerZ, multiplierCurveZ);
        
        int totalPixels = textureResolution * textureResolution;
        _pixelCache = new Color[totalPixels]; // Обновляем кэш

        for (var i = 0; i < totalPixels; i++)
        {
            _pixelCache[i] = new Color(resR[i], resG[i], resB[i], 1f);
        }
        
        generatedTex.SetPixels(_pixelCache);
        generatedTex.Apply(false, true); // Загружаем на GPU и очищаем память текстуры (она нам больше не нужна, так как есть _pixelCache)
    }

    private float[] GetOneDimensionValues(float radius, float height, float verticalShift, AnimationCurve horizontalShift, bool useSinusMultiplier, float sinusMultiplierPower, AnimationCurve multiplierCurve)
    { 
        int totalPixels = textureResolution * textureResolution;
        float[] raw = new float[totalPixels];
        float sum = 0f;

        for (int v = 0; v < textureResolution; v++)
        {
            float h = (float)v / (textureResolution - 1);
            for (int u = 0; u < textureResolution; u++)
            {
                float t = (float)u / (textureResolution - 1);
                float angle = t * 2f * Mathf.PI;

                float px = radius * Mathf.Cos(angle) + verticalShift;
                float py = radius * Mathf.Sin(angle) + verticalShift;
                float pz = height * h + verticalShift;

                float val = Simplex3D(px, py, pz);

                int index = v * textureResolution + u;
                raw[index] = val;
                sum += val;
            }
        }

        float avg = sum / totalPixels;
        float maxAbs = 0.0001f;

        for (int v = 0; v < textureResolution; v++)
        {
            var h = (float)v / (textureResolution - 1);
            var sh = Mathf.Clamp(horizontalShift.Evaluate(h),-1f,1f);
            for (int u = 0; u < textureResolution; u++)
            {
                int i = v * textureResolution + u;
                raw[i] -= avg;
                if (Mathf.Abs(raw[i] + sh) > maxAbs) maxAbs = Mathf.Abs(raw[i]);
            }
        }

        var res = new float[totalPixels];
        for (int v = 0; v < textureResolution; v++)
        {
            var h = (float)v / (textureResolution - 1);
            var mask = useSinusMultiplier
                ? Mathf.Pow(Mathf.Max(0f, Mathf.Sin(h * Mathf.PI)), sinusMultiplierPower)
                : Mathf.Clamp01(multiplierCurve.Evaluate(h));
            var sh = Mathf.Clamp(horizontalShift.Evaluate(h),-1f,1f)/maxAbs;
            for (int u = 0; u < textureResolution; u++)
            {
                int i = v * textureResolution + u;
                var disp = (raw[i] / maxAbs) * mask + sh;
                res[i] = disp * 0.5f + 0.5f;
            }
        }

        return res;
    }

    #region Fast Simplex Noise 3D
    private int[] perm;
    private void InitSimplexNoise()
    {
        if (perm != null) return;
        perm = new int[512];
        System.Random rnd = new System.Random(_seed);
        for (int i = 0; i < 256; i++) perm[i] = i;
        for (int i = 0; i < 256; i++) { int j = rnd.Next(256); (perm[i], perm[j]) = (perm[j], perm[i]); }
        for (int i = 0; i < 256; i++) perm[256 + i] = perm[i];
    }

    private static int FastFloor(float x) => x > 0 ? (int)x : (int)x - 1;
    private static float Dot(int[] g, float x, float y, float z) => g[0]*x + g[1]*y + g[2]*z;

    private static readonly int[][] grad3 = {
        new[]{1,1,0}, new[]{-1,1,0}, new[]{1,-1,0}, new[]{-1,-1,0},
        new[]{1,0,1}, new[]{-1,0,1}, new[]{1,0,-1}, new[]{-1,0,-1},
        new[]{0,1,1}, new[]{0,-1,1}, new[]{0,1,-1}, new[]{0,-1,-1}
    };

    private float Simplex3D(float x, float y, float z)
    {
        float F3 = 1.0f / 3.0f;
        float G3 = 1.0f / 6.0f;
        float s = (x + y + z) * F3;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);
        int k = FastFloor(z + s);
        float t = (i + j + k) * G3;
        float X0 = i - t, Y0 = j - t, Z0 = k - t;
        float x0 = x - X0, y0 = y - Y0, z0 = z - Z0;

        int i1, j1, k1, i2, j2, k2;
        if (x0 >= y0) {
            if (y0 >= z0)      { i1=1; j1=0; k1=0; i2=1; j2=1; k2=0; }
            else if (x0 >= z0) { i1=1; j1=0; k1=0; i2=1; j2=0; k2=1; }
            else               { i1=0; j1=0; k1=1; i2=1; j2=0; k2=1; }
        } else {
            if (y0 < z0)       { i1=0; j1=0; k1=1; i2=0; j2=1; k2=1; }
            else if (x0 < z0)  { i1=0; j1=1; k1=0; i2=0; j2=1; k2=1; }
            else               { i1=0; j1=1; k1=0; i2=1; j2=1; k2=0; }
        }

        float x1 = x0 - i1 + G3, y1 = y0 - j1 + G3, z1 = z0 - k1 + G3;
        float x2 = x0 - i2 + 2.0f*G3, y2 = y0 - j2 + 2.0f*G3, z2 = z0 - k2 + 2.0f*G3;
        float x3 = x0 - 1.0f + 3.0f*G3, y3 = y0 - 1.0f + 3.0f*G3, z3 = z0 - 1.0f + 3.0f*G3;

        int ii = i & 255, jj = j & 255, kk = k & 255;
        float n0, n1, n2, n3;

        float t0 = 0.6f - x0*x0 - y0*y0 - z0*z0;
        if (t0 < 0) n0 = 0.0f;
        else { t0 *= t0; n0 = t0 * t0 * Dot(grad3[perm[ii + perm[jj + perm[kk]]] % 12], x0, y0, z0); }

        float t1 = 0.6f - x1*x1 - y1*y1 - z1*z1;
        if (t1 < 0) n1 = 0.0f;
        else { t1 *= t1; n1 = t1 * t1 * Dot(grad3[perm[ii + i1 + perm[jj + j1 + perm[kk + k1]]] % 12], x1, y1, z1); }

        float t2 = 0.6f - x2*x2 - y2*y2 - z2*z2;
        if (t2 < 0) n2 = 0.0f;
        else { t2 *= t2; n2 = t2 * t2 * Dot(grad3[perm[ii + i2 + perm[jj + j2 + perm[kk + k2]]] % 12], x2, y2, z2); }

        float t3 = 0.6f - x3*x3 - y3*y3 - z3*z3;
        if (t3 < 0) n3 = 0.0f;
        else { t3 *= t3; n3 = t3 * t3 * Dot(grad3[perm[ii + 1 + perm[jj + 1 + perm[kk + 1]]] % 12], x3, y3, z3); }

        return 32.0f * (n0 + n1 + n2 + n3);
    }
    #endregion
}