using UnityEngine;

namespace RealmCommander.RTS
{
    public class MobileRTSCameraController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField, Range(10f, 80f)] private float cameraPitch = 75f;

        [Header("Pan")]
        [SerializeField] private float panSensitivity = 0.005f;
        [SerializeField] private float keyboardPanSpeed = 20f;
        [SerializeField] private Vector2 xBounds = new Vector2(-45f, 45f);
        [SerializeField] private Vector2 zBounds = new Vector2(-45f, 45f);

        [Header("Zoom / Height")]
        [SerializeField] private float zoomSensitivity = 0.004f;
        [SerializeField] private float scrollZoomSpeed = 5f;
        [SerializeField] private Vector2 heightBounds = new Vector2(15f, 60f);

        [Header("FOV Zoom")]
        [SerializeField] private float fovMin = 50f;
        [SerializeField] private float fovMax = 75f;

        [Header("Rotation")]
        [SerializeField] private float rotateSensitivity = 0.3f;
        [SerializeField] private float rotateSmoothing = 8f;

        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float smoothTime = 0.06f;

        private Camera cam;
        private float yaw;
        private float targetYaw;
        private Vector3 panVelocity;
        private Vector3 targetPosition;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("[MobileRTSCameraController] This script must be on the same GameObject as a Camera component!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
            targetYaw = yaw;
            targetPosition = transform.position;
            if (targetPosition.y < heightBounds.x || targetPosition.y > heightBounds.y)
                targetPosition.y = 35f;
            ApplyRotation();
            ApplyFov(targetPosition.y);
            Debug.Log($"[Camera] Init pos={targetPosition}, yaw={yaw}, pitch={cameraPitch}");
        }

        private void Update()
        {
            if (cam == null) return;

            HandleDesktopInput();
            HandleMobileInput();

            yaw = Mathf.Lerp(yaw, targetYaw, Time.deltaTime * rotateSmoothing);

            if (enableSmoothing)
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref panVelocity, smoothTime);
            else
                transform.position = targetPosition;

            ApplyRotation();
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

            if (Input.GetKey(KeyCode.Q))
                targetYaw -= rotateSensitivity;
            if (Input.GetKey(KeyCode.E))
                targetYaw += rotateSensitivity;

            delta += Vector3.right * Input.GetAxis("Horizontal") * keyboardPanSpeed;
            delta += Vector3.forward * Input.GetAxis("Vertical") * keyboardPanSpeed;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetPosition.y = Mathf.Clamp(
                    targetPosition.y - scroll * scrollZoomSpeed,
                    heightBounds.x, heightBounds.y);
            }

            if (delta.sqrMagnitude > 0.001f)
            {
                targetPosition += delta * panSensitivity;
            }

            ClampPosition();
        }

        private void OnGUI()
        {
            if (!enabled) return;
            GUI.Label(new Rect(10, Screen.height - 60, 400, 20),
                $"Camera: pos={transform.position:F1} yaw={yaw:F0} pitch={cameraPitch:F0}");
        }

        private void HandleMobileInput()
        {
            if (!MobileRTSInput.TouchControlsActive || Input.touchCount != 2) return;

            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);

            Vector2 averageDelta = (first.deltaPosition + second.deltaPosition) * 0.5f;
            Vector3 pan = (-transform.right * averageDelta.x - FlattenedForward() * averageDelta.y) * panSensitivity * 2f;

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

        private void ApplyRotation()
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
