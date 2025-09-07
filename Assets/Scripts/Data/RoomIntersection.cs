using System.Collections.Generic;
using UnityEngine;

// this type should be in-sync with the one that PlanMapper serializes
public class RoomIntersection : AbstractIntersection
{
    public string id;
    public string roomId1;
    public string roomId2;
    public Vector2 intersection;

    public override string ToString()
    {
        return "ID: " + id 
               + "roomId1: " + roomId1
               + "roomId2: " + roomId2
               + "intersection: " + intersection;
    }

    public override Vector2 GetLocation()
    {
        return intersection;
    }
}