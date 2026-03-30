using System.Collections.Generic;
using System.Linq;
using SOP_Engine.Core;
using SOP_Engine.UI;
using SOP_Engine.Voice;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SOP_Engine.AR
{
    /// <summary>
    /// World-space "screen paint" tool.
    /// Creates a new LineRenderer stroke for each pointer down, and places points on a plane
    /// a fixed distance in front of the AR camera.
    /// </summary>
    public class ARDrawController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SOPDay1Controller sopController;
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private RectTransform drawInputOverlay;

        [Header("Stroke Prefab")]
        [SerializeField] private GameObject drawingStrokePrefab;
        [SerializeField] private string drawingStrokeTag = "DrawingStroke";

        [Header("Enable Conditions")]
        [SerializeField] private string enableOnTag = "draw";

        [Header("Placement")]
        [Tooltip("Fallback distance (meters) in front of the AR camera if no plane is hit.")]
        [SerializeField] private float distanceFromCamera = 0.5f;

        [Header("Sampling")]
        [Tooltip("Minimum distance (in meters) between points.")]
        [SerializeField] private float minPointDistance = 0.01f;

        private static readonly List<ARRaycastHit> _hits = new();

        private readonly List<Vector3> _activePoints = new();
        private LineRenderer _activeLine;
        private bool _drawingEnabled;
        private bool _pointerDown;

        private void Awake()
        {
            if (sopController == null)
                sopController = FindFirstObjectByType<SOPDay1Controller>();

            if (arCamera == null)
                arCamera = Camera.main;

            if (raycastManager == null)
                raycastManager = FindFirstObjectByType<ARRaycastManager>();

            if (drawInputOverlay == null)
            {
                var overlayGo = GameObject.Find("Canvas/DrawInputOverlay");
                if (overlayGo != null)
                    drawInputOverlay = overlayGo.GetComponent<RectTransform>();
            }

            SetDrawingEnabled(false);
        }

        private void OnEnable()
        {
            if (sopController != null)
                sopController.StepChanged += OnStepChanged;

            VoiceCommandBus.CommandRecognized += OnVoiceCommand;
        }

        private void OnDisable()
        {
            if (sopController != null)
                sopController.StepChanged -= OnStepChanged;

            VoiceCommandBus.CommandRecognized -= OnVoiceCommand;
        }

        private void OnStepChanged(SOPStep step)
        {
            var enabled = step?.Tags != null && step.Tags.Any(t => string.Equals(t, enableOnTag, System.StringComparison.OrdinalIgnoreCase));
            SetDrawingEnabled(enabled);

            if (!enabled)
                Clear();
        }

        private void SetDrawingEnabled(bool enabled)
        {
            _drawingEnabled = enabled;
            _pointerDown = false;

            if (drawInputOverlay != null)
                drawInputOverlay.gameObject.SetActive(enabled);
        }

        private void OnVoiceCommand(string command)
        {
            if (!_drawingEnabled)
                return;

            if (string.IsNullOrWhiteSpace(command))
                return;

            command = command.Trim().ToLowerInvariant();
            if (command == "clear")
                ClearAllStrokes();
        }

        private void Update()
        {
            if (!_drawingEnabled)
                return;

            if (arCamera == null || drawInputOverlay == null || drawingStrokePrefab == null)
                return;

            if (Input.touchSupported)
                HandleTouch();
            else
                HandleMouse();
        }

        private void HandleTouch()
        {
            if (Input.touchCount <= 0)
                return;

            var t = Input.GetTouch(0);

            if (!IsOverDrawOverlay(t.position, t.fingerId))
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    _pointerDown = false;
                return;
            }

            switch (t.phase)
            {
                case TouchPhase.Began:
                    _pointerDown = true;
                    StartStroke(t.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_pointerDown)
                        AddPoint(t.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    _pointerDown = false;
                    EndStroke();
                    break;
            }
        }

        private void HandleMouse()
        {
            var pos = (Vector2)Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                if (!IsOverDrawOverlay(pos, -1))
                    return;

                _pointerDown = true;
                StartStroke(pos);
            }
            else if (Input.GetMouseButton(0))
            {
                if (!_pointerDown)
                    return;

                if (!IsOverDrawOverlay(pos, -1))
                    return;

                AddPoint(pos);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_pointerDown)
                {
                    _pointerDown = false;
                    EndStroke();
                }
            }
        }

        private bool IsOverDrawOverlay(Vector2 screenPosition, int pointerId)
        {
            if (drawInputOverlay == null)
                return false;

            if (EventSystem.current == null)
                return true; // No UI event system to filter against.

            var ped = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                pointerId = pointerId
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            if (results == null || results.Count == 0)
                return false;

            // Only allow drawing when the top-most UI hit is our overlay (or a child of it).
            var top = results[0].gameObject;
            if (top == null)
                return false;

            return top == drawInputOverlay.gameObject || top.transform.IsChildOf(drawInputOverlay);
        }

        private void StartStroke(Vector2 screenPosition)
        {
            _activePoints.Clear();

            var go = Instantiate(drawingStrokePrefab);
            if (!string.IsNullOrWhiteSpace(drawingStrokeTag))
                go.tag = drawingStrokeTag;

            _activeLine = go.GetComponent<LineRenderer>();
            if (_activeLine == null)
            {
                Debug.LogWarning("DrawingStroke prefab has no LineRenderer.");
                Destroy(go);
                return;
            }

            // Insert duplicated start point so the stroke appears immediately.
            var p = ScreenToWorldPoint(screenPosition);
            _activePoints.Add(p);
            _activePoints.Add(p);
            ApplyActivePoints();
        }

        private void AddPoint(Vector2 screenPosition)
        {
            if (_activeLine == null)
                return;

            var p = ScreenToWorldPoint(screenPosition);

            if (_activePoints.Count > 0)
            {
                var last = _activePoints[^1];
                if (Vector3.Distance(last, p) < minPointDistance)
                    return;
            }

            _activePoints.Add(p);
            ApplyActivePoints();
        }

        private void EndStroke()
        {
            _activePoints.Clear();
            _activeLine = null;
        }

        private Vector3 ScreenToWorldPoint(Vector2 screenPosition)
        {
            // Prefer anchoring points to detected planes, so strokes stay fixed in the world.
            if (raycastManager != null && raycastManager.Raycast(screenPosition, _hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds))
                return _hits[0].pose.position;

            // Fallback: fixed distance in front of camera.
            var ray = arCamera.ScreenPointToRay(screenPosition);
            return ray.GetPoint(distanceFromCamera);
        }

        private void ApplyActivePoints()
        {
            if (_activeLine == null)
                return;

            _activeLine.positionCount = _activePoints.Count;
            _activeLine.SetPositions(_activePoints.ToArray());
        }

        public void Clear()
        {
            _activePoints.Clear();
            _activeLine = null;
        }

        public void ClearAllStrokes()
        {
            if (string.IsNullOrWhiteSpace(drawingStrokeTag))
                return;

            var strokes = GameObject.FindGameObjectsWithTag(drawingStrokeTag);
            foreach (var s in strokes)
            {
                if (s != null)
                    Destroy(s);
            }
        }
    }
}
