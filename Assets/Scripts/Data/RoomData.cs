using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

// this type should be in-sync with the one that PlanMapper serializes
public class RoomData
{
    public string id;
    public string name;
    public List<Vector2> points;
    public string code;
    public string notes;

    public string prettyName;

    public Vector2 CalculateCenter()
    {
        return StaticUtils.FindCoordsCenter(points);
    }

    [OnDeserialized]
    private void CalculatePrettyName(StreamingContext context)
    {
        prettyName = string.IsNullOrEmpty(code)
            ? name
            : code + (string.IsNullOrEmpty(name) ? "" : $" ({name})");
    }
}