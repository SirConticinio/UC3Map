using System.Collections.Generic;
using UnityEngine;

// this type should be in-sync with the one that PlanMapper serializes
public class BundleFloorInfo
{
    public string id;
    public string name;
    public int number;
    public bool isGroundFloor;
    public int version;
}