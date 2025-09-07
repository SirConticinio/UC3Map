using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BundleController : MonoBehaviour
{
    public static BundleController _instance;
    public TextMeshProUGUI selectedText;

    public Transform spawnedFloorsParent;
    
    [Header("Room info")]
    public GameObject roomInfoPrefab;
    public Transform roomInfoParent;
    public TMP_InputField roomInfoInputField;
    public TextMeshProUGUI roomInfoSelectedText;
    private RoomData roomInfoSelectedRoom;
    
    [Header("Room route")]
    public GameObject roomRoutePrefab;
    public Transform roomRouteParent;
    public TMP_InputField roomRouteInputField;
    public TextMeshProUGUI roomRouteSelectedText;

    [Header("Checkbox")]
    public GameObject checkboxPrefab;
    public Sprite checkboxOnTexture;
    public Sprite checkboxOffTexture;
    private Dictionary<MapData, GameObject> spawnedFloorCheckboxes = new Dictionary<MapData, GameObject>();
    
    private List<MapData> loadedMaps = new List<MapData>();
    private BundleCampusInfo loadedCampus;
    public RoomData selectedRoom;
    public RoomData prevSelectedRoom;

    public MapData selectedRouteOriginMap;
    public RoomData selectedRouteOriginRoom;
    public MapData selectedRouteTargetMap;
    public RoomData selectedRouteTargetRoom;
    
    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        NullValueHandling = NullValueHandling.Ignore
    };

    // Start is called before the first frame update
    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        SetupRoomLookup();
        
        // temporarily load campus automatically if the main menu ain't active
        if (!AppController._instance.mainMenu.activeSelf)
        {
            LoadCurrentCampusData();
        }
    }

    private void SetupRoomLookup()
    {
        SetupRoomSearchBars(roomInfoInputField, roomInfoParent, roomInfoPrefab, (spawned, mapData, roomData) =>
        {
            spawned.transform.Find("View").GetComponent<Button>().onClick.AddListener(() =>
            {
                roomInfoSelectedRoom = roomData;
                UpdateSelectedRoomInfoText();
                ModelController._instance.ShowSeparateRoom(mapData, roomData);
                AppController._instance.GoToRoomShowMenu();
            });
            spawned.transform.Find("Select").GetComponent<Button>().onClick.AddListener(() =>
            {
                GameObject model = ModelController._instance.FindSpawnedRoom(mapData, roomData);
                SelectRoom(model);
                AppController._instance.GoToMainMenu();
            });
        });
        
        SetupRoomSearchBars(roomRouteInputField, roomRouteParent, roomRoutePrefab, (spawned, mapData, roomData) =>
        {
            spawned.transform.Find("Origin").GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedRouteOriginMap = mapData;
                selectedRouteOriginRoom = roomData;
                UpdateRouteInfoText();
            });
            spawned.transform.Find("Target").GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedRouteTargetMap = mapData;
                selectedRouteTargetRoom = roomData;
                UpdateRouteInfoText();
            });
        });
    }

    private void SetupRoomSearchBars(TMP_InputField inputField, Transform infoParent, GameObject infoPrefab, UnityAction<GameObject, MapData, RoomData> dispatch)
    {
        inputField.onValueChanged.AddListener((text) =>
        {
            string lower = text.ToLowerInvariant();
            StaticUtils.KillChildren(infoParent);
            
            // now we iterate all maps to setup the prefabs
            List<GameObject> spawnedObjs = new List<GameObject>();
            foreach (MapData mapData in loadedMaps)
            {
                foreach (RoomData roomData in mapData.rooms)
                {
                    if (string.IsNullOrEmpty(text) || roomData.prettyName.ToLowerInvariant().Contains(lower))
                    {
                        // we spawn it
                        GameObject spawned = Instantiate(infoPrefab, infoParent);
                        spawnedObjs.Add(spawned);
                        spawned.name = roomData.prettyName;
                        
                        dispatch.Invoke(spawned, mapData, roomData);

                        string topText = string.IsNullOrEmpty(roomData.name) ? roomData.code : roomData.name;
                        spawned.transform.Find("TextContainer").Find("Room").GetComponent<TextMeshProUGUI>().text = topText;
                        string subtext = mapData.name + (string.IsNullOrEmpty(roomData.code) ? "" : $" - {roomData.code}");
                        spawned.transform.Find("TextContainer").Find("Map").GetComponent<TextMeshProUGUI>().text = subtext;
                    }
                }
            }
            
            // now we order them
            spawnedObjs = spawnedObjs.OrderBy(obj => obj.name).ToList();
            for (int i = 0; i < spawnedObjs.Count; i++)
            {
                spawnedObjs[i].transform.SetSiblingIndex(i);
            }
            
            // and refresh
            StaticUtils.RefreshLayoutGroupsImmediateAndRecursive(infoParent.gameObject);
        });
    }

    public void SetMapVisibility(MapData mapData, bool visibility)
    {
        if (spawnedFloorCheckboxes.ContainsKey(mapData))
        {
            GameObject obj = spawnedFloorCheckboxes[mapData];
            obj.transform.Find("Image").GetComponent<Image>().sprite = (visibility ? checkboxOnTexture : checkboxOffTexture);
        }
        
        mapData.isEnabled = visibility;
        ModelController._instance.SetMapVisibility(mapData, visibility);
    }

    public void SetOnlyMapVisible(MapData mapData)
    {
        foreach (MapData map in loadedMaps)
        {
            SetMapVisibility(map, mapData.Equals(map));
        }
    }

    private void SpawnMapCheckbox(MapData mapData)
    {
        GameObject spawned = GameObject.Instantiate(checkboxPrefab, spawnedFloorsParent);
        spawnedFloorCheckboxes[mapData] = spawned;
        SetMapVisibility(mapData, true);
        
        spawned.transform.Find("Image").GetComponent<Button>().onClick.AddListener(() =>
        {
            SetMapVisibility(mapData, !mapData.isEnabled);
        });
        spawned.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = mapData.name;
        
        StaticUtils.RefreshLayoutGroupsImmediateAndRecursive(spawnedFloorsParent.gameObject);
    }

    public void RequestFloorBundle()
    {
        FileManager.RequestFile("Choose the bundle's file", FILETYPE.ZIP_AND_JSON_AND_ICS, path =>
        {
            string extension = FileManager.GetExtension(path);
            if (extension.Equals("zip"))
            {
                HandleZipBundle(path);
            }
            else if (extension.Equals("json"))
            {
                HandleJsonBundle(path);
            }
            else if (extension.Equals("ics"))
            {
                CalendarController._instance.HandleCalendarFile(path);
            }
        });
    }

    private void HandleJsonBundle(string path)
    {
        string importedBundle = SaveController._instance.ImportCampusBundle(path);
        LoadJsonCampusData(importedBundle);
    }

    public void LoadCurrentCampusData()
    {
        string[] paths = Directory.GetFiles(SaveController.SAVE_FOLDER_BUNDLES_CAMPUS, "*.json");
        if (paths.Length > 0)
        {
            // clear current campus
            ClearCurrentCampus();
            
            // load new one
            LoadJsonCampusData(paths[0]);
            if (loadedCampus != null && loadedCampus.buildings.Count > 0)
            {
                foreach (BundleFloorInfo floor in loadedCampus.buildings[0].floors)
                {
                    LoadFloorFromId(floor.id);
                }
            }
        }
    }

    private void ClearCurrentCampus()
    {
        // remove data
        loadedCampus = null;
        loadedMaps.Clear();
        
        // remove models
        ModelController._instance.RemoveModels();
        
        // also kill floor checkboxes
        StaticUtils.KillChildren(spawnedFloorsParent);
        
        AppController.RefreshLayouts();
    }

    private void LoadJsonCampusData(string path)
    {
        // read file and convert
        string text = File.ReadAllText(path);
        // TODO we might want to transform MapData rooms and intersections into dictionaries for quicker checkups
        loadedCampus = JsonConvert.DeserializeObject<BundleCampusInfo>(text, settings);
        Debug.Log($"Loaded bundle campus! {loadedCampus.name}");
    }

    private void HandleZipBundle(string path)
    {
        // bring to our folder and extract
        string extractedDirectory = SaveController._instance.ImportFloorBundle(path);
        
        // we can now load the data
        LoadFloorFromPath(extractedDirectory);
    }

    private void LoadFloorFromId(string id)
    {
        string directory = SaveController.SAVE_FOLDER_BUNDLES_FLOORS + Path.DirectorySeparatorChar + id;
        if (!Directory.Exists(directory))
        {
            return;
        }
        
        LoadFloorFromPath(directory);
    }

    private void LoadFloorFromPath(string directory)
    {
        string[] files = Directory.GetFiles(directory);
        foreach (string file in files)
        {
            if (file.EndsWith("map.json"))
            {
                // found our json data
                LoadJsonMapData(file);
            }
            else if (file.EndsWith("mesh.obj"))
            {
                // send it to the model controller
                //ModelController._instance.LoadModel(file);
            }
        }
    }

    private void LoadJsonMapData(string path)
    {
        // read file and convert
        string text = File.ReadAllText(path);
        // TODO we might want to transform MapData rooms and intersections into dictionaries for quicker checkups
        MapData loadedMap = JsonConvert.DeserializeObject<MapData>(text, settings);
        CenterAndScaleMapData(loadedMap);
        loadedMaps.Add(loadedMap);
        Debug.Log($"Loaded map data! {loadedMap.name}");

        // send it to the model controller
        ModelController._instance.LoadFloor(loadedMap);
        
        // spawn button
        SpawnMapCheckbox(loadedMap);
    }

    private void CenterAndScaleMapData(MapData mapData)
    {
        // since the original map points are different in each map, we're gonna use its scale to bring them all to a 1 point : 1 meter scale
        // that way we can calculate distances more easily later on. for now we ignore their GPS coords placement since we're centering them anyway
        Vector2 center = CalculateMapCenter(mapData);
        float scale = mapData.scale;
        
        // set room points
        foreach (RoomData roomData in mapData.rooms)
        {
            List<Vector2> newVectors = new List<Vector2>();
            foreach (Vector2 vector in roomData.points)
            {
                newVectors.Add(CenterAndScaleVector(vector, center, scale));
            }
            roomData.points = newVectors;
        }
        
        // set room and floor intersection points
        foreach (FloorIntersection floorIntersection in mapData.floorIntersections)
        {
            floorIntersection.intersection = CenterAndScaleVector(floorIntersection.intersection, center, scale);
        }
        foreach (RoomIntersection roomIntersection in mapData.intersections)
        {
            roomIntersection.intersection = CenterAndScaleVector(roomIntersection.intersection, center, scale);
        }
    }

    private Vector2 CenterAndScaleVector(Vector2 vector, Vector2 center, float scale)
    {
        return new Vector2((vector.x - center.x) * scale, (vector.y - center.y) * scale);
    }
    
    private Vector2 CalculateMapCenter(MapData mapData) {
        // first we calculate the min and the max corners
        Vector2 minPoint = new Vector2(mapData.rooms[0].points[0].x, mapData.rooms[0].points[0].y);
        Vector2 maxPoint = new Vector2(minPoint.x, minPoint.y);
        foreach (RoomData roomData in mapData.rooms)
        {
            foreach (Vector2 point in roomData.points)
            {
                if (point.x < minPoint.x)
                {
                    minPoint.x = point.x;
                }
                if (point.y < minPoint.y)
                {
                    minPoint.y = point.y;
                }
                if(point.x > maxPoint.x)
                {
                    maxPoint.x = point.x;
                }
                if (point.y > maxPoint.y)
                {
                    maxPoint.y = point.y;
                }
            }
        }

        // from there, we can calculate the center by averaging them
        return new Vector2((minPoint.x + maxPoint.x) / 2, (minPoint.y + maxPoint.y) / 2);
    }

    public void UnselectRoom()
    {
        SelectRoom(null);
    }

    public MapData FindMapDataFromRoom(RoomData roomData)
    {
        return FindMapDataFromRoomId(roomData.id);
    }

    public MapData FindMapDataFromRoomId(string id)
    {
        foreach (MapData mapData in loadedMaps)
        {
            foreach (RoomData data in mapData.rooms)
            {
                if (data.id.Equals(id))
                {
                    return mapData;
                }
            }
        }
        return null;
    }

    public MapData FindMapDataFromFloorIntersection(string id)
    {
        foreach (MapData mapData in loadedMaps)
        {
            foreach (FloorIntersection intersection in mapData.floorIntersections)
            {
                if (intersection.id.Equals(id))
                {
                    return mapData;
                }
            }
        }
        return null;
    }

    public MapData FindMapDataFromId(string id)
    {
        foreach (MapData mapData in loadedMaps)
        {
            if (mapData.id.Equals(id))
            {
                return mapData;
            }
        }
        return null;
    }

    public FloorIntersection FindFloorIntersectionFromId(string id)
    {
        foreach (MapData mapData in loadedMaps)
        {
            foreach (FloorIntersection intersection in mapData.floorIntersections)
            {
                if (intersection.id.Equals(id))
                {
                    return intersection;
                }
            }
        }
        return null;
    }

    public RoomData FindRoom(string id)
    {
        foreach (MapData loadedMap in loadedMaps)
        {
            RoomData room = FindRoom(loadedMap, id);
            if (room != null)
            {
                return room;
            }
        }

        return null;
    }

    public RoomData FindRoom(MapData mapData, string id)
    {
        foreach (RoomData roomData in mapData.rooms)
        {
            if (roomData.id.Equals(id))
            {
                return roomData;
            }
        }

        return null;
    }

    public RoomData FindRoomFromPartialCode(string id)
    {
        // first we try to retrieve the code
        string code = null;
        foreach (string part in id.Split(' '))
        {
            if (Regex.IsMatch(part, @"\d\."))
            {
                code = part;
                break;
            }
        }
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }
        
        // now try to find a room that contains this code
        foreach (MapData mapData in loadedMaps)
        {
            foreach (RoomData roomData in mapData.rooms)
            {
                if (roomData.code.Contains(code))
                {
                    return roomData;
                }
            }
        }
        return null;
    }

    public void SelectRoom(GameObject selectedPart)
    {
        // can't load room if no map data
        if (loadedMaps.Count == 0)
        {
            return;
        }

        // now remove the loaded room and try to find the corresponding one if it's not null
        prevSelectedRoom = selectedRoom;
        selectedRoom = null;
        if (selectedPart != null)
        {
            // the parts from the obj have names corresponding to the room ID, so we can filter by that
            string roomId = selectedPart.name;
            RoomData roomData = FindRoom(roomId);
            selectedRoom = roomData;
        }
        
        // set the data in the textbox
        UpdateSelectedMainMenuText();
        
        // show selected model in main map
        if (selectedPart != null)
        {
            // also dim all models
            ModelController._instance.SetAllModelsAlpha(0.5f);
            ModelController._instance.SelectRoom(selectedPart, Color.red);
        }
        else
        {
            // undim all models and remove highlights
            ModelController._instance.SetAllModelsAlpha(1f);
            ModelController._instance.RemoveAllHighlights();
        }
    }

    private void UpdateSelectedMainMenuText()
    {
        if (selectedRoom == null)
        {
            selectedText.text = "Selected object: <None>";
        }
        else
        {
            // and set the text
            selectedText.text = $"Selected room: {selectedRoom.prettyName}"
                                + $"\nFloor: {FindMapDataFromRoom(selectedRoom).name}"
                                + (string.IsNullOrEmpty(selectedRoom.notes) ? "" : $"\nNotes: {selectedRoom.notes}");
        }
    }

    private void UpdateSelectedRoomInfoText()
    {
        if (roomInfoSelectedRoom == null)
        {
            roomInfoSelectedText.text = "No room selected.";
        }
        else
        {
            roomInfoSelectedText.text = $"Info about the room:\n" + 
                                        $"Code: {roomInfoSelectedRoom.code}\n" +
                                        $"Name: {roomInfoSelectedRoom.name}\n" +
                                        $"Notes: {roomInfoSelectedRoom.notes}\n" +
                                        $"ID: {roomInfoSelectedRoom.id}\n" +
                                        $"Center: {roomInfoSelectedRoom.CalculateCenter().ToString()}\n" +
                                        $"Number of polygon points: {roomInfoSelectedRoom.points.Count}";
        }
    }

    private void UpdateRouteInfoText()
    {
        string text = "Info about the new route:\n";
        text += $"ORIGIN = {selectedRouteOriginMap?.name ?? "No map selected"}, {selectedRouteOriginRoom?.prettyName ?? "no room selected"}\n";
        text += $"TARGET = {selectedRouteTargetMap?.name ?? "No map selected"}, {selectedRouteTargetRoom?.prettyName ?? "no room selected"}\n";
        roomRouteSelectedText.text = text;
    }
}
