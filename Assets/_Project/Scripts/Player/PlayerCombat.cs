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
	[SerializeField] private float _fireRate = 0.15f;   // Giây gi?a m?i viên
	[SerializeField] private float _bulletSpeed = 20f;
	[SerializeField] private float _bulletDamage = 10f;

	[Header("Melee - Chu?t ph?i")]
	[SerializeField] private float _meleeRange = 2f;
	[SerializeField] private float _meleeDamage = 30f;
	[SerializeField] private float _meleeCooldown = 0.5f;

	private float _nextFireTime;
	private float _nextMeleeTime;
	private Camera _camera;
	private PlayerController _controller;

	private void Awake()
	{
		_camera = Camera.main;
		_controller = GetComponent<PlayerController>();
	}

	private void Update()
	{
		// Luôn xoay v? h??ng con tr? chu?t trên m?t ??t ph?ng
		RotateTowardsMouse();

		// N?u ?ang l??t thì không cho phép t?n công
		if (_controller != null && _controller.IsDashing)
			return;

		// Gi? chu?t trái -> B?n
		if (Input.GetMouseButton(0) && !IsPointerOverUI())
			TryShoot();

		// Click chu?t ph?i -> Chém
		if (Input.GetMouseButtonDown(1) && !IsPointerOverUI())
			TryMelee();
	}

	void RotateTowardsMouse()
	{
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		// Dùng Plane ph?ng y = transform.position.y ho?c Raycast
		Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

		if (groundPlane.Raycast(ray, out float enter))
		{
			Vector3 hitPoint = ray.GetPoint(enter);
			Vector3 dir = hitPoint - transform.position;
			dir.y = 0;
			if (dir.sqrMagnitude > 0.001f)
				transform.rotation = Quaternion.LookRotation(dir);
		}
		else if (Physics.Raycast(ray, out RaycastHit hit, 200f))
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

		Vector3 direction = transform.forward;
		GameObject bullet = Instantiate(_projectilePrefab, _gunPoint.position, Quaternion.identity);
		bullet.GetComponent<Projectile>().Init(direction, _bulletSpeed, _bulletDamage, "Enemy");
	}

	void TryMelee()
	{
		if (Time.time < _nextMeleeTime) return;
		_nextMeleeTime = Time.time + _meleeCooldown;

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

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, _meleeRange);
	}
}