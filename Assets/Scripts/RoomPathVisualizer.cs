using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class RoomPathVisualizer : MonoBehaviour
{
    private LineRenderer AssignLineRenderer(GameObject obj)
    {
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = obj.AddComponent<LineRenderer>();
            lr.startWidth = 0.22f;
            lr.endWidth = 0.22f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.loop = false;
            lr.useWorldSpace = true;
            lr.startColor = PathfinderController._instance.lineRendererColor;
            lr.endColor = PathfinderController._instance.lineRendererColor;
        }
        return lr;
    }

    public void DrawPath(GameObject target, RoomData roomData, List<Vector2> path, float distance)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("No path found!");
            return;
        }

        // displace the path to the target object
        List<Vector3> finalPath = FindDisplacedCoords(target, roomData, path);
        AppendArrowHead(finalPath);

        // and display it
        LineRenderer lr = AssignLineRenderer(target);
        lr.positionCount = 0;
        DOTween.To(() => lr.positionCount, x =>
        {
            lr.positionCount = x+1;
            for (int i = 0; i <= x; i++)
            {
                lr.SetPosition(i, finalPath[i]);
            }
        }, finalPath.Count-1, distance / PathfinderController._instance.arrowSpeed);
    }

    private void AppendArrowHead(List<Vector3> path)
    {
        Vector3 tip = path[path.Count - 1];
        Vector3 prev = path[path.Count - 2];

        Vector3 direction = (tip - prev).normalized;
        Vector3 basePos = tip - direction * 0.5f;

        // Perpendicular
        Vector3 n = new Vector3(-direction.y, direction.x, 0);
        float width = 0.25f;
        Vector3 left = basePos + n * width;
        Vector3 right = basePos - n * width;

        path.Add(left);
        path.Add(right);
        path.Add(tip);
    }

    private List<Vector3> FindDisplacedCoords(GameObject target, RoomData roomData, List<Vector2> path)
    {
        float height = 20; // TODO we should obtain height from mapdata, but for now this works
        
        // i'm gonna center all our coords and then displace them to the center of our target
        // since it's the same scale, it should be fine
        Vector2 center = roomData.CalculateCenter();
        Vector3 modelCenter = target.GetComponent<MeshCollider>().bounds.center;

        // displace our coords
        List<Vector3> newCoords = new List<Vector3>();
        foreach (Vector2 coord in path)
        {
            Vector2 offset = coord - center;
            // the Y is inverted so that's why we remove it instead of adding
            newCoords.Add(new Vector3(offset.x + modelCenter.x, modelCenter.y - offset.y, modelCenter.z - height));
        }
        
        return newCoords;
    }
}