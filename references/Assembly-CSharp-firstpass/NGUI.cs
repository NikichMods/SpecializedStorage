using UnityEngine;

public class UIWidget : MonoBehaviour
{
    public enum Pivot
    {
        BottomLeft = 6
    }

    public Pivot pivot { get; set; }
    public Color color { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int depth { get; set; }
    public Vector3[] localCorners { get { return null; } }
    public bool autoResizeBoxCollider;
}

public class UI2DSprite : UIWidget
{
    public Sprite sprite2D { get; set; }
}

public static class NGUITools
{
    public static T FindInParents<T>(GameObject go) where T : Component
    {
        return null;
    }
}
