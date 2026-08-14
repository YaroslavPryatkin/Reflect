using UnityEngine;
using System.Collections.Generic;

public class ElectricArcChainCreator : BaseChainCreator
{
    [Header("Collider")] 
    [SerializeField] private Vector2 colliderSize = new(0.1f,0.1f);
    [SerializeField] private Vector2 colliderPlace = new(0,0);
    [SerializeField] private HazardType electricArcHazardType;
    protected override void BuildSegment(Transform pointA, Transform pointB, int index, int totalSegments, List<Transform> points, Transform parent)
    {
        if (piecePrefab == null) return;

        Vector3 posA = pointA.position;
        Vector3 posB = pointB.position;
        Vector3 segmentDir = (posB - posA).normalized;
        float distance = Vector3.Distance(posA, posB);

        var segmentRoot = new GameObject($"Arc_Segment_{index}");
        segmentRoot.transform.SetParent(parent);
        segmentRoot.transform.position = (posA + posB) / 2f;
        
        segmentRoot.transform.rotation = Quaternion.LookRotation(segmentDir, GetRootUp(segmentDir));

        var col = segmentRoot.AddComponent<BoxCollider>();
        col.center = colliderPlace;
        Vector3 size = colliderSize;
        size.z = distance;
        col.size = size;
        col.isTrigger = true;
        
        var hazard = segmentRoot.AddComponent<HazardCollider>();
        hazard.hazardType = electricArcHazardType;
        
        int piecesCount = Mathf.Max(1, Mathf.RoundToInt(distance / pieceLength));
        float scaledPieceLength = distance / piecesCount;
        float scaleZ = scaledPieceLength / pieceLength;

        
        var tempPieces = new List<GameObject>();
        Vector3 upVector = Vector3.up;

        for (int j = 0; j < piecesCount; j++)
        {
            float offsetZ = j * scaledPieceLength;
            if (isPrefabPivotInCenter) offsetZ += scaledPieceLength / 2f;
            
            
            Vector3 spawnPos = posA + segmentDir * offsetZ;
            Quaternion rotation = Quaternion.LookRotation(segmentDir, upVector);

            GameObject piece = Instantiate(piecePrefab, spawnPos, rotation, segmentRoot.transform);

            Vector3 scale = piece.transform.localScale;
            piece.transform.localScale = new Vector3(scale.x, scale.y, scale.z * scaleZ);

            tempPieces.Add(piece);
            upVector = Quaternion.AngleAxis(angleToRotateNextPieceBy, segmentDir) * upVector;
        }
        
        
        
        CombinePieces(segmentRoot, tempPieces, out _);

        SetupElectricArcComponent(segmentRoot, pointA, pointB);
        //SetupParticleSystem(segmentRoot, piecesCount, scaleZ, distance);
    }

    private Vector3 GetRootUp(in Vector3 segmentDir)
    {
        var dotUp = Vector3.Dot(segmentDir, transform.up);
        return Mathf.Abs(dotUp) > 0.99f ? -transform.forward : transform.up;
    }
    
