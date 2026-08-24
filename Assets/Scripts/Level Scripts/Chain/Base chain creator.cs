using UnityEngine;
using System.Collections.Generic;

public abstract class BaseChainCreator : MonoBehaviour
{
    [Header("Main settings")]
    [SerializeField] protected bool isLoop = false;

    [Header("Chain anchors")] 
    [SerializeField] private bool useMyTransformAsFirstAnchor = false;
    [SerializeField] private List<Transform> preAnchors = new ();
    [SerializeField] private List<Transform> postAnchors = new ();
    
    [Header("Segment piece")]
    [SerializeField] protected GameObject piecePrefab;
    [SerializeField] protected float pieceLength = 2.0f;
    [SerializeField] protected float angleToRotateNextPieceBy = 0f;
    [SerializeField] protected bool isPrefabPivotInCenter = false;

    protected const string RootName = "Chain Segments Generated";

    protected virtual void Start()
    {
        GenerateChain();
    }

    [ContextMenu("Generate Chain")]
    public virtual void GenerateChain()
    {
        var oldRoot = transform.Find(RootName);
        if (oldRoot)
        {
            if (Application.isPlaying) Destroy(oldRoot.gameObject);
            else DestroyImmediate(oldRoot.gameObject);
        }

        var root = UtilityFunctions.MakeEmptyObject(RootName, transform);

        var points = GetPoints(root.transform);
        if (points.Count < 2) return;

        PreGenerate(points, root.transform);

        int segmentCount = isLoop ? points.Count : points.Count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            var pointA = points[i];
            var pointB = points[(i + 1) % points.Count];

            BuildSegment(pointA, pointB, i, segmentCount, points, root.transform);
        }

        PostGenerate(points, root.transform);
    }

    protected List<Transform> GetPoints(Transform rootTransform)
    {
        var points = new List<Transform>();
        
        if (useMyTransformAsFirstAnchor)
        {
            points.Add(transform);
        }

        foreach (var anchor in preAnchors)
        {
            if(anchor!=null) points.Add(anchor);
        }
        foreach (Transform child in transform)
        {
            if (child != rootTransform) points.Add(child);
        }
        foreach (var anchor in postAnchors)
        {
            if(anchor!=null) points.Add(anchor);
        }
        return points;
    }

    protected virtual void PreGenerate(List<Transform> points, Transform root) { }
    protected virtual void PostGenerate(List<Transform> points, Transform root) { }

    protected abstract void BuildSegment(
        Transform pointA, Transform pointB, int index, int totalSegments, 
        List<Transform> points, Transform parent);

    protected Vector3 GetIncomingDirection(List<Transform> points, int index)
    {
        if (index == 0 && !isLoop) 
            return (points[1].position - points[0].position).normalized;
        
        int prevIndex = index - 1 < 0 ? points.Count - 1 : index - 1;
        return (points[index].position - points[prevIndex].position).normalized;
    }

    protected Vector3 GetOutgoingDirection(List<Transform> points, int index)
    {
        if (index == points.Count - 1 && !isLoop) 
            return (points[index].position - points[index - 1].position).normalized;
        
        int nextIndex = (index + 1) % points.Count;
        return (points[nextIndex].position - points[index].position).normalized;
    }

    protected static void DestroyObject(GameObject obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    protected GameObject CombinePieces(GameObject targetRoot, List<GameObject> pieces, out Material sharedMat)
    {
        sharedMat = null;
        var combiners = new List<CombineInstance>();

        foreach (var piece in pieces)
        {
            foreach (var mf in piece.GetComponentsInChildren<MeshFilter>())
            {
                if (!sharedMat) sharedMat = mf.GetComponent<Renderer>().sharedMaterial;
                var ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = targetRoot.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix
                };
                combiners.Add(ci);
            }
            
            foreach (var smr in piece.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!sharedMat) sharedMat = smr.sharedMaterial;
                var ci = new CombineInstance
                {
                    mesh = smr.sharedMesh,
                    transform = targetRoot.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix
                };
                combiners.Add(ci);
            }

            DestroyObject(piece);
        }

        if (combiners.Count > 0)
        {
            var finalMf = targetRoot.AddComponent<MeshFilter>();
            var finalMr = targetRoot.AddComponent<MeshRenderer>();
            
            var combinedMesh = new Mesh
            {
                name = targetRoot.name + "_Mesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            combinedMesh.CombineMeshes(combiners.ToArray(), true, true);

            finalMf.sharedMesh = combinedMesh;
            finalMr.sharedMaterial = sharedMat;
        }

        return targetRoot;
    }
}