using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Dummiesman;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ModelController : MonoBehaviour
{
    public static ModelController _instance;
    public Material defaultMaterial;
    public GameObject spawnReference;
    public Transform spawnSeparateOrthoReference;
    public Transform spawnSeparatePerspectiveReference;
    public float rotationDuration = 4f;
    public Ease rotationEase = Ease.OutQuad;
    
    private Tween rotatingTween;
    private Vector3 spawnedReferencePosition;
    private Dictionary<MapData, GameObject> spawnedFloors = new Dictionary<MapData, GameObject>();
    private Dictionary<string,HighlightElement> highlightedElements = new Dictionary<string, HighlightElement>();
    [HideInInspector] public GameObject currentOrthoSeparateRoom;
    [HideInInspector] public GameObject currentPerspectiveSeparateRoom;

    private void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        spawnedReferencePosition = spawnReference.transform.position;
    }

    public void RemoveModels()
    {
        SetTweenRotation(false, spawnReference.transform);
        RemoveAllHighlights();
        foreach (GameObject obj in spawnedFloors.Values)
        {
            Destroy(obj);
        }
        spawnedFloors.Clear();
    }
    
    public void ToggleRotation()
    {
        SetGeneralRotation(rotatingTween == null);
    }

    public void SetGeneralRotation(bool isRotating)
    {
        SetTweenRotation(isRotating, spawnReference.transform);
    }

    private void SetTweenRotation(bool rotationEnabled, Transform spawnTransform)
    {
        spawnTransform.localRotation = Quaternion.Euler(Vector3.zero);
        if (rotatingTween != null)
        {
            rotatingTween.Kill();
            rotatingTween = null;
        }
        if (rotationEnabled)
        {
            rotatingTween = spawnTransform.DOLocalRotate(new Vector3(0, 0, 360), rotationDuration, RotateMode.LocalAxisAdd)
                .SetEase(rotationEase)
                .SetLoops(-1);
        }
    }

    public void SetMapVisibility(MapData mapData, bool visibility)
    {
        if (spawnedFloors.ContainsKey(mapData))
        {
            spawnedFloors[mapData].SetActive(visibility);
        }
        
        ReorderFloors();
    }

    public void LoadFloor(MapData mapData)
    {
        string modelPath = SaveController.SAVE_FOLDER_BUNDLES_FLOORS
                           + Path.DirectorySeparatorChar + mapData.id
                           + Path.DirectorySeparatorChar + "mesh.obj";
        if (!File.Exists(modelPath))
        {
            Debug.LogError("The model isn't available in the path!");
            return;
        }
        
        // Load map model
        LoadModel(mapData, modelPath);

        // And reorder the floors
        ReorderFloors();
    }

    private void ReorderFloors()
    {
        // position floors consider the lowest altitude as z offset = 0
        float lowestAltitude = GetCurrentLowestAltitude();
        foreach (MapData mapData in spawnedFloors.Keys)
        {
            if (!mapData.isEnabled)
            {
                continue;
            }
            float newAltitude = mapData.altitude - lowestAltitude;
            Transform spawned = spawnedFloors[mapData].transform;
            Vector3 spawnedPos = spawned.localPosition;
            spawned.localPosition = new Vector3(spawnedPos.x, spawnedPos.y, -newAltitude);
        }
    }

    private float GetCurrentLowestAltitude()
    {
        float minAltitude = float.MaxValue;
        foreach (MapData mapData in spawnedFloors.Keys)
        {
            if (mapData.altitude < minAltitude)
            {
                minAltitude = mapData.altitude;
            }
        }
        return minAltitude;
    }

    private void LoadModel(MapData mapData, string path)
    {
        Debug.Log($"Loading obj from {path}!");
        GameObject spawnedModel = new OBJLoader().Load(path);
        spawnedFloors[mapData] = spawnedModel;
        spawnedModel.name = mapData.id;
        spawnedModel.transform.SetParent(spawnReference.transform);
        spawnedModel.transform.localPosition = Vector3.zero;
        // we also need to invert it.. for some reason
        spawnedModel.transform.localScale = new Vector3(1, -1, 1);

        foreach (MeshRenderer rend in spawnedModel.transform.GetComponentsInChildren<MeshRenderer>())
        {
            // Add mesh collider (we need it to be able to click the proper model part)
            GameObject meshGameObject = rend.gameObject;
            MeshCollider meshCollider = meshGameObject.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            meshCollider.sharedMesh = rend.transform.GetComponent<MeshFilter>().sharedMesh;

            // Also add random material color
            rend.material.CopyPropertiesFromMaterial(defaultMaterial);
            rend.material.color = Random.ColorHSV(0f, 1f, 0.2f, 0.5f, 0.8f, 1f);
        }
    }

    public HighlightElement HighlightElement(GameObject selectedPart, Color color)
    {
        // remove previous selection
        if (highlightedElements.ContainsKey(selectedPart.name))
        {
            RemoveHighlight(highlightedElements[selectedPart.name]);
        }
        
        // spawn highlight
        GameObject highlight = Instantiate(selectedPart, selectedPart.transform.parent, true);
        highlight.SetActive(true);
        selectedPart.SetActive(false);
        
        // mark as red
        Renderer highlightRenderer = highlight.GetComponent<Renderer>();
        if (highlightRenderer != null)
        {
            Debug.Log("Painting " + highlight.name + " with color " + color);
            Material material = highlightRenderer.material;
            material.color = color;
            StaticUtils.SetMaterialTransparent(material, true);
        }
        
        // save
        HighlightElement element = new HighlightElement(selectedPart, highlight);
        highlightedElements[selectedPart.name] = element;
        return element;
    }

    public GameObject FindSpawnedRoom(MapData mapData, RoomData roomData, bool onlyEnabled = false, bool allowClones = false)
    {
        // if no map data is provided, we need to find it
        if (mapData == null)
        {
            mapData = BundleController._instance.FindMapDataFromRoom(roomData);
        }
        
        // first we need to find the object with the ID
        GameObject spawnedModel = spawnedFloors[mapData];
        for (int i = 0; i < spawnedModel.transform.childCount; i++)
        {
            Transform child = spawnedModel.transform.GetChild(i);
            bool equalsName = child.gameObject.name.Equals(roomData.id) ||
                              (allowClones && child.gameObject.name.Equals(roomData.id + "(Clone)"));
            if (equalsName && (!onlyEnabled || child.gameObject.activeSelf))
            {
                return child.gameObject;
            }
        }
        Debug.Log("Couldn't find related GameObject! ID = " + roomData.id);
        return null;
    }
    
    public void HighlightRoom(MapData mapData, RoomData roomData, Color color)
    {
        GameObject selectedPart = FindSpawnedRoom(mapData, roomData);
        if (selectedPart == null)
        {
            return;
        }
        
        HighlightElement(selectedPart, color);
    }

    public void SelectRoom(GameObject selectedPart, Color color)
    {
        // load single selection
        RemoveAllHighlights();
        
        // add highlight data
        HighlightElement(selectedPart, color);
    }
    
    public void RemoveAllHighlights()
    {
        foreach (HighlightElement element in highlightedElements.Values)
        {
            RemoveHighlight(element);
        }
        highlightedElements.Clear();
    }

    private void RemoveHighlight(HighlightElement element)
    {
        if (element.highlight != null)
        {
            Destroy(element.highlight);
        }
        if (element.original != null)
        {
            element.original.SetActive(true);
        }
    }

    public void HideNonHighlightedRooms()
    {
        List<GameObject> list = FindNonHighlightedRooms();
        foreach (GameObject model in list)
        {
            model.SetActive(false);
        }
    }

    public void DimNonHighlightedRooms()
    {
        List<GameObject> list = FindNonHighlightedRooms();
        foreach (GameObject model in list)
        {
            SetModelAlpha(model, 0.5f);
        }
    }

    public void SetAllModelsAlpha(float alpha)
    {
        foreach (GameObject model in spawnedFloors.Values)
        {
            for (int i = 0; i < model.transform.childCount; i++)
            {
                Transform child = model.transform.GetChild(i);
                SetModelAlpha(child.gameObject, alpha);
            }
        }
    }

    private void SetModelAlpha(GameObject model, float alpha)
    {
        Renderer highlightRenderer = model.GetComponent<Renderer>();
        if (highlightRenderer != null)
        {
            Material material = highlightRenderer.material;
            Color newColor = material.color;
            newColor.a = alpha;
            material.color = newColor;
            StaticUtils.SetMaterialTransparent(material, alpha < 1f);
        }
    }

    private List<GameObject> FindNonHighlightedRooms()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (GameObject spawnedModel in spawnedFloors.Values)
        {
            for (int i = 0; i < spawnedModel.transform.childCount; i++)
            {
                Transform child = spawnedModel.transform.GetChild(i);
                bool isHighlightElement = false;
                foreach (HighlightElement element in highlightedElements.Values)
                {
                    if (element.original == child.gameObject || element.highlight == child.gameObject)
                    {
                        isHighlightElement = true;
                        break;
                    }
                }
                if (!isHighlightElement)
                {
                    list.Add(child.gameObject);
                }
            }
        }
        return list;
    }

    public void ShowSeparateRoom(MapData mapData, RoomData roomData)
    {
        if (currentOrthoSeparateRoom != null)
        {
            Destroy(currentOrthoSeparateRoom);
            Destroy(currentPerspectiveSeparateRoom);
        }
        SetTweenRotation(false, spawnSeparatePerspectiveReference);
        
        // we have to load it in a separate world and center the camera to it
        GameObject room = FindSpawnedRoom(mapData, roomData);
        currentOrthoSeparateRoom = SpawnSeparateRoom(room, spawnSeparateOrthoReference);
        currentPerspectiveSeparateRoom = SpawnSeparateRoom(room, spawnSeparatePerspectiveReference);

        // now we center the camera to it
        CameraController._instance.EnableSeparateCameras(currentOrthoSeparateRoom);
        
        // rotate perspective model automatically
        SetTweenRotation(true, spawnSeparatePerspectiveReference);
        
        // TODO find a way to kill all this when returning to another menu. rn i'm just doing stuff quick.
    }

    private GameObject SpawnSeparateRoom(GameObject room, Transform spawnParent)
    {
        GameObject spawned = Instantiate(room, spawnParent, false);
        spawned.SetActive(true);
        spawned.transform.localScale = new Vector3(1, -1, 1);
        Physics.SyncTransforms();
        Vector3 distance = spawnParent.position - spawned.GetComponent<MeshCollider>().bounds.center;
        distance.z = 0;
        spawned.transform.localPosition = distance;
        SetModelAlpha(spawned, 1f);
        return spawned;
    }
}

public struct HighlightElement
{
    public GameObject original;
    public GameObject highlight;

    public HighlightElement(GameObject original, GameObject highlight)
    {
        this.original = original;
        this.highlight = highlight;
    }
}