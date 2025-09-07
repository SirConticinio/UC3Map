using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PathfinderController : MonoBehaviour
{
    public static PathfinderController _instance;
    
    public string roomId1;
    public string roomId2;

    public Color lineRendererColor = Color.red;
    public Color currentRoomColor = Color.green;
    public Color previousRoomColor = Color.gray;
    public Color nextRoomColor = Color.red;
    public float arrowSpeed = 10f;

    public Transform routeCheckboxParent;
    private bool useOnlyElevators = false;
    
    public TextMeshProUGUI overviewText;
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI floorText;

    [Header("Inner room pathfinder")]
    public RoomPathVisualizer roomPathVisualizer;
    public float cellSize = 0.8f;
    public float maxClearance = 2f;
    public float penaltyFactor = 1f;
    
    private int currentIndex = 0;
    private MapPathfindInstance currentPathfinder;
    
    // Start is called before the first frame update
    void Start()
    {
        _instance = this;
        SpawnElevatorCheckbox();
    }

    private void SpawnElevatorCheckbox()
    {
        GameObject spawned = GameObject.Instantiate(BundleController._instance.checkboxPrefab, routeCheckboxParent);
        SetUseElevators(spawned, false);
        spawned.transform.Find("Image").GetComponent<Button>().onClick.AddListener(() =>
        {
            SetUseElevators(spawned, !useOnlyElevators);
        });
        spawned.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "Ruta por ascensores";
        StaticUtils.RefreshLayoutGroupsImmediateAndRecursive(routeCheckboxParent.gameObject);
    }

    private void SetUseElevators(GameObject spawned, bool elevators)
    {
        useOnlyElevators = elevators;
        spawned.transform.Find("Image").GetComponent<Image>().sprite = (elevators ? BundleController._instance.checkboxOnTexture : BundleController._instance.checkboxOffTexture);
    }

    public void PathfindDebugRoute()
    {
        RoomData room1 = string.IsNullOrEmpty(roomId1) ? BundleController._instance.prevSelectedRoom : BundleController._instance.FindRoom(roomId1);
        RoomData room2 = string.IsNullOrEmpty(roomId2) ? BundleController._instance.selectedRoom : BundleController._instance.FindRoom(roomId2);
        MapPathfindInstance pathfinder = new MapPathfindInstance(room1, room2, false);
        pathfinder.FindPath();
    }

    public void CreateRouteFromSelectedData()
    {
        RoomData originRoom = BundleController._instance.selectedRouteOriginRoom;
        RoomData targetRoom = BundleController._instance.selectedRouteTargetRoom;
        CreateRouteFromData(originRoom, targetRoom);
    }

    public void CreateRouteFromData(RoomData originRoom, RoomData targetRoom)
    {
        if (originRoom == null || targetRoom == null)
        {
            return;
        }
        
        currentPathfinder = new MapPathfindInstance(originRoom, targetRoom, useOnlyElevators);
        currentPathfinder.FindPath();

        ModelController._instance.SetAllModelsAlpha(0.2f);
        HighlightStep(null);

        CameraController._instance.SetCameraPerspective();
        ModelController._instance.SetGeneralRotation(true);

        AppController._instance.GoToRouteShowMenu();
        GenerateRouteOverviewText();
        
        // if there's no route, at least highlight both rooms
        if (currentPathfinder.finishedRoute == null)
        {
            MapData originMap = BundleController._instance.FindMapDataFromRoom(originRoom);
            MapData targetMap = BundleController._instance.FindMapDataFromRoom(targetRoom);
            ModelController._instance.HighlightElement(ModelController._instance.FindSpawnedRoom(originMap, originRoom), Color.red);
            ModelController._instance.HighlightElement(ModelController._instance.FindSpawnedRoom(targetMap, targetRoom), Color.yellow);
        }
    }

    private void GenerateRouteOverviewText()
    {
        if (currentPathfinder.finishedRoute == null || currentPathfinder.finishedRoute.Count == 0)
        {
            overviewText.text = "No route found.";
            return;
        }

        List<MapData> visitedMaps = new List<MapData>();
        float totalDistance = 0;
        foreach (PathfindStep step in currentPathfinder.finishedRoute)
        {
            if (!visitedMaps.Contains(step.map))
            {
                visitedMaps.Add(step.map);
            }
            totalDistance += step.distance;
        }

        
        string text = "Route found!" +
                      $"\n-> Runs from {currentPathfinder.finishedRoute[0].room.prettyName} to {currentPathfinder.finishedRoute[^1].room.prettyName}." +
                      $"\n-> It consists of {currentPathfinder.finishedRoute.Count} rooms." +
                      $"\n-> Route goes across {visitedMaps.Count} floor(s)." +
                      $"\n-> Total distance is {totalDistance.ToString("0.00")} meters." +
                      $"\n\nPress the button to start the live guide.";
        overviewText.text = text;
    }

    public void StartLiveRoute()
    {
        ModelController._instance.SetAllModelsAlpha(0.45f);
        ModelController._instance.SetGeneralRotation(false);
        
        SetupStep(0);
        AppController._instance.GoToRouteStepMenu();
    }

    public void SetupStep(int index)
    {
        currentIndex = index;
        PathfindStep step = currentPathfinder.finishedRoute[currentIndex];

        stepText.text = step.instructions;
        ModelController._instance.ShowSeparateRoom(step.map, step.room);

        RoomPathfindInstance roomPathfind = new RoomPathfindInstance(
            step.room.points, step.originPos, step.targetPos, cellSize,
            maxClearance, penaltyFactor);

        roomPathfind.FindPath();
        List<Vector2> path = roomPathfind.finishedPath;
        
        // add initial and end positions for more smooth paths
        path[0] = step.originPos;
        path[^1] = step.targetPos;

        // enable cam into general map
        BundleController._instance.SetOnlyMapVisible(step.map);
        MovePathfindingCamera(step.map, step.room);
        
        // show highlight for current room, dimmed highlight for the others
        GameObject highlight = HighlightStep(step);
        //roomPathVisualizer.DrawPath(ModelController._instance.currentOrthoSeparateRoom, step.room, path);
        roomPathVisualizer.DrawPath(highlight, step.room, path, step.distance);
        
        // show floor name
        floorText.text = $"Mapa actual: {step.map.name}";
    }

    private GameObject HighlightStep(PathfindStep targetStep)
    {
        if (currentPathfinder.finishedRoute == null)
        {
            return null;
        }
        
        GameObject highlightStep = null;
        foreach (PathfindStep step in currentPathfinder.finishedRoute)
        {
            Color color = step == targetStep
                ? currentRoomColor
                : (targetStep != null && highlightStep == null) ? previousRoomColor : nextRoomColor;
            HighlightElement obj = ModelController._instance.HighlightElement(step.spawnedModel, color);
            if (step == targetStep)
            {
                highlightStep = obj.highlight;
            }
        }
        return highlightStep;
    }
    
    public void TogglePathfindingCameraOverview()
    {
        bool isOverview = Math.Abs(CameraController._instance.pathfindingOrthoCamera.orthographicSize - CameraController._instance.orthoCamera.orthographicSize) < 0.01f;
        if (isOverview)
        {
            PathfindStep step = currentPathfinder.finishedRoute[currentIndex];
            MovePathfindingCamera(step.map, step.room);
        }
        else
        {
            CameraController._instance.MovePathfindingCameraToOverview();
        }
    }

    private void MovePathfindingCamera(MapData mapData, RoomData roomData)
    {
        GameObject spawnedRoom = ModelController._instance.FindSpawnedRoom(mapData, roomData, true, true);
        CameraController._instance.EnablePathfindingCamera(spawnedRoom);
    }

    public void GoPrevious()
    {
        if (currentIndex > 0)
        {
            SetupStep(currentIndex-1);
        }
    }

    public void GoNext()
    {
        if (currentIndex < currentPathfinder.finishedRoute.Count-1)
        {
            SetupStep(currentIndex+1);
        }
    }
}
