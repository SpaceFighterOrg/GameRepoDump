using UnityEngine;

public class MapController : ControllerBase
{
    [Header("Zoom")]
    [SerializeField] private float zoomMax = 8f;
    [SerializeField] private float zoomMin = 1f;

    [Header("Pan")]
    [SerializeField] private float damping = 8f;   // higher = snappier stop
    [SerializeField] private float maxVelocity = 50f;  // world units/sec cap on momentum
    [SerializeField] private float maxDeltaPerFrame = 20f; // max pan distance per frame

    [Header("Map Bounds (world space)")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-500f, -500f);
    [SerializeField] private Vector2 boundsMax = new Vector2(500f, 500f);

    private Camera _camera;
    private Plane _dragPlane;

    private Vector3 _dragStartWorld;
    private bool _isDragging;
    private Vector3 _velocity;

    private Vector2 _lastTouchPos;
    private bool _firstDragFrame;

    private void Start()
    {
        _camera = Camera.main;
        _dragPlane = new Plane(Vector3.forward, Vector3.zero);
    }

    private void Update()
    {
        HandleDrag();
        HandleZoom();
        ApplyDamping();
        ClampToBounds();
    }

    private void HandleDrag()
    {
#if UNITY_EDITOR
        HandleDragMouse();
#else
        HandleDragTouch();
#endif
    }

#if UNITY_EDITOR
    private void HandleDragMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (IsPointerOverUIToolkit(mousePos))
            {
                _isDragging = false;
                return;
            }
            _isDragging = true;
            _firstDragFrame = true;
            _velocity = Vector3.zero;
            _lastTouchPos = mousePos;
            _dragStartWorld = ScreenToWorld(mousePos);
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            _velocity = Vector3.ClampMagnitude(_velocity, maxVelocity);
            return;
        }

        if (!_isDragging || !Input.GetMouseButton(0)) return;

        Vector2 currentMousePos = Input.mousePosition;

        if (_firstDragFrame)
        {
            _firstDragFrame = false;
            _dragStartWorld = ScreenToWorld(currentMousePos);
            _lastTouchPos = currentMousePos;
            return;
        }

        Vector3 currentWorld = ScreenToWorld(currentMousePos);
        Vector3 delta = _dragStartWorld - currentWorld;
        delta = Vector3.ClampMagnitude(delta, maxDeltaPerFrame);

        _camera.transform.position += delta;

        if (Time.deltaTime > 0f)
            _velocity = Vector3.ClampMagnitude(delta / Time.deltaTime, maxVelocity);

        _dragStartWorld = ScreenToWorld(currentMousePos);
        _lastTouchPos = currentMousePos;
    }
#endif

    private void HandleDragTouch()
    {
        if (Input.touchCount != 1)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _velocity = Vector3.ClampMagnitude(_velocity, maxVelocity);
            }
            return;
        }

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began && IsPointerOverUIToolkit(touch.position))
        {
            _isDragging = false;
            return;
        }

        if (touch.phase == TouchPhase.Began)
        {
            _isDragging = true;
            _firstDragFrame = true;
            _velocity = Vector3.zero;
            _lastTouchPos = touch.position;

            _dragStartWorld = ScreenToWorld(touch.position);
            return;
        }

        if (!_isDragging) return;

        if (_firstDragFrame)
        {
            _firstDragFrame = false;
            _dragStartWorld = ScreenToWorld(touch.position);
            _lastTouchPos = touch.position;
            return;
        }

        if (touch.phase == TouchPhase.Moved)
        {
            Vector3 currentWorld = ScreenToWorld(touch.position);
            Vector3 delta = _dragStartWorld - currentWorld;

            delta = Vector3.ClampMagnitude(delta, maxDeltaPerFrame);

            _camera.transform.position += delta;

            if (Time.deltaTime > 0f)
                _velocity = Vector3.ClampMagnitude(delta / Time.deltaTime, maxVelocity);

            _dragStartWorld = ScreenToWorld(touch.position);
            _lastTouchPos = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            _isDragging = false;
            _velocity = Vector3.ClampMagnitude(_velocity, maxVelocity);
        }
    }

    private void ApplyDamping()
    {
        if (_isDragging) return;
        if (_velocity.sqrMagnitude < 0.01f) { _velocity = Vector3.zero; return; }

        _camera.transform.position += _velocity * Time.deltaTime;
        _velocity = Vector3.Lerp(_velocity, Vector3.zero, damping * Time.deltaTime);
    }

    private void HandleZoom()
    {
#if UNITY_EDITOR
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _isDragging = false;
            _velocity = Vector3.zero;
            Zoom(scroll * 10f, Input.mousePosition);
        }
#else
        if (Input.touchCount != 2) return;

        _isDragging = false;
        _velocity = Vector3.zero;

        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        Vector2 t0Prev = t0.position - t0.deltaPosition;
        Vector2 t1Prev = t1.position - t1.deltaPosition;

        float prevMag = (t0Prev - t1Prev).magnitude;
        float currMag = (t0.position - t1.position).magnitude;
        float diff = currMag - prevMag;

        if (Mathf.Abs(diff) < 0.5f) return;

        Vector2 pinchCenter = (t0.position + t1.position) * 0.5f;
        Zoom(diff, pinchCenter);
#endif
    }

    private void Zoom(float pinchDelta, Vector2 screenCenter)
    {
        float zoomSpeed = 0.01f;
        float oldSize = _camera.orthographicSize;
        float newSize = Mathf.Clamp(oldSize - pinchDelta * zoomSpeed, zoomMin, zoomMax);
        if (Mathf.Approximately(oldSize, newSize)) return;

        Ray ray = _camera.ScreenPointToRay(screenCenter);
        if (_dragPlane.Raycast(ray, out float enter))
        {
            Vector3 pinchWorld = ray.GetPoint(enter);
            float ratio = 1f - newSize / oldSize;
            Vector3 shift = (pinchWorld - _camera.transform.position) * ratio;
            shift = Vector3.ClampMagnitude(shift, maxDeltaPerFrame);
            _camera.transform.position += shift;
        }

        _camera.orthographicSize = newSize;
    }

    private void ClampToBounds()
    {
        Vector3 pos = _camera.transform.position;

        float halfH = _camera.orthographicSize;
        float halfW = halfH * _camera.aspect;

        float clampedX = Mathf.Clamp(pos.x, boundsMin.x + halfW, boundsMax.x - halfW);
        float clampedY = Mathf.Clamp(pos.y, boundsMin.y + halfH, boundsMax.y - halfH);

        if (boundsMax.x - boundsMin.x < halfW * 2f) clampedX = (boundsMin.x + boundsMax.x) * 0.5f;
        if (boundsMax.y - boundsMin.y < halfH * 2f) clampedY = (boundsMin.y + boundsMax.y) * 0.5f;

        _camera.transform.position = new Vector3(clampedX, clampedY, pos.z);

        if (!Mathf.Approximately(pos.x, clampedX)) _velocity.x = 0f;
        if (!Mathf.Approximately(pos.y, clampedY)) _velocity.y = 0f;
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Ray ray = _camera.ScreenPointToRay(screenPos);
        return _dragPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
    }
}