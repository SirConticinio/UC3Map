
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapPathfindInstance
{
    private RoomData origin;
    private MapData originMapData;
    private MapData targetMapData;
    private RoomData target;
    private bool useOnlyElevators;

    private List<RoomIntersection> targetIntersections = new List<RoomIntersection>();
    private Vector2 targetCenter;

    public List<PathfindStep> finishedRoute;
    
    public MapPathfindInstance(RoomData origin, RoomData target, bool useOnlyElevators)
    {
        this.origin = origin;
        this.target = target;
        this.useOnlyElevators = useOnlyElevators;
        CalculateInitialParameters();
    }

    private void CalculateInitialParameters()
    {
        // find target map data
        originMapData = BundleController._instance.FindMapDataFromRoom(origin);
        targetMapData = BundleController._instance.FindMapDataFromRoom(target);
        
        // calculate target center and its intersections
        targetCenter = target.CalculateCenter();
        foreach (RoomIntersection intersection in targetMapData.intersections)
        {
            if (intersection.roomId1.Equals(target.id) || intersection.roomId2.Equals(target.id))
            {
                targetIntersections.Add(intersection);
            }
        }
    }
    
    public void FindPath()
    {
        // A* algorithm
        // create the open and closed list
        SimpleNodeQueue<Node> openList = new SimpleNodeQueue<Node>(n => n.f);
        List<Node> closedList = new List<Node>();
        List<Node> adjacentList = new List<Node>();
        
        // add the first element
        Node current = CreateIntersectionNode(originMapData, origin, null, null, 0);
        openList.Enqueue(current);

        // we iterate over the room intersections
        while (openList.IsNotEmpty())
        {
            // retrieve the next node and its adjacent nodes
            current = openList.Dequeue();
            closedList.Add(current);
            adjacentList = FindAdjacentNodes(current);

            foreach (Node adjacentNode in adjacentList)
            {
                if (adjacentNode.room == target)
                {
                    // finish! this node is the target
                    FinishRoute(adjacentNode);
                    return;
                }
                
                // we check if we've already visited this node with a lower f, either in the open or the closed list
                // if so, we skip this one
                bool skip = HasAlreadyVisited(adjacentNode, openList.list) || HasAlreadyVisited(adjacentNode, closedList);
                if (!skip)
                {
                    // we add it to the open list!
                    openList.Enqueue(adjacentNode);
                }
            }

            //Debug.Log("Visiting: " + current.room.id + ", " + current.cost + ", " + current.priority + " = " + current.f);
            if (closedList.Count > 2000000)
            {
                break;
            }
        }
        
        // if we reach this place, there was no path found
        Debug.Log("No path found!");
    }

    private void FinishRoute(Node finalNode)
    {
        Debug.Log("Found path!");
        
        // iterate route from end to start
        finishedRoute = new List<PathfindStep>();
        string previousInstruction = null;
        AbstractIntersection previousIntersection = null;
        
        while (finalNode != null)
        {
            Debug.Log("Intersection is: " + finalNode.entryIntersection);

            // add instructions, skipping the moving-to-center-of-stairs node
            if (!(finalNode.entryIntersection is FloorIntersection && finalNode.parent?.entryIntersection is not FloorIntersection))
            {
                Vector2 stepOrigin = finalNode.entryIntersection?.GetLocation() ?? finalNode.room.CalculateCenter();
                Vector2 stepTarget = previousIntersection?.GetLocation() ?? finalNode.room.CalculateCenter();
                float meters = (stepTarget - stepOrigin).magnitude;
                
                string instruction = "You're in " + finalNode.room.prettyName + "\n";
                instruction += string.IsNullOrEmpty(previousInstruction) ? "You arrived!" : previousInstruction;
                instruction += "\n-> Distance: (" + meters.ToString("0.00") + "m)\n";
                
                PathfindStep step = new PathfindStep(finalNode.map, finalNode.room, stepOrigin, stepTarget, meters, instruction, finalNode.entryIntersection, previousIntersection);
                finishedRoute.Add(step);
                
                previousInstruction = finalNode.entryIntersection is FloorIntersection
                    ? "-> Change to Floor " + finalNode.map.name
                    : "-> Move to " + finalNode.room.prettyName;
            }
            
            previousIntersection = finalNode.entryIntersection;
            finalNode = finalNode.parent;
        }

        // reverse finished route
        finishedRoute.Reverse();
    }

    private bool HasAlreadyVisited(Node node, ICollection<Node> list)
    {
        foreach (Node openNode in list)
        {
            if (openNode.room == node.room && openNode.entryIntersection == node.entryIntersection && openNode.f <= node.f)
            {
                return true;
            }
        }
        return false;
    }

    private List<Node> FindAdjacentNodes(Node node)
    {
        List<Node> nodeList = FindAdjacentFloorNodes(node);
        
        if (node.entryIntersection is FloorIntersection)
        {
            // we also need to change floors!
            nodeList.AddRange(FindChangingFloorNodes(node));
        }
        
        // now we order the list based on the F function
        nodeList = nodeList.OrderBy(listNode => listNode.f).ToList();
        return nodeList;
    }

    private List<Node> FindChangingFloorNodes(Node node)
    {
        List<Node> nodeList = new List<Node>();
        if (node.entryIntersection is not FloorIntersection inter || useOnlyElevators != inter.isElevator)
        {
            // we return if we're looking for elevators and this is stairs, or we're looking for stairs and this is an elevator
            return nodeList;
        }
        
        // here we want to transverse to the target intersections in the other floors, so we also need to change the map
        foreach (FloorIntersection intersection in node.map.floorIntersections)
        {
            if (intersection.originRoomId.Equals(node.room.id))
            {
                foreach (FloorIntersectionTarget floorTarget in intersection.targets)
                {
                    MapData newMap = BundleController._instance.FindMapDataFromId(floorTarget.mapId);
                    FloorIntersection targetIntersection = BundleController._instance.FindFloorIntersectionFromId(floorTarget.intersectionId);
                    nodeList.Add(CreateAdjacentNode(newMap, node, intersection, targetIntersection.originRoomId));
                }
            }
        }
        return nodeList;
    }

    private List<Node> FindAdjacentFloorNodes(Node node)
    {
        List<Node> nodeList = new List<Node>();

        // first we add the nodes to change the room
        foreach (RoomIntersection intersection in node.map.intersections)
        {
            if (intersection.roomId1.Equals(node.room.id))
            {
                nodeList.Add(CreateAdjacentNode(node.map, node, intersection, intersection.roomId2));
            }
            else if (intersection.roomId2.Equals(node.room.id))
            {
                nodeList.Add(CreateAdjacentNode(node.map, node, intersection, intersection.roomId1));
            }
        }
        
        // and then we add the nodes to change the floor
        foreach (FloorIntersection intersection in node.map.floorIntersections)
        {
            if (intersection.originRoomId.Equals(node.room.id))
            {
                nodeList.Add(CreateAdjacentNode(node.map, node, intersection, intersection.originRoomId));
            }
        }
        
        return nodeList;
    }

    private Node CreateAdjacentNode(MapData mapData, Node parent, AbstractIntersection intersection, string newRoomId)
    {
        // first we try to find the new room
        RoomData roomData = BundleController._instance.FindRoom(newRoomId);
        if (roomData == null)
        {
            Debug.LogError("room data is null! " + newRoomId);
        }
        
        // we calculate cost and create the intersection node
        float costBetweenIntersections = CalculateCostBetweenIntersections(parent, intersection);
        return CreateIntersectionNode(mapData, roomData, parent, intersection, parent.cost + costBetweenIntersections);
    }

    private float CalculateCostBetweenIntersections(Node parent, AbstractIntersection intersection)
    {
        // now we calculate the linear cost from the new intersection.
        // if the previous node doesn't have an intersection, we start from its center
        Vector2 originVector = parent.entryIntersection?.GetLocation() ?? parent.center;
        // TODO this cost could take into account the altitude, so we can pathfind through floors better.
        return (intersection.GetLocation() - originVector).magnitude;
    }

    private Node CreateIntersectionNode(MapData mapData, RoomData roomData, Node parent, AbstractIntersection previousIntersection, float cost)
    {
        Node node = new Node(mapData, roomData, parent, previousIntersection);
        return SetupNodeF(node, cost);
    }

    private Node SetupNodeF(Node node, float cost)
    {
        node.cost = cost;
        node.priority = CalculatePriority(node);
        node.f = node.cost + node.priority;
        return node;
    }

    private float CalculatePriority(Node node)
    {
        // current heuristics: euclidean distance from previous intersection/center to closest final room intersection
        Vector2 originVector = node.entryIntersection?.GetLocation() ?? node.center;
        
        RoomIntersection closestIntersection = null;
        float closestSquaredDistance = float.MaxValue;
        foreach (RoomIntersection intersection in targetIntersections)
        {
            float distance = (intersection.intersection - originVector).sqrMagnitude;
            if (distance < closestSquaredDistance)
            {
                closestIntersection = intersection;
                closestSquaredDistance = distance;
            }
        }
        return closestIntersection != null ? (closestIntersection.intersection - originVector).magnitude : 0;
    }
}

class Node
{
    public MapData map;
    public RoomData room;
    public float priority;
    public float cost;
    public float f;
    public Vector2 center;
    public AbstractIntersection entryIntersection;
    public Node parent;
    
    public Node(MapData mapData, RoomData room, Node parent, AbstractIntersection entryIntersection)
    {
        this.map = mapData;
        this.room = room;
        this.entryIntersection = entryIntersection;
        this.parent = parent;
        this.center = room.CalculateCenter();
    }
}