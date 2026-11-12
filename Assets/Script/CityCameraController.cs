using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Kamera isometrik untuk CityOverWorld. Klik kiri memilih lokasi;
/// klik kanan ditahan untuk pan; scroll untuk zoom.
/// </summary>
public class CityCameraController : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform pivotTarget;
    [SerializeField] private CityWorldUI worldUI;

    [Header("Gerak")]
    [SerializeField] private float yaw = 45f;
    [SerializeField] private float pitch = 42f;
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 24f;
    [SerializeField] private float startDistance = 17f;
    [SerializeField] private float panSpeed = 0.08f;
    [SerializeField] private float zoomStep = 1.4f;
    [SerializeField] private float smoothTime = 0.32f;
    [SerializeField] private float zoomSmooth = 6f;

    private Vector3 pivot;
    private Vector3 pivotVelocity;
    private float distance;
    private float targetDistance;
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
        initialized = true;
        ApplyCamera();
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        HandleInput();

        pivot = Vector3.SmoothDamp(pivot, pivotTarget != null ? pivotTarget.position : pivot,
            ref pivotVelocity, smoothTime);
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

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 move = (-right * delta.x - forward * delta.y) * panSpeed * distance * 0.05f;
            pivotTarget.position += move;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomStep, minDistance, maxDistance);
        }

        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

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
        if (worldUI != null) worldUI.ShowLocation(location);
    }
}
