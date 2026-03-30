using System.Collections.Generic;
using System.Linq;
using SOP_Engine.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SOP_Engine.UI
{
    /// <summary>
    /// Simple screen-space drawing tool using a LineRenderer.
    /// Intended as a low-math fallback: lets the user circle/mark issues over the camera feed.
    /// </summary>
    public class ScreenSpaceDrawTool : MonoBehaviour
    {
        [Header("SOP Binding")]
        [SerializeField] private SOPDay1Controller sopController;
        [SerializeField] private string enableOnTag = "draw";

        [Header("State")]
        [SerializeField] private bool drawingEnabled;

        [Header("Rendering")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform drawingRect;
        [SerializeField] private LineRenderer line;
        [SerializeField] private Color lineColor = new(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private float lineWidth = 8f;

        [Header("Sampling")]
        [Tooltip("Minimum distance in canvas units between samples.")]
        [SerializeField] private float minPointDistance = 6f;

        private readonly List<Vector3> _points = new();
        private bool _isPointerDown;

        public bool DrawingEnabled
        {
            get => drawingEnabled;
            set
            {
                drawingEnabled = value;
                if (!drawingEnabled)
                    _isPointerDown = false;
            }
        }

        private void Awake()
        {
            if (sopController == null)
                sopController = FindFirstObjectByType<SOPDay1Controller>();

            if (targetCanvas == null)
                targetCanvas = FindFirstObjectByType<Canvas>();

            if (drawingRect == null && targetCanvas != null)
                drawingRect = targetCanvas.GetComponent<RectTransform>();

            if (line == null)
                line = GetComponentInChildren<LineRenderer>();

            if (line == null)
            {
                var go = new GameObject("DrawLine");
                go.transform.SetParent(transform, false);
                line = go.AddComponent<LineRenderer>();
            }

            SetupLineRenderer();
            Clear();
            DrawingEnabled = false;
        }

        private void OnEnable()
        {
            if (sopController != null)
                sopController.StepChanged += OnStepChanged;
        }

        private void OnDisable()
        {
            if (sopController != null)
                sopController.StepChanged -= OnStepChanged;
        }

        private void OnStepChanged(SOPStep step)
        {
            var hasTag = step?.Tags != null && step.Tags.Any(t => string.Equals(t, enableOnTag, System.StringComparison.OrdinalIgnoreCase));
            DrawingEnabled = hasTag;
            if (!DrawingEnabled)
                Clear();
        }

        private void SetupLineRenderer()
        {
            if (line == null)
                return;

            line.useWorldSpace = false;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 6;
            line.numCapVertices = 6;
            line.textureMode = LineTextureMode.Stretch;

            // Use a built-in sprite shader material if none is provided.
            if (line.material == null)
                line.material = new Material(Shader.Find("Sprites/Default"));

            line.startColor = lineColor;
            line.endColor = lineColor;

            // Width is in world/canvas units. With typical canvas scaling this reads as "pixels-ish".
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
        }

        public void Clear()
        {
            _points.Clear();
            if (line != null)
                line.positionCount = 0;
        }

        private void Update()
        {
            if (!drawingEnabled)
                return;

            if (line == null || drawingRect == null)
                return;

            if (Input.touchSupported)
            {
                HandleTouch();
            }
            else
            {
                HandleMouse();
            }
        }

        private void HandleTouch()
        {
            if (Input.touchCount <= 0)
                return;

            var t = Input.GetTouch(0);

            // Avoid drawing when interacting with UI controls.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                return;

            if (t.phase == TouchPhase.Began)
            {
                _isPointerDown = true;
                AddPoint(t.position, startStroke: true);
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                if (_isPointerDown)
                    AddPoint(t.position, startStroke: false);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _isPointerDown = false;
            }
        }

        private void HandleMouse()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0))
            {
                _isPointerDown = true;
                AddPoint(Input.mousePosition, startStroke: true);
            }
            else if (Input.GetMouseButton(0))
            {
                if (_isPointerDown)
                    AddPoint(Input.mousePosition, startStroke: false);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isPointerDown = false;
            }
        }

        private void AddPoint(Vector2 screenPoint, bool startStroke)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    drawingRect,
                    screenPoint,
                    targetCanvas != null ? targetCanvas.worldCamera : null,
                    out var local))
                return;

            var p = new Vector3(local.x, local.y, 0f);

            if (startStroke)
            {
                // New stroke: insert a duplicate point so the line appears immediately.
                _points.Add(p);
                _points.Add(p);
                ApplyPoints();
                return;
            }

            if (_points.Count > 0)
            {
                var last = _points[^1];
                if (Vector3.Distance(last, p) < minPointDistance)
                    return;
            }

            _points.Add(p);
            ApplyPoints();
        }

        private void ApplyPoints()
        {
            if (line == null)
                return;

            line.positionCount = _points.Count;
            line.SetPositions(_points.ToArray());
        }
    }
}
