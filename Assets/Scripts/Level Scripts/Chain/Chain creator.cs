using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;

public class ChainCreator : BaseChainCreator
{
    [Header("Collision logic")]
    [SerializeField] private bool addCollisionLogic = false;
    [SerializeField, EnableIf("addCollisionLogic")] private bool makeTrigger = true;
    [SerializeField, EnableIf("addCollisionLogic")] private bool addChainSegmentController = true;
    [SerializeField, EnableIf("addCollisionLogic")] private float colliderRadius = 0.1f;
    [SerializeField, EnableIf("addCollisionLogic")] private LayerMask colliderLayer;
    
    private int _realLayer = 31;

    [Header("Segment joint settings")]
    [SerializeField] private bool cutByBisector = false;
    [SerializeField, EnableIf("cutByBisector")] private float segmentExtension = 0.3f;

    [Header("Caps")]
    [SerializeField] private GameObject endCapPrefab; 
    [SerializeField] private GameObject jointPrefab;
    [SerializeField] private bool placeVertically = false;
    [SerializeField] private bool placeOneJoint = true;
    [SerializeField] private float sideShift = 0f;

    private ChainSegmentController _previousSegmentController;
    private ChainSegmentController _firstSegmentController;
    private bool _wasSegmentController = false;

    private struct ClipPlanes
    {
        public Vector3 PosStart;
        public Vector3 NormStart;
        public Vector3 PosEnd;
        public Vector3 NormEnd;
    }

    protected override void Start()
    {
        if (addCollisionLogic)
        {
            while (_realLayer > 0 && ((1 << _realLayer) & (int)colliderLayer) == 0)
                _realLayer--;
        }

        base.Start();
    }

    protected override void PreGenerate(List<Transform> points, Transform root)
    {
        _wasSegmentController = false;
        _previousSegmentController = null;
        _firstSegmentController = null;
    }

    protected override void BuildSegment(
        Transform pointA, Transform pointB, int index, int totalSegments, List<Transform> points, Transform parent)
    {
        Vector3 posA = pointA.position;
        Vector3 posB = pointB.position;
        Vector3 segmentDir = (posB - posA).normalized;
        float distance = Vector3.Distance(posA, posB);

        var segmentRoot = new GameObject($"Segment_{index}");
        segmentRoot.transform.SetParent(parent);
        segmentRoot.transform.position = (posA + posB) / 2f;
        segmentRoot.transform.rotation = Quaternion.LookRotation(segmentDir);

        if (addCollisionLogic)
        {
            segmentRoot.layer = _realLayer;
            
            var col = segmentRoot.AddComponent<CapsuleCollider>();
            col.direction = 2; // Z-axis
            col.radius = colliderRadius;
            col.height = distance + (colliderRadius * 2f);
            col.isTrigger = makeTrigger;

            if (addChainSegmentController)
            {
                var controller = segmentRoot.AddComponent<ChainSegmentController>();

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
            }
        }

        if (piecePrefab == null) return;

        ClipPlanes planes = CalculateClipPlanes(pointA, pointB, index, totalSegments, points, segmentDir);

        float extraOffset = cutByBisector ? segmentExtension : 0f;
        float totalLength = distance + (extraOffset * 2f);

        int piecesCount = Mathf.CeilToInt(totalLength / pieceLength);
        var tempPieces = new List<GameObject>();
        Vector3 upVector = Vector3.up;

        for (int j = 0; j < piecesCount; j++)
        {
            float offsetZ = (j * pieceLength) - extraOffset;
            if (isPrefabPivotInCenter) offsetZ += pieceLength / 2f;

            Vector3 spawnPos = posA + segmentDir * offsetZ;
            GameObject piece = Instantiate(piecePrefab, spawnPos, Quaternion.LookRotation(segmentDir, upVector), segmentRoot.transform);
            tempPieces.Add(piece);

            upVector = Quaternion.AngleAxis(angleToRotateNextPieceBy, segmentDir) * upVector;
        }

        BakePlanesToPieces(segmentRoot, tempPieces, planes, true, true, true);
    }

