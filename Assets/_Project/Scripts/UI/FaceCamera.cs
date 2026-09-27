using UnityEngine;

public class FaceCamera : MonoBehaviour
{
	private Camera _cam;

	private void Awake()
	{
		_cam = Camera.main;
	}

	private void LateUpdate()
	{
		if (_cam != null)
		{
			transform.rotation = _cam.transform.rotation;
		}
	}
}