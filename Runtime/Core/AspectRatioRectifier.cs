using UnityEngine;

namespace SAS.Core
{
    [ExecuteAlways]
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class AspectRatioRectifier : MonoBehaviour
    {
        private Camera _camera;

        public ScreenDimensions targetDimensions = new ScreenDimensions();
        private ScreenDimensions _currentDimension = new ScreenDimensions();

        private float _targetAspectRatio;

        public float targetFOV = 60;
        private float _horizontalTargetFOV = 0;
        private float _cachedFov = 60;
        private float _currentFOV = 60;

        public float targetOrthographicSize = 2;
        private float _cachedOrthographicSize = 2;

        public enum FOVAxis
        {
            Horizontal,
            Vertical
        }

        public FOVAxis fovAxis = FOVAxis.Vertical;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            RectifyCamera();
        }

        // Update is called once per frame
        private void Update()
        {
            if (ShouldUpdate())
                RectifyCamera();
        }

        private bool ShouldUpdate()
        {
            return _currentDimension.width != Screen.width
                   || _currentDimension.height != Screen.height
                   || !Mathf.Approximately(_horizontalTargetFOV, _currentFOV)
                   || !Mathf.Approximately(_cachedOrthographicSize, targetOrthographicSize)
                   || !Mathf.Approximately(_cachedFov, targetFOV);
        }

        private void RectifyCamera()
        {
            // cache values
            _currentDimension.width = Screen.width;
            _currentDimension.height = Screen.height;

            _targetAspectRatio = targetDimensions.width / (float)targetDimensions.height;

            _cachedFov = targetFOV;
            _horizontalTargetFOV = Camera.VerticalToHorizontalFieldOfView(_cachedFov, _targetAspectRatio);
            _cachedOrthographicSize = targetOrthographicSize;

            // if screen aspect is greater than target aspect than it is wider
            var screenAspect = Screen.width / (float)Screen.height;
            if (_camera.orthographic)
            {
                if (_targetAspectRatio < screenAspect)
                    _camera.orthographicSize = _cachedOrthographicSize;
                else
                    _camera.orthographicSize = _cachedOrthographicSize * (_targetAspectRatio / screenAspect);
            }
            else
            {
                if (screenAspect > _targetAspectRatio)
                    _camera.fieldOfView = _cachedFov;
                else
                    _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(_horizontalTargetFOV, screenAspect);
            }
        }
    }

    [System.Serializable]
    public class ScreenDimensions
    {
        public int width;
        public int height;
    }
}