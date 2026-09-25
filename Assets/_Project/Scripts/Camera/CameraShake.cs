using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
	public static CameraShake Instance { get; private set; }

	private Vector3 _shakeOffset = Vector3.zero;
	private Coroutine _shakeCoroutine;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	/// <summary>
	/// Kích hoạt rung màn hình nhẹ
	/// duration: thời gian rung (giây)
	/// magnitude: biên độ rung (độ lệch)
	/// </summary>
	public void Shake(float duration = 0.08f, float magnitude = 0.05f)
	{
		if (_shakeCoroutine != null)
			StopCoroutine(_shakeCoroutine);

		_shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
	}

	private IEnumerator DoShake(float duration, float magnitude)
	{
		float elapsed = 0f;

		while (elapsed < duration)
		{
			// Tạo độ lệch ngẫu nhiên nhỏ
			float x = Random.Range(-1f, 1f) * magnitude;
			float y = Random.Range(-1f, 1f) * magnitude;

			_shakeOffset = new Vector3(x, y, 0f);

			elapsed += Time.deltaTime;
			yield return null;
		}

		_shakeOffset = Vector3.zero;
		_shakeCoroutine = null;
	}

	// LateUpdate chạy sau khi script CameraFollow đã tính toán xong vị trí
	private void LateUpdate()
	{
		if (_shakeOffset != Vector3.zero)
		{
			transform.position += _shakeOffset;
		}
	}
}