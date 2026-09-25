using UnityEngine;

/// <summary>
/// Viên đạn bay thẳng, gây damage khi chạm enemy.
/// Tự động kích hoạt rung camera và sinh hiệu ứng va chạm (nếu có).
/// </summary>
public class Projectile : MonoBehaviour
{
	[Header("Hit Effects (Tùy chọn)")]
	[SerializeField] private GameObject _impactVfxPrefab; // Prefab hạt tia lửa (để trống nếu chưa có)

	private float _speed;
	private float _damage;
	private Vector3 _direction;
	private string _targetTag;

	public void Init(Vector3 direction, float speed, float damage, string targetTag)
	{
		_direction = direction.normalized;
		_speed = speed;
		_damage = damage;
		_targetTag = targetTag;

		// Xoay đầu viên đạn nhìn thẳng theo hướng bay
		if (_direction != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(_direction);

		Destroy(gameObject, 3f);
	}

	private void Update()
	{
		transform.position += _direction * _speed * Time.deltaTime;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag(_targetTag)) return;

		// 1. Gây sát thương (sẽ kích hoạt EnemyHitFlash thông qua OnDamaged trong HealthSystem)
		if (other.TryGetComponent<HealthSystem>(out var health))
		{
			health.TakeDamage(_damage);
		}

		// 2. Rung Camera nhẹ khi đạn trúng đích
		//if (CameraShake.Instance != null)
		//{
		//	CameraShake.Instance.Shake(0.04f, 0.03f);
		//}

		// 3. Sinh tia lửa va chạm (nếu đã kéo prefab vào Inspector)
		if (_impactVfxPrefab != null)
		{
			GameObject spark = Instantiate(_impactVfxPrefab, transform.position, Quaternion.identity);
			Destroy(spark, 0.5f);
		}

		// 4. Hủy viên đạn
		Destroy(gameObject);
	}
}