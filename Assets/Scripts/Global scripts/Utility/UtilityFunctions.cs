using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using Random = UnityEngine.Random;


public static class UtilityFunctions 
{
    public interface IPlayable
    {
        public float ClipSpeed => 0f;   
        public bool ClipIsNull  => true;
        public AnimationClip Clip => null;
        public bool ShouldResetTimeOfPlayableAnyway => false;
    }

    public enum BaseActionTransitionsEnum { Base, BaseToAction, Action, ActionToBase }

    public static T GetRandomElement<T>(this IList<T> list, ref int lastIndex)
    {
        if (list.Count <= 1)
        {
            lastIndex = 0;
            return list[0];
        }
        
        var newIndex = Random.Range(0, list.Count - 1);
            
        if (newIndex >= lastIndex)
        {
            ++newIndex;
        }
        lastIndex = newIndex;
        return list[newIndex];
    }
    
    public static void DrawColliderGizmo(this Transform transform, Color color, float insideAlphaFraction=0.1f)
    {
        var prevMatrix = Gizmos.matrix;
        var prevColor = Gizmos.color;
        
        var color2 = color;
        color2.a *= insideAlphaFraction;
        
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = color2;
        if (transform.TryGetComponent(out BoxCollider box))
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = color;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (transform.TryGetComponent(out SphereCollider sphere))
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = color;
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }

