using System.Collections.Generic;
using SOP_Engine.Core;
using SOP_Engine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SOP_Engine.AR
{
    public class ARAnnotationManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ARRaycastManager raycastManager;

        [Header("Prefab")]
        [SerializeField] private GameObject arrowPrefab;

        [Header("Optional")]
        [SerializeField] private SOPDay1Controller sopController;

        private readonly Queue<SOPAnnotation> _queue = new();
        private static readonly List<ARRaycastHit> _hits = new();

        private void Awake()
        {
            if (raycastManager == null)
                raycastManager = FindFirstObjectByType<ARRaycastManager>();

            if (sopController == null)
                sopController = FindFirstObjectByType<SOPDay1Controller>();

            if (sopController != null)
                sopController.StepChanged += OnStepChanged;
        }

        private void OnDestroy()
        {
            if (sopController != null)
                sopController.StepChanged -= OnStepChanged;
        }

        private void OnStepChanged(SOPStep step)
        {
            _queue.Clear();

            if (step?.Annotations == null) return;
            foreach (var a in step.Annotations)
                _queue.Enqueue(a);
        }

        private void Update()
        {
            if (_queue.Count == 0) return;
            if (raycastManager == null) return;

            if (TryGetTapPosition(out var screenPos))
            {
                if (raycastManager.Raycast(screenPos, _hits, TrackableType.PlaneWithinPolygon))
                {
                    var pose = _hits[0].pose;
                    PlaceNext(pose);
                }
            }
        }

        private void PlaceNext(Pose pose)
        {
            var annotation = _queue.Dequeue();

            var prefab = arrowPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("ARAnnotationManager: arrowPrefab not set.");
                return;
            }

            var go = Instantiate(prefab, pose.position, pose.rotation);

            // Try set label
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null && !string.IsNullOrWhiteSpace(annotation.Label))
                tmp.text = annotation.Label;
        }

        private static bool TryGetTapPosition(out Vector2 screenPos)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPos = touch.position;
                    return true;
                }
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }
#endif

            screenPos = default;
            return false;
        }
    }
}
