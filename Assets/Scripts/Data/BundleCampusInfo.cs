using System.Collections.Generic;
using UnityEngine;

// this type should be in-sync with the one that PlanMapper serializes
public class BundleCampusInfo
{
    public string id;
    public string name;
    public int version;
    public List<BundleBuildingInfo> buildings;
}