    protected override void PostGenerate(List<Transform> points, Transform root)
    {
        if (addCollisionLogic && addChainSegmentController && isLoop && _wasSegmentController)
        {
            _previousSegmentController.SetForward(_firstSegmentController);
            _firstSegmentController.SetBackward(_previousSegmentController);
        }

        PlaceCapsAndJoints(points, root);
    }

    private ClipPlanes CalculateClipPlanes(Transform pointA, Transform pointB, int i, int segmentCount, List<Transform> points, Vector3 segmentDir)
    {
        var planes = new ClipPlanes();

        if (cutByBisector)
        {
            var dirInA = GetIncomingDirection(points, i);
            var bisectorA = (dirInA + segmentDir).normalized;
            if (bisectorA.sqrMagnitude < 0.001f) bisectorA = segmentDir;

            int nextIndex = (i + 1) % points.Count;
            var dirOutB = GetOutgoingDirection(points, nextIndex);
            var bisectorB = (segmentDir + dirOutB).normalized;
            if (bisectorB.sqrMagnitude < 0.001f) bisectorB = segmentDir;

            planes.PosStart = pointA.position;
            planes.NormStart = (!isLoop && i == 0) ? segmentDir : bisectorA;

            planes.PosEnd = pointB.position;
            planes.NormEnd = (!isLoop && i == segmentCount - 1) ? -segmentDir : -bisectorB;
        }
        else
        {
            planes.PosStart = pointA.position;
            planes.NormStart = segmentDir;
            planes.PosEnd = pointB.position;
            planes.NormEnd = -segmentDir;
        }

        return planes;
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

        var planes = new ClipPlanes
        {
            PosStart = pos,
            NormStart = clipNormal
        };

        BakePlanesToPieces(joint, new List<GameObject> { joint }, planes, false, true);
    }

    private void BakePlanesToPieces(GameObject targetRoot, List<GameObject> pieces, ClipPlanes planes, bool combine = false, bool usePlaneStart = false, bool usePlaneEnd = false)
    {
        var pStart = new Vector4(planes.NormStart.x, planes.NormStart.y, planes.NormStart.z, -Vector3.Dot(planes.NormStart, planes.PosStart));
        var pEnd = new Vector4(planes.NormEnd.x, planes.NormEnd.y, planes.NormEnd.z, -Vector3.Dot(planes.NormEnd, planes.PosEnd));

        var mpb = new MaterialPropertyBlock();
        mpb.SetFloat("_UseUV1", usePlaneStart ? 1f : 0f);
        mpb.SetFloat("_UseUV2", usePlaneEnd ? 1f : 0f);

        if (combine)
        {
            CombinePieces(targetRoot, pieces, out Material sharedMat);
            var mr = targetRoot.GetComponent<MeshRenderer>();
            var mf = targetRoot.GetComponent<MeshFilter>();
            if (mf && mr && mf.sharedMesh != null)
            {
                ApplyUVs(mf.sharedMesh, pStart, pEnd);
                mr.SetPropertyBlock(mpb);
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
                    ApplyUVs(uniqueMesh, pStart, pEnd);
                    mf.sharedMesh = uniqueMesh;
                    mr.SetPropertyBlock(mpb);
                }

                foreach (var smr in piece.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var uniqueMesh = Instantiate(smr.sharedMesh);
                    ApplyUVs(uniqueMesh, pStart, pEnd);
                    smr.sharedMesh = uniqueMesh;
                    smr.SetPropertyBlock(mpb);
                }
            }
        }
    }

    private void ApplyUVs(Mesh uniqueMesh, Vector4 pStart, Vector4 pEnd)
    {
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
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        var points = GetPoints(transform);
        if (points.Count < 2) return;

        Gizmos.color = addCollisionLogic ? Color.green : Color.red;
        float radius = addCollisionLogic ? colliderRadius * 2 : 0.1f;
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
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(radius, radius, dist));
            }
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}