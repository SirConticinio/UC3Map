using System.Collections.Generic;
using UnityEngine;

// this type should be in-sync with the one that PlanMapper serializes
public class MapData
{
    public string id;
    public string name;
    public List<RoomData> rooms;
    public List<RoomIntersection> intersections;
    public List<CoordsReference> coordsReferences;
    public List<FloorIntersection> floorIntersections;
    public int version;
    public float height;
    public float altitude;
    public float scale;

    // new variables used only by UC3Map
    public bool isEnabled = true;
}