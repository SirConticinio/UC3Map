using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModelClickHandler : MonoBehaviour, IPointerDownHandler
{
    private RectTransform rectTransform;
    private Rect rect;
    
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // find world corners of the image rect
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        // normalized position compared to the world corners
        float normalizedX = (eventData.position.x - worldCorners[0].x) / (worldCorners[2].x - worldCorners[0].x);
        float normalizedY = (eventData.position.y - worldCorners[0].y) / (worldCorners[2].y - worldCorners[0].y);
        Vector2 normalizedPos = new Vector2(normalizedX, normalizedY);

        // raycast to try to find the 3D model part
        Ray ray = CameraController._instance.GetActiveMainCamera().ViewportPointToRay(normalizedPos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            Debug.Log("Hit object: " + hit.collider.name);
            BundleController._instance.SelectRoom(hit.collider.gameObject);
        }
        else
        {
            Debug.Log("No hit detected");
            BundleController._instance.UnselectRoom();
        }
    }
}