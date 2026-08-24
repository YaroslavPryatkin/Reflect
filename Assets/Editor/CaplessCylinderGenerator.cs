using UnityEngine;
using UnityEditor;

public class CaplessCylinderGenerator : EditorWindow
{
    public enum RadiusType
    {
        ToVertices,
        ToEdges    
    }

    private float radius = 1f;
    private float height = 1f;
    private int segments = 32;
    private RadiusType radiusType = RadiusType.ToVertices;
    private bool generateCaps = false; 
    private string savePath = "Assets/CaplessCylinder.asset";

    [MenuItem("Tools/Generate Capless Cylinder (Tube)")]
    public static void ShowWindow()
    {
        GetWindow<CaplessCylinderGenerator>("Cylinder Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cylinder parameters", EditorStyles.boldLabel);

        radius = EditorGUILayout.FloatField("Radius", radius);
        radiusType = (RadiusType)EditorGUILayout.EnumPopup("Radius type", radiusType);
        height = EditorGUILayout.FloatField("height", height);
        segments = EditorGUILayout.IntSlider("Segments", segments, 3, 256);
        generateCaps = EditorGUILayout.Toggle("Generate caps", generateCaps); // Переключатель в GUI

        EditorGUILayout.Space();
        savePath = EditorGUILayout.TextField("Save path", savePath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate mesh", GUILayout.Height(30)))
        {
            GenerateMesh();
        }
    }

    private void GenerateMesh()
    {
        float angleStep = (Mathf.PI * 2f) / segments;
        float actualRadius = radius;

        if (radiusType == RadiusType.ToEdges)
        {
            actualRadius = radius / Mathf.Cos(angleStep / 2f);
        }

        Mesh mesh = new Mesh();
        mesh.name = generateCaps ? "CylinderWithCaps" : "CaplessCylinder";
        
        int sideVerticesCount = (segments + 1) * 2;
        int capsVerticesCount = generateCaps ? (segments + 2) * 2 : 0; 
        int totalVerticesCount = sideVerticesCount + capsVerticesCount;

        int sideTrianglesCount = segments * 6;
        int capsTrianglesCount = generateCaps ? segments * 6 : 0;
        int totalTrianglesCount = sideTrianglesCount + capsTrianglesCount;

        Vector3[] vertices = new Vector3[totalVerticesCount];
        Vector2[] uvs = new Vector2[totalVerticesCount];
        int[] triangles = new int[totalTrianglesCount];

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Sin(angle) * actualRadius;
            float z = Mathf.Cos(angle) * actualRadius;

            float u = (float)i / segments;

            vertices[i] = new Vector3(x, -height / 2f, z);
            uvs[i] = new Vector2(u, 0f);

            vertices[i + segments + 1] = new Vector3(x, height / 2f, z);
            uvs[i + segments + 1] = new Vector2(u, 1f);
        }

        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            int v0 = i;
            int v1 = i + segments + 1;
            int v2 = i + segments + 2;
            int v3 = i + 1;

            triangles[t++] = v0;
            triangles[t++] = v2;
            triangles[t++] = v1;

            triangles[t++] = v0;
            triangles[t++] = v3;
            triangles[t++] = v2;
        }

        if (generateCaps)
        {
            int capOffset = sideVerticesCount;

            int topCenterIndex = capOffset;
            vertices[topCenterIndex] = new Vector3(0f, height / 2f, 0f);
            uvs[topCenterIndex] = new Vector2(0.5f, 0.5f);

            int topRingStart = topCenterIndex + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep;
                float x = Mathf.Sin(angle) * actualRadius;
                float z = Mathf.Cos(angle) * actualRadius;

                vertices[topRingStart + i] = new Vector3(x, height / 2f, z);
                uvs[topRingStart + i] = new Vector2(0.5f + Mathf.Sin(angle) * 0.5f, 0.5f + Mathf.Cos(angle) * 0.5f);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[t++] = topCenterIndex;
                triangles[t++] = topRingStart + i;
                triangles[t++] = topRingStart + i + 1;
            }

            int bottomCenterIndex = topRingStart + segments + 1;
            vertices[bottomCenterIndex] = new Vector3(0f, -height / 2f, 0f);
            uvs[bottomCenterIndex] = new Vector2(0.5f, 0.5f);

            int bottomRingStart = bottomCenterIndex + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep;
                float x = Mathf.Sin(angle) * actualRadius;
                float z = Mathf.Cos(angle) * actualRadius;

                vertices[bottomRingStart + i] = new Vector3(x, -height / 2f, z);
                uvs[bottomRingStart + i] = new Vector2(0.5f + Mathf.Sin(angle) * 0.5f, 0.5f - Mathf.Cos(angle) * 0.5f);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[t++] = bottomCenterIndex;
                triangles[t++] = bottomRingStart + i + 1;
                triangles[t++] = bottomRingStart + i;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        AssetDatabase.CreateAsset(mesh, savePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>Success!</color> Mesh is saved at: {savePath}");
    }
}