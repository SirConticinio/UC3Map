using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class StaticUtils
{
    public static void KillChildren(Transform parent)
    {
        for (int i = parent.childCount-1; i >= 0 ; i--)
        {
            GameObject.Destroy(parent.GetChild(i).gameObject);
        }
    }
    public static void RefreshLayoutGroupsImmediateAndRecursive(GameObject root)
    {
        foreach (LayoutGroup layoutGroup in root.GetComponentsInChildren<LayoutGroup>())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }
    }
    
    // from https://discussions.unity.com/t/changing-lightweight-shader-surface-type-in-c/720000/4
    public static void SetMaterialTransparent(Material material, bool enabled)
    {
        material.SetFloat("_Surface", enabled ? 1 : 0);
        material.SetShaderPassEnabled("SHADOWCASTER", !enabled);
        material.renderQueue = enabled ? 3000 : 2000;
        material.SetFloat("_DstBlend", enabled ? 10 : 0);
        material.SetFloat("_SrcBlend", enabled ? 5 : 1);
        material.SetFloat("_ZWrite", enabled ? 0 : 1);
    }
    
    public static Vector2 FindCoordsCenter(List<Vector2> points)
    {
        if (points == null || points.Count == 0)
        {
            return new Vector2();
        }

        Vector2 minPoint = new Vector2(points[0].x, points[0].y);
        Vector2 maxPoint = new Vector2(minPoint.x, minPoint.y);
        foreach (Vector2 point in points)
        {
            if (point.x < minPoint.x)
            {
                minPoint.x = point.x;
            }
            if (point.y < minPoint.y)
            {
                minPoint.y = point.y;
            }
            if (point.x > maxPoint.x)
            {
                maxPoint.x = point.x;
            }
            if (point.y > maxPoint.y)
            {
                maxPoint.y = point.y;
            }
        }
        
        return new Vector2((minPoint.x + maxPoint.x) / 2, (minPoint.y + maxPoint.y) / 2);
    }

    public static List<Vector2> CenterCoords(List<Vector2> points, Vector2 center)
    {
        if (points == null || points.Count == 0)
        {
            return points;
        }

        // the displacement vector is the distance between the coords center and 0,0
        Vector2 offset = new Vector2() - center;
        
        // displace our coords
        List<Vector2> newCoords = new List<Vector2>();
        foreach (Vector2 coord in points)
        {
            newCoords.Add(coord + offset);
        }

        return newCoords;
    }
}