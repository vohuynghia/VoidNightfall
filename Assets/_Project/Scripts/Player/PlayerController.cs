using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float _moveSpeed = 8f;
	[SerializeField] private float _rotateSpeed = 15f;

	private Rigidbody _rb;
	private Vector3 _moveDirection;
	private Animator _animator;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_animator = GetComponentInChildren<Animator>();
	}

	private void Update()
	{
		float h = Input.GetAxisRaw("Horizontal");
		float v = Input.GetAxisRaw("Vertical");
		_moveDirection = new Vector3(h, 0, v).normalized;
		if (_animator != null)
			_animator.SetFloat("Speed", _moveDirection.magnitude);
	}

	private void FixedUpdate()
	{
		Vector3 velocity = _moveDirection * _moveSpeed;
		velocity.y = _rb.linearVelocity.y;
		_rb.linearVelocity = velocity;

		if (_moveDirection != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				targetRotation,
				_rotateSpeed * Time.fixedDeltaTime
			);
		}
	}
}