using UnityEngine;

namespace RealmCommander.RTS
{
    public class MobileRTSCameraController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField, Range(10f, 90f)] private float cameraPitch = 55f;

        [Header("Pan")]
        [SerializeField] private float panSensitivity = 0.02f;
        [SerializeField] private float keyboardPanSpeed = 12f;
        [SerializeField] private Vector2 xBounds = new Vector2(-45f, 45f);
        [SerializeField] private Vector2 zBounds = new Vector2(-45f, 45f);

        [Header("Zoom / Height")]
        [SerializeField] private float zoomSensitivity = 0.02f;
        [SerializeField] private float scrollZoomSpeed = 3f;
        [SerializeField] private Vector2 heightBounds = new Vector2(10f, 45f);

        [Header("FOV Zoom")]
        [SerializeField] private float fovMin = 50f;
        [SerializeField] private float fovMax = 75f;

        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float smoothTime = 0.15f;

        private Camera cam;
        private float yaw;
        private Vector3 panVelocity;
        private Vector3 targetPosition;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
            targetPosition = transform.position;
            targetPosition.y = Mathf.Max(targetPosition.y, heightBounds.y * 0.6f);
            ApplyPitch();
            ApplyFov(targetPosition.y);
        }

        private void Update()
        {
            ApplyPitch();

            HandleDesktopInput();
            HandleMobileInput();

            if (enableSmoothing)
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref panVelocity, smoothTime);
            else
                transform.position = targetPosition;

            ApplyFov(transform.position.y);
        }

        private void HandleDesktopInput()
        {
            if (MobileRTSInput.TouchControlsActive && Input.touchCount > 0) return;

            Vector3 delta = Vector3.zero;

            if (Input.GetMouseButton(2))
            {
                delta += -transform.right * Input.GetAxis("Mouse X") - FlattenedForward() * Input.GetAxis("Mouse Y");
            }

            delta += Vector3.right * Input.GetAxis("Horizontal") * keyboardPanSpeed * Time.deltaTime;
            delta += Vector3.forward * Input.GetAxis("Vertical") * keyboardPanSpeed * Time.deltaTime;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetPosition.y = Mathf.Clamp(
                    targetPosition.y - scroll * scrollZoomSpeed,
                    heightBounds.x, heightBounds.y);
            }

            if (delta.sqrMagnitude > 0.001f)
            {
                targetPosition += delta * panSensitivity * 100f * Time.deltaTime;
            }

            ClampPosition();
        }

        private void HandleMobileInput()
        {
            if (!MobileRTSInput.TouchControlsActive || Input.touchCount != 2) return;

            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);

            Vector2 averageDelta = (first.deltaPosition + second.deltaPosition) * 0.5f;
            Vector3 pan = (-transform.right * averageDelta.x - FlattenedForward() * averageDelta.y) * panSensitivity;

            targetPosition += pan;

            Vector2 prevPos = first.position - first.deltaPosition;
            Vector2 prevSecond = second.position - second.deltaPosition;
            float prevDist = Vector2.Distance(prevPos, prevSecond);
            float currDist = Vector2.Distance(first.position, second.position);

            targetPosition.y = Mathf.Clamp(
                targetPosition.y - (currDist - prevDist) * zoomSensitivity,
                heightBounds.x, heightBounds.y);

            ClampPosition();
        }

        private void ClampPosition()
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, xBounds.x, xBounds.y);
            targetPosition.z = Mathf.Clamp(targetPosition.z, zBounds.x, zBounds.y);
            targetPosition.y = Mathf.Clamp(targetPosition.y, heightBounds.x, heightBounds.y);
        }

        private void ApplyPitch()
        {
            transform.eulerAngles = new Vector3(cameraPitch, yaw, 0f);
        }

        private void ApplyFov(float height)
        {
            if (cam == null) return;
            float t = Mathf.InverseLerp(heightBounds.x, heightBounds.y, height);
            cam.fieldOfView = Mathf.Lerp(fovMin, fovMax, t);
        }

        private Vector3 FlattenedForward()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }
    }
}
