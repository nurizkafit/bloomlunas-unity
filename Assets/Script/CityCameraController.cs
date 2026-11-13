using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Kamera orbit untuk CityOverWorld. Kamera berputar mengelilingi satu pivot
/// (CameraTarget), tidak bisa digeser bebas. Klik kiri memilih lokasi;
/// klik kanan ditahan untuk memutar (yaw/pitch, dibatasi); scroll untuk zoom.
/// </summary>
public class CityCameraController : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform pivotTarget;
    [SerializeField] private CityWorldUI worldUI;
    [Tooltip("UI kota yang menampilkan panel tempat setelah gedung diklik.")]
    [SerializeField] private CityHudUI cityHud;

    [Header("Gerak")]
    [SerializeField] private float yaw = 45f;
    [SerializeField] private float pitch = 42f;
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 24f;
    [SerializeField] private float startDistance = 17f;
    [SerializeField] private float zoomStep = 1.4f;
    [SerializeField] private float smoothTime = 0.32f;
    [SerializeField] private float zoomSmooth = 6f;

    [Header("Orbit (pivot)")]
    [Tooltip("Derajat putaran per satuan gerakan mouse saat klik kanan ditahan.")]
    [SerializeField] private float orbitSensitivity = 0.18f;
    [SerializeField] private float yawMin = 20f;
    [SerializeField] private float yawMax = 70f;
    [SerializeField] private float pitchMin = 24f;
    [SerializeField] private float pitchMax = 60f;
    [SerializeField] private float orbitSmooth = 10f;

    private Vector3 pivot;
    private Vector3 pivotVelocity;
    private float distance;
    private float targetDistance;
    private float targetYaw;
    private float targetPitch;
    private bool initialized;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = GetComponent<Camera>();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Start()
    {
        if (pivotTarget != null) pivot = pivotTarget.position;
        else
        {
            GameObject target = GameObject.Find("CameraTarget");
            if (target != null) pivot = target.transform.position;
            else pivot = new Vector3(3.18f, 0f, -10.28f);
        }

        distance = startDistance;
        targetDistance = startDistance;
        targetYaw = Mathf.Clamp(yaw, yawMin, yawMax);
        targetPitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        initialized = true;
        ApplyCamera();
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        HandleInput();

        pivot = Vector3.SmoothDamp(pivot, pivotTarget != null ? pivotTarget.position : pivot,
            ref pivotVelocity, smoothTime);
        float orbitBlend = 1f - Mathf.Exp(-orbitSmooth * Time.deltaTime);
        yaw = Mathf.Lerp(yaw, targetYaw, orbitBlend);
        pitch = Mathf.Lerp(pitch, targetPitch, orbitBlend);
        distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-zoomSmooth * Time.deltaTime));
        ApplyCamera();
    }

    private void ApplyCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
        transform.position = pivot - rotation * Vector3.forward * distance;
    }

    private void HandleInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // Klik kanan ditahan = putar kamera mengelilingi pivot (bukan geser).
        if (mouse.rightButton.isPressed && !overUI)
        {
            Vector2 delta = mouse.delta.ReadValue();
            targetYaw = Mathf.Clamp(targetYaw + delta.x * orbitSensitivity, yawMin, yawMax);
            targetPitch = Mathf.Clamp(targetPitch - delta.y * orbitSensitivity, pitchMin, pitchMax);
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f && !overUI)
        {
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomStep, minDistance, maxDistance);
        }

        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (overUI) return;

        Ray ray = worldCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 300f, ~0, QueryTriggerInteraction.Ignore))
        {
            CityLocation location = hit.collider.GetComponentInParent<CityLocation>();
            if (location != null) FocusOn(location);
        }
    }

    private void FocusOn(CityLocation location)
    {
        if (pivotTarget != null)
        {
            pivotTarget.position = location.FocusPoint;
        }
        else
        {
            pivot = location.FocusPoint;
        }

        targetDistance = Mathf.Clamp(startDistance * 0.65f, minDistance, maxDistance);
        if (cityHud == null) cityHud = FindObjectOfType<CityHudUI>(true);
        if (cityHud != null) cityHud.SelectLocation(location);
        if (worldUI != null) worldUI.ShowLocation(location);
    }

    /// <summary>Dipakai UI/debug untuk mengembalikan kamera ke sudut awal (pivot tetap).</summary>
    public void ResetOrbit()
    {
        targetYaw = Mathf.Clamp(45f, yawMin, yawMax);
        targetPitch = Mathf.Clamp(42f, pitchMin, pitchMax);
    }
}
