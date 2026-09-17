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

	private Vector3 _velocity = Vector3.zero;
	private float _zoomLevel = 1f;
	private float _zoomVelocity = 0f;
	private float _targetZoom = 1f;
	private Quaternion _fixedRotation;

	private void Awake()
	{
		// Tính góc quay cố định 1 lần duy nhất, dựa trên hướng offset gốc
		_fixedRotation = Quaternion.LookRotation(-_offset.normalized);
	}

	private void LateUpdate()
	{
		if (_target == null) return;

		HandleZoomInput();
		HandleFollow();
	}

	void HandleFollow()
	{
		Vector3 desiredPos = _target.position + (_offset * _zoomLevel);

		transform.position = Vector3.SmoothDamp(
			transform.position,
			desiredPos,
			ref _velocity,
			_smoothTime
		);

		// Dùng góc quay cố định, KHÔNG tính lại theo vị trí tức thời
		transform.rotation = _fixedRotation;
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
}