
using UnityEngine;

public class PathfindStep
{
    public MapData map;
    public RoomData room;
    public Vector2 originPos;
    public Vector2 targetPos;
    public float distance;
    public string instructions;
    public AbstractIntersection originIntersection;
    public AbstractIntersection targetIntersection;
    public GameObject spawnedModel;

    public PathfindStep(MapData map, RoomData room, Vector2 originPos, Vector2 targetPos, float distance, string instructions, AbstractIntersection originIntersection, AbstractIntersection targetIntersection)
    {
        this.map = map;
        this.room = room;
        this.originPos = originPos;
        this.targetPos = targetPos;
        this.distance = distance;
        this.instructions = instructions;
        this.originIntersection = originIntersection;
        this.targetIntersection = targetIntersection;

        spawnedModel = ModelController._instance.FindSpawnedRoom(map, room);
    }
}
