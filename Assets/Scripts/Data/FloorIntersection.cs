using System.Collections.Generic;
using UnityEngine;

// this type should be in-sync with the one that PlanMapper serializes
public class FloorIntersection : AbstractIntersection
{
    public string id;
    public Vector2 intersection;
    public bool isElevator;
    public string originRoomId;
    public List<FloorIntersectionTarget> targets;
    public override Vector2 GetLocation()
    {
        return intersection;
    }
}