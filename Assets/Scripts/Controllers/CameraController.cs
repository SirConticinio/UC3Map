using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{

    public static CameraController _instance;
    public Camera orthoCamera;
    public Camera perspectiveCamera;
    public List<RawImage> modelDisplays;
    public Camera separateOrthoCamera;
    public Camera separatePerspectiveCamera;
    public List<RawImage> separateModelOrthoDisplays;
    public List<RawImage> separateModelPerspectiveDisplays;
    public List<RawImage> pathfindingOrthoDisplays;
    
    public Camera pathfindingOrthoCamera;

    private RenderTexture renderTexture;
    private RenderTexture separateOrthoRenderTexture;
    private RenderTexture separatePerspectiveRenderTexture;
    private RenderTexture pathfindingRenderTexture;
    
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    public float pathfindingCameraSpeed = 0.4f;
    public float pathfindingCameraDefaultSize = 10f;
    public float pathfindingCameraSizeMargin = 2f;

    // Start is called before the first frame update
    void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        renderTexture = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32);
        orthoCamera.targetTexture = renderTexture;
        perspectiveCamera.targetTexture = renderTexture;
        modelDisplays.ForEach(display => display.texture = renderTexture);
        
        // separate room
        separateOrthoRenderTexture = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32);
        separatePerspectiveRenderTexture = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32);
        separateOrthoCamera.targetTexture = separateOrthoRenderTexture;
        separatePerspectiveCamera.targetTexture = separatePerspectiveRenderTexture;
        separateModelOrthoDisplays.ForEach(display => display.texture = separateOrthoRenderTexture);
        separateModelPerspectiveDisplays.ForEach(display => display.texture = separatePerspectiveRenderTexture);
        
        // pathfinding follower
        pathfindingRenderTexture = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32);
        pathfindingOrthoCamera.targetTexture = pathfindingRenderTexture;
        pathfindingOrthoDisplays.ForEach(display => display.texture = pathfindingRenderTexture);
    }

    public Camera GetActiveMainCamera()
    {
        return orthoCamera.gameObject.activeSelf ? orthoCamera : perspectiveCamera;
    }

    public void SetCameraOrthogonal()
    {
        perspectiveCamera.gameObject.SetActive(false);
        orthoCamera.gameObject.SetActive(true);
    }

    public void SetCameraPerspective()
    {
        perspectiveCamera.gameObject.SetActive(true);
        orthoCamera.gameObject.SetActive(false);
    }

    public void EnableSeparateCameras(GameObject room)
    {
        // enable cameras
        separatePerspectiveCamera.gameObject.SetActive(true);
        separateOrthoCamera.gameObject.SetActive(true);
        
        // scale properly
        separateOrthoCamera.orthographicSize = GetNewCameraSize(room);
    }

    private float GetNewCameraSize(GameObject room)
    {
        MeshCollider meshCollider = room.GetComponent<MeshCollider>();
        Bounds bounds = meshCollider.bounds;
        return Math.Max((bounds.max.x - bounds.min.x) / 2, (bounds.max.y - bounds.min.y) / 2);
    }

    public void EnablePathfindingCamera(GameObject room)
    {
        // enable cam
        bool wasEnabled = pathfindingOrthoCamera.gameObject.activeSelf;
        pathfindingOrthoCamera.gameObject.SetActive(true);

        // center cam and resize
        Physics.SyncTransforms();
        MeshCollider meshCollider = room.GetComponent<MeshCollider>();
        Vector3 center = meshCollider.bounds.center;
        float newSize = Math.Max(GetNewCameraSize(room) + pathfindingCameraSizeMargin, pathfindingCameraDefaultSize);
        SetPathfindingCameraPosAndSize(new Vector3(center.x, center.y, pathfindingOrthoCamera.transform.position.z), newSize, !wasEnabled);
    }
    
    public void MovePathfindingCameraToOverview()
    {
        Vector3 pos = orthoCamera.transform.position;
        pos.z = pathfindingOrthoCamera.transform.position.z;
        SetPathfindingCameraPosAndSize(pos, orthoCamera.orthographicSize, false);
    }

    private void SetPathfindingCameraPosAndSize(Vector3 pos, float size, bool immediately)
    {
        Transform camTransform = pathfindingOrthoCamera.transform;
        camTransform.DOMove(pos, immediately ? 0 : pathfindingCameraSpeed);
        pathfindingOrthoCamera.DOOrthoSize(size, immediately ? 0 : pathfindingCameraSpeed);
    }

    public void DisableSeparateCameras() {
        separatePerspectiveCamera.gameObject.SetActive(false);
        separateOrthoCamera.gameObject.SetActive(false);
    }
}
