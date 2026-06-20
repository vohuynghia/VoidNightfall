using UnityEngine;

/// <summary>
/// X? lý combat c?a player:
/// Chu?t trái gi? = b?n ??n
/// Chu?t ph?i click = chém g?n
/// Nhân v?t luôn xoay v? h??ng chu?t khi combat
/// </summary>
public class PlayerCombat : MonoBehaviour
{
	[Header("Gun - Chu?t trái")]
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private Transform _gunPoint;       // ?i?m b?n ??n
	[SerializeField] private float _fireRate = 0.15f;   // giây gi?a m?i viên
	[SerializeField] private float _bulletSpeed = 20f;
	[SerializeField] private float _bulletDamage = 10f;

	[Header("Melee - Chu?t ph?i")]
	[SerializeField] private float _meleeRange = 2f;
	[SerializeField] private float _meleeDamage = 30f;
	[SerializeField] private float _meleeCooldown = 0.5f;

	private float _nextFireTime;
	private float _nextMeleeTime;
	private Camera _camera;

	private void Awake()
	{
		_camera = Camera.main;
	}

	private void Update()
	{
		RotateTowardsMouse();

		// Gi? chu?t trái ? b?n
		if (Input.GetMouseButton(0) && !IsPointerOverUI())
			TryShoot();

		// Click chu?t ph?i ? chém
		if (Input.GetMouseButtonDown(1) && !IsPointerOverUI())
			TryMelee();
	}

	void RotateTowardsMouse()
	{
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, 200f))
		{
			Vector3 dir = hit.point - transform.position;
			dir.y = 0;
			if (dir != Vector3.zero)
				transform.rotation = Quaternion.LookRotation(dir);
		}
	}

	void TryShoot()
	{
		if (Time.time < _nextFireTime) return;
		if (_projectilePrefab == null || _gunPoint == null) return;

		_nextFireTime = Time.time + _fireRate;

		// H??ng b?n = h??ng nhân v?t ?ang nhìn
		Vector3 direction = transform.forward;
		GameObject bullet = Instantiate(_projectilePrefab, _gunPoint.position, Quaternion.identity);
		bullet.GetComponent<Projectile>().Init(direction, _bulletSpeed, _bulletDamage, "Enemy");
	}

	void TryMelee()
	{
		if (Time.time < _nextMeleeTime) return;
		_nextMeleeTime = Time.time + _meleeCooldown;

		// Phát hi?n enemy trong vòng tròn xung quanh
		Collider[] hits = Physics.OverlapSphere(transform.position, _meleeRange);
		foreach (var hit in hits)
		{
			if (!hit.CompareTag("Enemy")) continue;
			if (hit.TryGetComponent<HealthSystem>(out var health))
			{
				health.TakeDamage(_meleeDamage);
				Debug.Log($"[PlayerCombat] Melee hit: {hit.gameObject.name}");
			}
		}

		Debug.Log("[PlayerCombat] Melee attack!");
	}

	bool IsPointerOverUI()
	{
		return UnityEngine.EventSystems.EventSystem.current != null &&
			   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
	}

	// V? melee range trong Scene view
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, _meleeRange);
	}
}