    private void SetupElectricArcComponent(GameObject segmentRoot, Transform pointA, Transform pointB)
    {
        SimpleElectricArc templateArc = piecePrefab.GetComponent<SimpleElectricArc>();
        if (templateArc == null) templateArc = piecePrefab.GetComponentInChildren<SimpleElectricArc>();

        SimpleElectricArc arc = segmentRoot.AddComponent<SimpleElectricArc>();
        arc.startPoint = pointA;
        arc.endPoint = pointB;

        if (templateArc != null)
        {
            arc.textureResolution = templateArc.textureResolution;
            
            arc.radiusX = templateArc.radiusX;
            arc.heightX = templateArc.heightX;
            arc.verticalShiftX = templateArc.verticalShiftX;
            
            arc.radiusY = templateArc.radiusY;
            arc.heightY = templateArc.heightY;
            arc.verticalShiftY = templateArc.verticalShiftY;
            
            arc.radiusY = templateArc.radiusZ;
            arc.heightY = templateArc.heightZ;
            arc.verticalShiftY = templateArc.verticalShiftZ;
            
            arc.amplitudeX = templateArc.amplitudeX;
            arc.amplitudeY = templateArc.amplitudeY;
            arc.amplitudeZ = templateArc.amplitudeZ;
            
            arc.loopDurationX = templateArc.loopDurationX;
            arc.loopDurationY = templateArc.loopDurationY;
            arc.loopDurationZ = templateArc.loopDurationZ;
            
            arc.particlesPerSecondPerMeter = templateArc.particlesPerSecondPerMeter;
            
            arc.horizontalShiftX = templateArc.horizontalShiftX;
            arc.horizontalShiftY = templateArc.horizontalShiftY;
            arc.horizontalShiftZ = templateArc.horizontalShiftZ;
            
            
            
            arc.useSinusMultiplierX = templateArc.useSinusMultiplierX;
            if(templateArc.useSinusMultiplierX)
                arc.sinusMultiplierPowerX = templateArc.sinusMultiplierPowerX;
            else
                arc.multiplierCurveX =  templateArc.multiplierCurveX;
            
            
            arc.useSinusMultiplierY = templateArc.useSinusMultiplierY;
            if(templateArc.useSinusMultiplierY)
                arc.sinusMultiplierPowerY = templateArc.sinusMultiplierPowerY;
            else
                arc.multiplierCurveY =  templateArc.multiplierCurveY;

            arc.useSinusMultiplierZ = templateArc.useSinusMultiplierZ;
            if(templateArc.useSinusMultiplierZ)
                arc.sinusMultiplierPowerZ = templateArc.sinusMultiplierPowerZ;
            else
                arc.multiplierCurveZ =  templateArc.multiplierCurveZ;
            
            if (templateArc.arcParticles != null)
            {
                var part = Instantiate(templateArc.arcParticles, arc.transform);
                part.transform.localPosition=Vector3.zero;
                part.transform.localRotation = Quaternion.identity;
                part.transform.localScale = Vector3.one;
                arc.arcParticles = part;
            }
            
        }
    }
    
    // private void SetupParticleSystem(GameObject segmentRoot, int piecesCount, float scaleZ, float distance)
    // {
    //     ParticleSystem templatePS = piecePrefab.GetComponentInChildren<ParticleSystem>(true);
    //     if (templatePS == null) return;
    //     
    //
    //     ParticleSystem psInstance = Instantiate(templatePS, segmentRoot.transform);
    //     psInstance.name = "Segment_Particles";
    //     psInstance.transform.localPosition = Vector3.zero;
    //     psInstance.transform.localRotation = Quaternion.identity;
    //     psInstance.transform.localScale = Vector3.one;
    //
    //     if (psInstance.GetComponent<MeshFilter>()) DestroyImmediate(psInstance.GetComponent<MeshFilter>());
    //     if (psInstance.GetComponent<MeshRenderer>()) DestroyImmediate(psInstance.GetComponent<MeshRenderer>());
    //     if (psInstance.GetComponent<SimpleElectricArc>()) DestroyImmediate(psInstance.GetComponent<SimpleElectricArc>());
    //     if (psInstance.GetComponent<Collider>()) DestroyImmediate(psInstance.GetComponent<Collider>());
    //
    //     var shape = psInstance.shape;
    //     shape.enabled = true;
    //     shape.shapeType = ParticleSystemShapeType.Mesh;
    //     shape.mesh = UtilityFunctions.CreateCylinderZ(distance, sparkMeshRadius, amountOfFaces, false);
    //     shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
    //     
    //     // MeshFilter combinedMeshFilter = segmentRoot.GetComponent<MeshFilter>();
    //     // if (combinedMeshFilter != null && combinedMeshFilter.sharedMesh != null)
    //     // {
    //     //     shape.mesh = combinedMeshFilter.sharedMesh;
    //     // }
    //
    //     var emission = psInstance.emission;
    //     float multiplier = piecesCount * scaleZ;
    //     
    //     emission.rateOverTimeMultiplier *= multiplier;
    //     emission.rateOverDistanceMultiplier *= multiplier;
    //
    //     ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
    //     emission.GetBursts(bursts);
    //     for (int i = 0; i < bursts.Length; i++)
    //     {
    //         bursts[i].count = new ParticleSystem.MinMaxCurve(
    //             bursts[i].count.constantMin * multiplier,
    //             bursts[i].count.constantMax * multiplier
    //         );
    //     }
    //     emission.SetBursts(bursts);
    // }
    
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        var points = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith(RootName)) points.Add(child);
        }
        if (points.Count < 2) return;

        Gizmos.color = Color.blue;
        
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
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(dir, GetRootUp(dir)), Vector3.one);
                Gizmos.DrawWireCube(colliderPlace, new Vector3(colliderSize.x, colliderSize.y, dist));
            }
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}