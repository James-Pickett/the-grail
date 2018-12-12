using UnityEngine;

namespace HexMap
{
    public class HexMapCamera : MonoBehaviour
    {
        private static HexMapCamera instance;

        public HexGrid grid;

        public float moveSpeedMinZoom, moveSpeedMaxZoom;

        private float rotationAngle;

        public float rotationSpeed;

        public float stickMinZoom, stickMaxZoom;

        private Transform swivel, stick;

        public float swivelMinZoom, swivelMaxZoom;

        private float zoom = 1f;

        public static bool Locked
        {
            set { instance.enabled = !value; }
        }

        public static void ValidatePosition()
        {
            instance.AdjustPosition(xDelta: 0f, zDelta: 0f);
        }

        private void Awake()
        {
            swivel = transform.GetChild(index: 0);
            stick = swivel.GetChild(index: 0);
        }

        private void OnEnable()
        {
            instance = this;
            ValidatePosition();
        }

        private void Update()
        {
            var zoomDelta = Input.GetAxis(axisName: "Mouse ScrollWheel");
            if (zoomDelta != 0f)
            {
                AdjustZoom(delta: zoomDelta);
            }

            var rotationDelta = Input.GetAxis(axisName: "Rotation");
            if (rotationDelta != 0f)
            {
                AdjustRotation(delta: rotationDelta);
            }

            var xDelta = Input.GetAxis(axisName: "Horizontal");
            var zDelta = Input.GetAxis(axisName: "Vertical");
            if (xDelta != 0f || zDelta != 0f)
            {
                AdjustPosition(xDelta: xDelta, zDelta: zDelta);
            }
        }

        private void AdjustZoom(float delta)
        {
            zoom = Mathf.Clamp01(value: zoom + delta);

            var distance = Mathf.Lerp(a: stickMinZoom, b: stickMaxZoom, t: zoom);
            stick.localPosition = new Vector3(x: 0f, y: 0f, z: distance);

            var angle = Mathf.Lerp(a: swivelMinZoom, b: swivelMaxZoom, t: zoom);
            swivel.localRotation = Quaternion.Euler(x: angle, y: 0f, z: 0f);
        }

        private void AdjustRotation(float delta)
        {
            rotationAngle += delta * rotationSpeed * Time.deltaTime;
            if (rotationAngle < 0f)
            {
                rotationAngle += 360f;
            }
            else if (rotationAngle >= 360f)
            {
                rotationAngle -= 360f;
            }

            transform.localRotation = Quaternion.Euler(x: 0f, y: rotationAngle, z: 0f);
        }

        private void AdjustPosition(float xDelta, float zDelta)
        {
            var direction =
                transform.localRotation *
                new Vector3(x: xDelta, y: 0f, z: zDelta).normalized;
            var damping = Mathf.Max(a: Mathf.Abs(f: xDelta), b: Mathf.Abs(f: zDelta));
            var distance =
                Mathf.Lerp(a: moveSpeedMinZoom, b: moveSpeedMaxZoom, t: zoom) *
                damping * Time.deltaTime;

            var position = transform.localPosition;
            position += direction * distance;
            transform.localPosition =
                grid.wrapping ? WrapPosition(position: position) : ClampPosition(position: position);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            var xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
            position.x = Mathf.Clamp(value: position.x, min: 0f, max: xMax);

            var zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
            position.z = Mathf.Clamp(value: position.z, min: 0f, max: zMax);

            return position;
        }

        private Vector3 WrapPosition(Vector3 position)
        {
            var width = grid.cellCountX * HexMetrics.innerDiameter;
            while (position.x < 0f)
            {
                position.x += width;
            }

            while (position.x > width)
            {
                position.x -= width;
            }

            var zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
            position.z = Mathf.Clamp(value: position.z, min: 0f, max: zMax);

            grid.CenterMap(xPosition: position.x);
            return position;
        }
    }
}