        Gizmos.matrix = prevMatrix;
        Gizmos.color = prevColor;
    }
    
    public static Mesh CreateCylinderZ(float length, float radius, int radialSegments = 8, bool includeCaps = true)
    {
        Mesh mesh = new Mesh { name = "CylinderZ" };

        if (radialSegments < 3) radialSegments = 3;
        float halfLength = length * 0.5f;

        int sideVertCount = (radialSegments + 1) * 2;
        int capVertCount = includeCaps ? (radialSegments + 1) * 2 + 2 : 0;
        int totalVerts = sideVertCount + capVertCount;

        Vector3[] vertices = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];

        int sideTrianglesCount = radialSegments * 6;
        int capTrianglesCount = includeCaps ? radialSegments * 6 : 0;
        int[] triangles = new int[sideTrianglesCount + capTrianglesCount];

        int vertIdx = 0;

        for (int i = 0; i <= radialSegments; i++)
        {
            float progress = (float)i / radialSegments;
            float angle = progress * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            vertices[vertIdx] = new Vector3(x, y, -halfLength);
            uvs[vertIdx] = new Vector2(progress, 0f);
            vertIdx++;

            vertices[vertIdx] = new Vector3(x, y, halfLength);
            uvs[vertIdx] = new Vector2(progress, 1f);
            vertIdx++;
        }

        int triIdx = 0;
        for (int i = 0; i < radialSegments; i++)
        {
            int b0 = i * 2;
            int f0 = i * 2 + 1;
            int b1 = (i + 1) * 2;
            int f1 = (i + 1) * 2 + 1;

            triangles[triIdx++] = b0;
            triangles[triIdx++] = f1;
            triangles[triIdx++] = f0;

            triangles[triIdx++] = b0;
            triangles[triIdx++] = b1;
            triangles[triIdx++] = f1;
        }

        
        
        if (includeCaps)
        {
            int backCenterIdx = vertIdx;
            vertices[vertIdx] = new Vector3(0, 0, -halfLength);
            uvs[vertIdx] = new Vector2(0.5f, 0.5f);
            vertIdx++;

            int frontCenterIdx = vertIdx;
            vertices[vertIdx] = new Vector3(0, 0, halfLength);
            uvs[vertIdx] = new Vector2(0.5f, 0.5f);
            vertIdx++;

            int backRingStart = vertIdx;
            for (int i = 0; i <= radialSegments; i++)
            {
                float angle = ((float)i / radialSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                vertices[vertIdx] = new Vector3(x, y, -halfLength);
                uvs[vertIdx] = new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f);
                vertIdx++;
            }

            int frontRingStart = vertIdx;
            for (int i = 0; i <= radialSegments; i++)
            {
                float angle = ((float)i / radialSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                vertices[vertIdx] = new Vector3(x, y, halfLength);
                uvs[vertIdx] = new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f);
                vertIdx++;
            }

            for (int i = 0; i < radialSegments; i++)
            {
                triangles[triIdx++] = backCenterIdx;
                triangles[triIdx++] = backRingStart + i;
                triangles[triIdx++] = backRingStart + i + 1;
            }

            for (int i = 0; i < radialSegments; i++)
            {
                triangles[triIdx++] = frontCenterIdx;
                triangles[triIdx++] = frontRingStart + i + 1;
                triangles[triIdx++] = frontRingStart + i;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
    
    public static List<Transform> GetAllLeaves(Transform root, ISet<Transform> ignore)
    {
        var leaves = new List<Transform>();
        CollectLeavesRecursive(root, leaves, ignore);
        return leaves;
    }

    private static void CollectLeavesRecursive(Transform current, ICollection<Transform> leaves, ISet<Transform> ignore)
    {
        if (current == null) return;

        var haveChild = false;
        
        foreach (Transform child in current)
        {
            if (!ignore.Contains(child))
            {
                CollectLeavesRecursive(child, leaves, ignore);
                haveChild = true;
            }
        }
        
        if(haveChild)
            leaves.Add(current);
    }
    
    public static string GetPathFromRoot(this Transform current, Transform root = null, char separator = '/')
    {
        var pathNodes = new List<string>();
        var t = current;

        while (t != null)
        {
            pathNodes.Add(t.name);

            if (t == root)
                break;

            t = t.parent;
        }

        pathNodes.Reverse();

        return string.Join(separator.ToString(), pathNodes);
    }

    public static Vector3 LerpByDistance(Vector3 a, Vector3 b, float distance)
    {
        var dir = b - a;
        var sqrMagnitude = dir.sqrMagnitude;

        if (sqrMagnitude <= 0.0001f) return a;
        
        var t = distance / Mathf.Sqrt(sqrMagnitude);
        return a + dir * Mathf.Clamp01(t);
    }
    
    public static Transform MakeEmptyObject(string name, Transform parent)
    {
        var res = new GameObject(name).transform;
        res.SetParent(parent);
        res.localPosition = Vector3.zero;
        res.localRotation = Quaternion.identity;
        return res;
    }
    
    public static Transform MakeEmptyObjectOrphan(string name, Transform parent)
    {
        var res = new GameObject(name).transform;
        res.position = parent.position;
        res.rotation = parent.rotation;
        return res;
    }
    
    public static float GetTransitionFraction(UtilityTimers.IFractionTimer<BaseActionTransitionsEnum> timer, float weightMultiplier = 1f)
    {
        return timer.Value switch
        {
            BaseActionTransitionsEnum.Action => weightMultiplier,
            BaseActionTransitionsEnum.BaseToAction => Mathf.Clamp01(timer.TimeFraction) * weightMultiplier,
            BaseActionTransitionsEnum.ActionToBase => (1f - Mathf.Clamp01(timer.TimeFraction)) * weightMultiplier,
            _ => 0f
        };
    }
    public static float GetAnimationSpeed(AnimationClip animationClip, float targetTime)
    {
        return animationClip.length / Mathf.Max(0.001f, targetTime);
    }
    
    public static float GetAnimationSpeed(AnimationClip animationClip, float targetTime, float crossFadeDuration)
    {
        return animationClip.length / Mathf.Max(0.001f, animationClip.isLooping ? targetTime : targetTime + crossFadeDuration);
    }

    public static float GetSpeedFraction(AnimationClip target, AnimationClip origin)
    {
        return origin.length / Mathf.Max(0.001f, target.length);
    }
    
    
    public static Vector3 ProjectPointOnLine(Transform point, Transform line)
    {
        var toPoint = point.position - line.position;
    
        var t = Vector3.Dot(toPoint, line.forward);
        
        return line.position + line.forward * t;
    }
    
    
    private static float distancePrecision = 0.01f;
    private static float fractionPrecision = 0.01f;
    private static float stepFromHitPoint = 0.05f;
    
    public static Vector3 GetSphereRayCastPoint(Vector3 origin, Vector3 direction, float distance, float cameraRadius, LayerMask ignoreLayers)
    {
        direction.Normalize();
        RaycastHit hit;
        if (Physics.SphereCast(origin, cameraRadius, direction, out hit, distance, ~ignoreLayers))
        {
            return  origin + direction * hit.distance;
        }
    
        return origin + direction * distance;
    }
    private static readonly Collider[] CollidersForNonAlloc = new Collider[6];
    
    public static bool HasLineOfSight(Vector3 origin, Collider target, int layerMask)
    {
        var count = Physics.OverlapSphereNonAlloc(origin, 0.02f, CollidersForNonAlloc, layerMask);
        for(var i=0;i<count;++i)
        {
            if (CollidersForNonAlloc[i] == target)
            {
                return true;
            }
        }
        if (Physics.Linecast(origin, target.bounds.center, out var hit, layerMask))
        {
            if (hit.collider == target)
            {
                return true;
            }
        }
        return false;
    }
    
    
    public static bool HasLineOfSight(Vector3 origin, Vector3 target, int layerMask, int targetLayerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        var count = Physics.OverlapSphereNonAlloc(origin, 0.02f, CollidersForNonAlloc, layerMask, queryTriggerInteraction);
        for(var i=0;i<count;++i)
        {
            if (((1<<CollidersForNonAlloc[i].gameObject.layer) & targetLayerMask) == 0) 
            {
                return false;
            }
        }
        
        if (Physics.Linecast(origin, target, out var hit, layerMask, queryTriggerInteraction))
        {
            if (((1<<hit.collider.gameObject.layer) & targetLayerMask) != 0) 
            {
                return true;
            }
        }
        return false;
    }


    private const int CurveEvaluatingResolution = 100;
    public static float EvaluateCurveAverage(AnimationCurve curve)
    {
        var sum = 0f;
        for (var i = 0; i < CurveEvaluatingResolution; i++)
        {
            var t = (float)i / (CurveEvaluatingResolution - 1);
            sum += curve.Evaluate(t);
        }
        return sum / CurveEvaluatingResolution;
    }

    public static AnimationCurve BakeIntegralCurve(AnimationCurve curve)
    {
        var res = new AnimationCurve();
        res.AddKey(0f, 0f);
        var accum = 0f;
        var dt = 1f / CurveEvaluatingResolution;
        var prevVal = curve.Evaluate(0f);
        for (var i = 1; i <= CurveEvaluatingResolution; i++)
        {
            var t = i * dt;
            var currVal = curve.Evaluate(t);
            
            accum += (prevVal + currVal) * 0.5f * dt; 
            res.AddKey(t, accum);
            prevVal = currVal;
        }

        return res;
    }
    
    public static AnimationCurve BakeDistanceCurve(AnimationCurve speedCurve, float desiredDistance)
    {
        var values = new float[CurveEvaluatingResolution + 1];
        var dt = 1f / CurveEvaluatingResolution;
        var accum = 0f;
        var prevVal = speedCurve.Evaluate(0f);
        values[0] = 0f;

        for (var i = 1; i <= CurveEvaluatingResolution; i++)
        {
            var currVal = speedCurve.Evaluate(i * dt);
            accum += (prevVal + currVal) * 0.5f * dt;
            values[i] = accum;
            prevVal = currVal;
        }

        var res = new AnimationCurve();

        if (accum <= 0.001f)
        {
            res.AddKey(0f, 0f);
            res.AddKey(1f, desiredDistance);
            return res;
        }

        var scale = desiredDistance / accum;
        for (var i = 0; i <= CurveEvaluatingResolution; i++)
        {
            res.AddKey(i * dt, values[i] * scale);
        }

        return res;
    }
    
    public static Vector3 GetCapsuleRayCastPoint(Vector3 origin, Vector3 centerToTop, float capsuleRadius, Vector3 direction, float distance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        direction.Normalize();
        
        int iterations = 70;
        var capsuleUp = centerToTop.normalized;

        Vector3 result = origin;
        //String resultCouse = "Origin";

        var binSearchResPoint = Vector3.zero;
        var binSearchResNormal = Vector3.zero;
        
        float distLeft = 0;
        float distRight = distance;
        while (distRight - distLeft > distancePrecision)
        {
            var distMid = (distLeft + distRight) / 2;


            bool found = false;
            //this internal binary search projects the point origin + direction * distMid onto the closest surface touched by capsule
            float fracLeft = 0f;
            float fracRight = 1f;
            while (fracRight - fracLeft > fractionPrecision)
            {
                iterations--;
                if (iterations <= 0)
                    return result;
                
                
                var fracMid = (fracRight + fracLeft) / 2;
                var midCenterToTop = centerToTop * fracMid;
                var midRadius = capsuleRadius * fracMid;


                if (Physics.CapsuleCast(origin - midCenterToTop, origin + midCenterToTop, midRadius,
                        direction, out RaycastHit hit, distMid, layerMask, queryTriggerInteraction))
                {
                    //Debug.Log("Fraction checking: " + fracMid + " for dist mid = " + distMid + " === success");
                    fracRight = fracMid;
                    binSearchResPoint = hit.point;
                    binSearchResNormal = hit.normal;
                    found = true;
                }
                else
                {
                    //Debug.Log("Fraction checking: " + fracMid + " for dist mid = " + distMid + " === fail");
                    fracLeft = fracMid;
                }
            }

            if (found)
            {
                //some capsule touched some surface
                Vector3 targetPos;

                float dot = Vector3.Dot(binSearchResNormal, capsuleUp);

                targetPos = binSearchResPoint + binSearchResNormal * (capsuleRadius + stepFromHitPoint);
                if (dot > 0.01f)
                {
                    targetPos += centerToTop;
                }
                else if (dot < -0.01f)
                {
                    targetPos -= centerToTop;
                }

                if (!Physics.CheckCapsule(targetPos - centerToTop, targetPos + centerToTop, capsuleRadius,
                        layerMask, queryTriggerInteraction))
                {
                    //resultCouse = "Found by checking, ";
                    result = targetPos;
                    distLeft = distMid;
                }
                else
                {
                    //Debug.Log("Distance checking = " + distMid + " === making less");
                    distRight = distMid;
                }
            }
            else
            {
                //we are free to move to that position since no capsule touched anything
                //Debug.Log("Distance checking = " + distMid + " === making more by free to move");
                //resultCouse = "Free place";
                result = origin + direction * distMid;
                distLeft = distMid;
            }

        }
        //Debug.Log(resultCouse);
        return result;
    }
    
    public static Vector3 FromLocalToGlobalByZX(Vector3 forward, Vector3 localVector)
    {
        var lookDirXZ = new Vector3(forward.x, 0f, forward.z).normalized;

        if (lookDirXZ == Vector3.zero) 
            return localVector;

        var cameraGroundedRotation = Quaternion.LookRotation(lookDirXZ, Vector3.up);
        
        return cameraGroundedRotation * localVector;
    }

    public static float ChangeMeasurementScale(float valueA, float minA, float maxA, float minB, float maxB)
    {
        return Math.Clamp((valueA - minA) *
            (maxB-minB)/(maxA-minA) + minB,
            minB, maxB);
    }
    
    ///<summary>
    /// Fraction = (maxB-minB)/(maxA-minA)
    ///</summary>
    public static float ChangeMeasurementScaleFraction(float valueA, float minA, float fraction, float minB, float maxB)
    {
        return Math.Clamp((valueA - minA) *
            fraction + minB,
            minB, maxB);
    }

}
