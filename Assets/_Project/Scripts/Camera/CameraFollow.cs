using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	[Header("Target")]
	[SerializeField] private Transform _target;

	[Header("Follow Settings")]
	[SerializeField] private Vector3 _offset = new Vector3(0, 13f, -15f);
	[SerializeField] private float _smoothTime = 0.15f;

	[Header("Zoom")]
	[SerializeField] private float _zoomSpeed = 3f;
	[SerializeField] private float _minZoom = 0.4f;
	[SerializeField] private float _maxZoom = 1.8f;
	[SerializeField] private float _zoomSmoothTime = 0.1f;

	[Header("Rotation")]
	[SerializeField] private float _rotateSpeed = 100f;

	private Vector3 _velocity = Vector3.zero;
	private float _zoomLevel = 1f;
	private float _zoomVelocity = 0f;
	private float _targetZoom = 1f;

	// L?u rotation riêng, không dùng transform.rotation
	private float _yAngle = 0f;

	private void LateUpdate()
	{
		if (_target == null) return;

		HandleZoomInput();
		HandleRotation();
		HandleFollow();
	}

	void HandleFollow()
	{
		// Xoay offset theo _yAngle (do ng??i dùng control)
		Vector3 rotatedOffset = Quaternion.Euler(0, _yAngle, 0) * (_offset * _zoomLevel);
		Vector3 desiredPos = _target.position + rotatedOffset;

		transform.position = Vector3.SmoothDamp(
			transform.position,
			desiredPos,
			ref _velocity,
			_smoothTime
		);

		// LookAt th?ng, không Slerp ?? tránh feedback loop
		transform.LookAt(_target.position);
	}

	void HandleZoomInput()
	{
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (Mathf.Abs(scroll) > 0.01f)
		{
			_targetZoom -= scroll * _zoomSpeed;
			_targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
		}

		_zoomLevel = Mathf.SmoothDamp(_zoomLevel, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
	}

	void HandleRotation()
	{
		if (Input.GetKey(KeyCode.Q))
			_yAngle -= _rotateSpeed * Time.deltaTime;
		if (Input.GetKey(KeyCode.E))
			_yAngle += _rotateSpeed * Time.deltaTime;
	}
}