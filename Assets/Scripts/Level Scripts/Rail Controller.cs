using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;

public class RailController : MonoBehaviour
{
    [Header("Main settings")]
    [SerializeField] private bool isLoop = false;
    [SerializeField] private float colliderRadius = 0.5f;
    [SerializeField] private LayerMask colliderLayer;

    [Header("Rail piece")]
    [SerializeField] private GameObject railPiecePrefab;
    [SerializeField] private float railPieceLength = 2.0f; 
    [SerializeField] private bool isPrefabPivotInCenter = false; 
    
    [Header("Caps")]
    [SerializeField] private GameObject endCapPrefab; 
    [SerializeField] private GameObject jointPrefab;
    [SerializeField] private bool placeVertically = false;
    [SerializeField] private bool placeOneJoint = true;
    [SerializeField] private float sideShift = 0f;

    private int realLayer=31;
    void Start()
    {
        while (realLayer > 0 && ((1<<realLayer) & (int)colliderLayer)==0)
            realLayer--;
        
        GenerateMonorail();
    }
    
    
    public void GenerateMonorail()
    {
        
        Transform oldRoot = transform.Find("Monorail_Generated");
        if (oldRoot) Destroy(oldRoot.gameObject);

        GameObject root = new GameObject("Monorail_Generated");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;

        List<Transform> points = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child != root.transform) points.Add(child);
        }

        if (points.Count < 2) return;

        int segmentCount = isLoop ? points.Count : points.Count - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            Transform pointA = points[i];
            Transform pointB = points[(i + 1) % points.Count];

            var segmentDir = (pointB.position - pointA.position).normalized;
            ClipPlanes planes;
            planes.posStart = pointA.position;
            planes.normStart = segmentDir; 
            planes.posEnd = pointB.position;
            planes.normEnd = -segmentDir;

            BuildSegment(pointA.position, pointB.position, planes, i, root.transform);
        }

        if (isLoop && _wasSegmentController)
        {
            _previousSegmentController.SetForward(_firstSegmentController);
            _firstSegmentController.SetBackward(_previousSegmentController);
        }

        PlaceCapsAndJoints(points, root.transform);
    }
    
    private struct ClipPlanes
    {
        public Vector3 posStart;
        public Vector3 normStart;
        public Vector3 posEnd;
        public Vector3 normEnd;
    }

    private RailSegmentController _previousSegmentController;
    private RailSegmentController _firstSegmentController;
    private bool _wasSegmentController = false;

    private void BuildSegment(Vector3 posA, Vector3 posB, ClipPlanes planes, int index, Transform parent)
    {
        Vector3 segmentDir = (posB - posA).normalized;
        float distance = Vector3.Distance(posA, posB);

        var segmentRoot = new GameObject($"Segment_{index}");
        segmentRoot.transform.SetParent(parent);
        segmentRoot.transform.position = (posA + posB) / 2f;
        segmentRoot.transform.rotation = Quaternion.LookRotation(segmentDir);
        segmentRoot.layer = realLayer;

        var col = segmentRoot.AddComponent<CapsuleCollider>();
        col.direction = 2; 
        col.radius = colliderRadius;
        col.height = distance + (colliderRadius * 2f); 
        col.isTrigger = true;

        var controller = segmentRoot.AddComponent<RailSegmentController>();
        
        if (_wasSegmentController)
        {
            _previousSegmentController.SetForward(controller);
            controller.SetBackward(_previousSegmentController);
        }
        else
        {
            _firstSegmentController = controller;
            _wasSegmentController = true;
        }
        _previousSegmentController = controller;

        if (railPiecePrefab == null) return;
        int piecesCount = Mathf.CeilToInt(distance / railPieceLength);
        List<GameObject> tempPieces = new List<GameObject>();

        for (int j = 0; j < piecesCount; j++)
        {
            float offsetZ = j * railPieceLength;
            if (isPrefabPivotInCenter) offsetZ += railPieceLength / 2f;

            Vector3 spawnPos = posA + segmentDir * offsetZ;
            GameObject piece = Instantiate(railPiecePrefab, spawnPos, Quaternion.LookRotation(segmentDir), segmentRoot.transform);
            tempPieces.Add(piece);
        }

        // if (combineMeshes)
        // {
        //     BakePlanesAndCombine(segmentRoot, tempPieces, normalA, posA, normalB, posB);
        // }
        // else
        // {
        //     foreach (var p in tempPieces) BakePlanesAndCombine(p, new List<GameObject>{p}, normalA, posA, normalB, posB, false);
        // }
        BakePlanesToPieces(segmentRoot, tempPieces, planes, true, true, true);
    }

    private void PlaceCapsAndJoints(List<Transform> points, Transform parent)
    {
        for (int i = 0; i < points.Count; i++)
        {
            bool isFirst = (i == 0);
            bool isLast = (i == points.Count - 1);

            if (!isLoop && (isFirst || isLast))
            {
                if (endCapPrefab != null)
                {
                    var dir = isFirst ? -GetOutgoingDirection(points, i) : GetIncomingDirection(points, i);
                    GameObject cap = Instantiate(endCapPrefab, points[i].position, Quaternion.LookRotation(dir), parent);
                    
                    BakePlanesToPieces(cap, new List<GameObject> { cap }, new ClipPlanes());
                    
                }
            }
            else if (jointPrefab != null)
            {
                var dirIn = GetIncomingDirection(points, i);
                var dirOut = GetOutgoingDirection(points, i);
                
                var bisector = (dirIn + dirOut).normalized;
                if (bisector.sqrMagnitude < 0.01f) bisector = dirOut;
                var bis2 = (dirOut - dirIn) / 2;
                var pos = points[i].position + bis2 * sideShift;
                
                if (placeOneJoint)
                {
                    var joint = Instantiate(jointPrefab, pos, Quaternion.LookRotation(bisector), parent);
                    BakePlanesToPieces(joint, new List<GameObject> { joint }, new ClipPlanes());
                }
                else
                {
                    SpawnClippedJoint(jointPrefab, pos, -dirIn, -bisector, parent);

                    SpawnClippedJoint(jointPrefab, pos, dirOut, bisector, parent);
                }
            }
        }
    }
    
    private void SpawnClippedJoint(GameObject prefab, Vector3 pos, Vector3 lookDir, Vector3 clipNormal, Transform parent)
    {
        if (placeVertically)
        {
            lookDir = new Vector3(lookDir.x, 0f, lookDir.z).normalized;
            clipNormal = new Vector3(clipNormal.x, 0f, clipNormal.z).normalized;
        }
        var joint = Instantiate(prefab, pos, Quaternion.LookRotation(lookDir), parent);
        
        var planes = new ClipPlanes();
        planes.posStart = pos;
        planes.normStart = clipNormal; 

        BakePlanesToPieces(joint, new List<GameObject> { joint }, planes, false, true);
    }
    
    // private void SpawnClippedJoint(GameObject prefab, Vector3 pos, Vector3 lookDir, Vector3 clipNormal, Transform parent)
    // {
    //     var joint = Instantiate(prefab, pos, Quaternion.LookRotation(lookDir), parent);
    //     var safeNormal = Vector3.up; 
    //     var safePos = pos + Vector3.up * 10000f;
    //
    //     BakePlanesAndCombine(joint, new List<GameObject>{joint}, clipNormal, pos, safeNormal, safePos, false);
    // }

    // private void BakePlanesAndCombine(GameObject targetObj, List<GameObject> pieces, Vector3 n1, Vector3 p1, Vector3 n2, Vector3 p2, bool combine = true)
    // {
    //     
    //     var plane1 = new Vector4(n1.x, n1.y, n1.z, -Vector3.Dot(n1, p1));
    //     var plane2 = new Vector4(n2.x, n2.y, n2.z, -Vector3.Dot(n2, p2));
    //
    //     var combiners = new List<CombineInstance>();
    //     Material mat = null;
    //
    //     foreach (var piece in pieces)
    //     {
    //         foreach (var mf in piece.GetComponentsInChildren<MeshFilter>())
    //         {
    //             if (mat == null) mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
    //
    //             var m = Instantiate(mf.sharedMesh);
    //             var uv2 = new List<Vector4>(m.vertexCount);
    //             var uv3 = new List<Vector4>(m.vertexCount);
    //
    //             for (int i = 0; i < m.vertexCount; i++)
    //             {
    //                 uv2.Add(plane1);
    //                 uv3.Add(plane2);
    //             }
    //
    //             m.SetUVs(1, uv2);
    //             m.SetUVs(2, uv3);
    //
    //             if (combine)
    //             {
    //                 CombineInstance ci = new CombineInstance();
    //                 ci.mesh = m;
    //                 ci.transform = targetObj.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
    //                 combiners.Add(ci);
    //             }
    //             else
    //             {
    //                 mf.sharedMesh = m;
    //             }
    //         }
    //         if (combine) Destroy(piece);
    //     }
    //
    //     if (combine && combiners.Count > 0)
    //     {
    //         var targetMf = targetObj.AddComponent<MeshFilter>();
    //         var targetMr = targetObj.AddComponent<MeshRenderer>();
    //         var combinedMesh = new Mesh();
    //         combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    //         combinedMesh.CombineMeshes(combiners.ToArray(), true, true);
    //         targetMf.sharedMesh = combinedMesh;
    //         targetMr.sharedMaterial = mat;
    //     }
    // }
    
    private void BakePlanesToPieces(GameObject targetRoot, List<GameObject> pieces, ClipPlanes planes, bool combine=false, bool usePlaneStart=false, bool usePlaneEnd=false)
    {
        Vector4 pStart = new Vector4(planes.normStart.x, planes.normStart.y, planes.normStart.z, -Vector3.Dot(planes.normStart, planes.posStart));
        Vector4 pEnd = new Vector4(planes.normEnd.x, planes.normEnd.y, planes.normEnd.z, -Vector3.Dot(planes.normEnd, planes.posEnd));

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetFloat("_UseUV1", usePlaneStart ? 1f : 0f);
        mpb.SetFloat("_UseUV2", usePlaneEnd ? 1f : 0f);

        Material sharedMat = null;

        if (combine)
        {
            var combiners = new List<CombineInstance>();
            foreach (var piece in pieces)
            {
                foreach (var mf in piece.GetComponentsInChildren<MeshFilter>())
                {
                    if (!sharedMat) sharedMat = mf.GetComponent<MeshRenderer>().sharedMaterial;
                    var ci = new CombineInstance();
                    ci.mesh = mf.sharedMesh; 
                    ci.transform = targetRoot.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                    combiners.Add(ci);
                }
                if (Application.isPlaying) Destroy(piece);
                else DestroyImmediate(piece);
            }

            if (combiners.Count > 0)
            {
                var finalMf = targetRoot.AddComponent<MeshFilter>();
                var finalMr = targetRoot.AddComponent<MeshRenderer>();
                
                var combinedMesh = new Mesh();
                combinedMesh.name = targetRoot.name + "_Mesh";
                combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                combinedMesh.CombineMeshes(combiners.ToArray(), true, true);
                
                int vCount = combinedMesh.vertexCount;
                List<Vector4> uv2 = new List<Vector4>(vCount);
                List<Vector4> uv3 = new List<Vector4>(vCount);
                for (int i = 0; i < vCount; i++)
                {
                    uv2.Add(pStart);
                    uv3.Add(pEnd);
                }
                combinedMesh.SetUVs(1, uv2);
                combinedMesh.SetUVs(2, uv3);

                finalMf.sharedMesh = combinedMesh;
                finalMr.sharedMaterial = sharedMat;
                finalMr.SetPropertyBlock(mpb);
            }
        }
        else
        {
            foreach (var piece in pieces)
            {
                foreach (var mf in piece.GetComponentsInChildren<MeshFilter>())
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    var uniqueMesh = Instantiate(mf.sharedMesh);
                    
                    int vCount = uniqueMesh.vertexCount;
                    List<Vector4> uv2 = new List<Vector4>(vCount);
                    List<Vector4> uv3 = new List<Vector4>(vCount);

                    for (int i = 0; i < vCount; i++)
                    {
                        uv2.Add(pStart);
                        uv3.Add(pEnd);
                    }
                    uniqueMesh.SetUVs(1, uv2);
                    uniqueMesh.SetUVs(2, uv3);

                    mf.sharedMesh = uniqueMesh;
                    mr.SetPropertyBlock(mpb);
                }
            }
        }
    }
    
    private Vector3 GetIncomingDirection(List<Transform> points, int index)
    {
        if (index == 0 && !isLoop) 
            return (points[1].position - points[0].position).normalized;
        
        int prevIndex = index - 1 < 0 ? points.Count - 1 : index - 1;
        return (points[index].position - points[prevIndex].position).normalized;
    }

    private Vector3 GetOutgoingDirection(List<Transform> points, int index)
    {
        if (index == points.Count - 1 && !isLoop) 
            return (points[index].position - points[index - 1].position).normalized;
        
        int nextIndex = (index + 1) % points.Count;
        return (points[nextIndex].position - points[index].position).normalized;
    }
    
    private void OnDrawGizmos()
    {
        List<Transform> points = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith("Monorail_Generated")) points.Add(child);
        }
        if (points.Count < 2) return;

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.7f);
        int count = isLoop ? points.Count : points.Count - 1;

        for (int i = 0; i < count; i++)
        {
            Vector3 a = points[i].position;
            Vector3 b = points[(i + 1) % points.Count].position;
            
            Vector3 center = (a + b) / 2f;
            Vector3 dir = (b - a).normalized;
            float dist = Vector3.Distance(a, b);

            if (dir != Vector3.zero)
            {
                
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(dir), Vector3.one);
                
                
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(colliderRadius * 2, colliderRadius * 2, dist));
            }
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}
