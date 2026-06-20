using UnityEngine;

/// <summary>
/// Turret t? ??ng phát hi?n enemy g?n nh?t trong t?m,
/// xoay v? h??ng ?ó và b?n ??n.
/// </summary>
public class Turret : MonoBehaviour
{
	[SerializeField] private TurretData _data;
	[SerializeField] private Transform _gunPivot;    // ph?n xoay c?a turret
	[SerializeField] private Transform _firePoint;   // ?i?m b?n ??n
	[SerializeField] private GameObject _projectilePrefab;

	private Transform _currentTarget;
	private float _nextFireTime;

	private void Update()
	{
		FindClosestEnemy();

		if (_currentTarget == null) return;

		RotateTowardsTarget();
		TryShoot();
	}

	void FindClosestEnemy()
	{
		// Tìm t?t c? enemy trong t?m
		Collider[] hits = Physics.OverlapSphere(transform.position, _data.DetectionRange);
		float minDist = float.MaxValue;
		_currentTarget = null;

		foreach (var hit in hits)
		{
			if (!hit.CompareTag("Enemy")) continue;

			// B? qua enemy ?ã ch?t
			if (hit.TryGetComponent<HealthSystem>(out var health) && health.IsDead)
				continue;

			float dist = Vector3.Distance(transform.position, hit.transform.position);
			if (dist < minDist)
			{
				minDist = dist;
				_currentTarget = hit.transform;
			}
		}
	}

	void RotateTowardsTarget()
	{
		if (_gunPivot == null) return;

		Vector3 dir = (_currentTarget.position - _gunPivot.position).normalized;
		dir.y = 0;

		if (dir == Vector3.zero) return;

		Quaternion targetRot = Quaternion.LookRotation(dir);
		_gunPivot.rotation = Quaternion.Slerp(
			_gunPivot.rotation,
			targetRot,
			_data.RotateSpeed * Time.deltaTime
		);
	}

	void TryShoot()
	{
		if (Time.time < _nextFireTime) return;
		if (_firePoint == null || _projectilePrefab == null) return;

		_nextFireTime = Time.time + (1f / _data.FireRate);

		Vector3 dir = (_currentTarget.position - _firePoint.position).normalized;
		GameObject bullet = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);

		if (bullet.TryGetComponent<Projectile>(out var proj))
			proj.Init(dir, _data.BulletSpeed, _data.BulletDamage, "Enemy");
	}

	// V? detection range trong Scene view
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, _data != null ? _data.DetectionRange : 15f);
	}
}