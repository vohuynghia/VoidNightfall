using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float _moveSpeed = 8f;

	[Header("Dash Settings")]
	[SerializeField] private KeyCode _dashKey = KeyCode.Space;
	[SerializeField] private float _dashSpeed = 22f;         
	[SerializeField] private float _dashDuration = 0.2f;       
	[SerializeField] private float _dashCooldown = 1.0f;      
	[SerializeField] private bool _enableInvulnerability = true; // Bất tử khi lướt (I-Frames)

	private Rigidbody _rb;
	private Animator _animator;
	private HealthSystem _healthSystem;

	private Vector3 _moveDirection;
	private Vector3 _dashDirection;
	private bool _isDashing;
	private float _nextDashTime;

	private int _originalLayer;
	private int _dashingLayer;

	public bool IsDashing => _isDashing;

	private DashGhostTrail _ghostTrail;

	public float DashCooldownDuration => _dashCooldown;
	public float DashCooldownRemaining => Mathf.Max(0, _nextDashTime - Time.time);

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_animator = GetComponentInChildren<Animator>();
		_healthSystem = GetComponent<HealthSystem>();

		_originalLayer = gameObject.layer;
		_dashingLayer = LayerMask.NameToLayer("PlayerDashing");
		_ghostTrail = GetComponent<DashGhostTrail>();
	}

	private void Update()
	{
		float h = Input.GetAxisRaw("Horizontal");
		float v = Input.GetAxisRaw("Vertical");
		_moveDirection = new Vector3(h, 0, v).normalized;

		if (_animator != null)
			_animator.SetFloat("Speed", _moveDirection.magnitude);

		// Kích hoạt lướt
		if (Input.GetKeyDown(_dashKey) && CanDash())
		{
			StartCoroutine(PerformDash());
		}
	}

	private void FixedUpdate()
	{
		if (_isDashing)
		{
			// Trong khi lướt: di chuyển theo hướng dash với tốc độ cao, giữ nguyên vận tốc trục Y
			Vector3 dashVelocity = _dashDirection * _dashSpeed;
			dashVelocity.y = _rb.linearVelocity.y;
			_rb.linearVelocity = dashVelocity;
			return;
		}

		// Di chuyển thông thường
		Vector3 velocity = _moveDirection * _moveSpeed;
		velocity.y = _rb.linearVelocity.y;
		_rb.linearVelocity = velocity;

	}

	private bool CanDash()
	{
		return !_isDashing && Time.time >= _nextDashTime;
	}

	private IEnumerator PerformDash()
	{
		_isDashing = true;
		_nextDashTime = Time.time + _dashCooldown;

		// Bật hiệu ứng vệt mờ
		if (_ghostTrail != null)
			_ghostTrail.StartGhostTrail();

		if (_dashingLayer != -1)
			gameObject.layer = _dashingLayer;

		if (_moveDirection != Vector3.zero)
			_dashDirection = _moveDirection;
		else
		{
			_dashDirection = transform.forward;
			_dashDirection.y = 0;
			_dashDirection.Normalize();
		}

		if (_enableInvulnerability && _healthSystem != null)
			_healthSystem.IsInvulnerable = true;

		if (_animator != null)
			_animator.SetTrigger("Dash");

		yield return new WaitForSeconds(_dashDuration);

		// Tắt hiệu ứng vệt mờ khi hết thời gian lướt
		if (_ghostTrail != null)
			_ghostTrail.StopGhostTrail();

		if (_enableInvulnerability && _healthSystem != null)
			_healthSystem.IsInvulnerable = false;

		gameObject.layer = _originalLayer;
		_isDashing = false;
	}
}