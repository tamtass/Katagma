using UnityEngine;

// Forces the camera to keep a fixed aspect ratio (16:9 by default) no matter what
// shape the window is. If the window doesn't match, it letterboxes/pillarboxes with
// black bars instead of stretching the game. Put this on the main camera.
public class CameraAspect : MonoBehaviour
{
    public float targetAspectWidth = 16f;    // the "16" in 16:9
    public float targetAspectHeight = 9f;    // the "9" in 16:9

    // Runs once at load. Compares the window's real aspect to the target and shrinks
    // the camera's viewport rect so the visible area always keeps the target ratio.
    void Awake()
    {
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;   // <1 means window is too tall, >1 too wide

        Camera cam = GetComponent<Camera>();

        if (scaleHeight < 1.0f)
        {
            // Window is too tall: add black bars top and bottom (letterbox).
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;   // centre the strip vertically
            cam.rect = rect;
        }
        else
        {
            // Window is too wide: add black bars left and right (pillarbox).
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;    // centre the strip horizontally
            rect.y = 0;
            cam.rect = rect;
        }
    }
}
