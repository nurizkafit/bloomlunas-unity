using UnityEngine;

/// <summary>
/// Satu titik interaksi di CityOverWorld. Komponen ini ditempel ke WalkPoint
/// yang sudah ada; label dan collider dibuat oleh komponen scene lain.
/// </summary>
public class CityLocation : MonoBehaviour
{
    [SerializeField] private string displayName = "Location";
    [SerializeField] private string targetScene = "";
    [SerializeField] private Vector3 labelOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.2f, 0f);

    public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
    public string TargetScene => targetScene;
    public Vector3 LabelAnchor => transform.position + labelOffset;
    public Vector3 FocusPoint => transform.position + focusOffset;

    private void Awake()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(2.2f, 2.8f, 2.2f);
        }
    }
}
