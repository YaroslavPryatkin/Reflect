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
        mesh.name = "CaplessCylinder";

        int verticesCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[verticesCount];
        Vector2[] uvs = new Vector2[verticesCount];
        int[] triangles = new int[segments * 6];

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