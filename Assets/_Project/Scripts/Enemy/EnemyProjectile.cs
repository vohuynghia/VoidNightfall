using UnityEngine;

/// <summary>
/// ??n c?a enemy — bay th?ng, gây damage khi ch?m Player ho?c Building.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
	private float _speed;
	private float _damage;
	private Vector3 _direction;

	public void Init(Vector3 direction, float speed, float damage)
	{
		_direction = direction.normalized;
		_speed = speed;
		_damage = damage;
		Destroy(gameObject, 4f);
	}

	private void Update()
	{
		transform.position += _direction * _speed * Time.deltaTime;
	}

	private void OnTriggerEnter(Collider other)
	{
		// Ch?m Player ho?c Building thì gây damage
		if (other.CompareTag("Player") || other.CompareTag("Building"))
		{
			if (other.TryGetComponent<HealthSystem>(out var health))
				health.TakeDamage(_damage);

			Destroy(gameObject);
		}

		// B? qua va ch?m v?i enemy khác
		if (other.CompareTag("Enemy")) return;
	}
}