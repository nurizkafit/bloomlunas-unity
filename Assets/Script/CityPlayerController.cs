using TMPro;
using UnityEngine;

/// <summary>
/// Player sementara berbentuk capsule. Pergerakan memakai target titik di
/// bidang XZ; tidak memakai NavMesh agar mudah diganti nanti.
/// </summary>
public class CityPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float turnSpeed = 9f;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private string displayName = "Player";

    private Camera worldCamera;
    private TextMeshPro nameLabel;
    private Vector3 target;
    private bool hasTarget;

    private void Start()
    {
        worldCamera = Camera.main;
        EnsureCapsule();
        EnsureLabel();
    }

    public bool IsMoving => hasTarget;

    public void MoveTo(Vector3 point)
    {
        point.y = transform.position.y;
        target = point;
        hasTarget = Vector3.Distance(transform.position, target) > 0.05f;
    }

    private void Update()
    {
        if (!hasTarget) return;

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.04f)
        {
            hasTarget = false;
            return;
        }

        Vector3 step = direction.normalized * moveSpeed * Time.deltaTime;
        if (step.magnitude > direction.magnitude) step = direction;
        transform.position += step;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (nameLabel == null) return;

        nameLabel.transform.position = transform.position + Vector3.up * 2.35f;
        if (worldCamera != null)
        {
            nameLabel.transform.rotation = worldCamera.transform.rotation;
        }
    }

    private void EnsureCapsule()
    {
        if (GetComponent<CapsuleCollider>() == null)
        {
            CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.height = 2f;
            collider.radius = 0.45f;
        }

        if (GetComponentInChildren<MeshRenderer>() != null) return;

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Capsule";
        capsule.transform.SetParent(transform, false);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
        capsule.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

        Collider primitiveCollider = capsule.GetComponent<Collider>();
        if (primitiveCollider != null) Destroy(primitiveCollider);

        MeshRenderer renderer = capsule.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.31f, 0.55f, 0.68f, 1f);
        }
    }

    private void EnsureLabel()
    {
        GameObject labelObject = new GameObject("PlayerName",
            typeof(RectTransform), typeof(MeshRenderer), typeof(TextMeshPro));
        nameLabel = labelObject.GetComponent<TextMeshPro>();
        nameLabel.text = displayName;
        nameLabel.fontSize = 3f;
        nameLabel.color = new Color(0.16f, 0.14f, 0.11f, 1f);
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.rectTransform.sizeDelta = new Vector2(5f, 1.5f);
        if (labelFont != null) nameLabel.font = labelFont;
        else if (TMP_Settings.defaultFontAsset != null) nameLabel.font = TMP_Settings.defaultFontAsset;
    }
}
