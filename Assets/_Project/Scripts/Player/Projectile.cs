using UnityEngine;

/// <summary>
/// Viên ??n bay th?ng, gây damage khi ch?m enemy.
/// </summary>
public class Projectile : MonoBehaviour
{
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

		Destroy(gameObject, 3f);
	}

	private void Update()
	{
		transform.position += _direction * _speed * Time.deltaTime;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag(_targetTag)) return;

		// Gây damage
		if (other.TryGetComponent<HealthSystem>(out var health))
			health.TakeDamage(_damage);

		Destroy(gameObject);
	}
}