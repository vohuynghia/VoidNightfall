using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
	public enum EnemyState { Chasing, Attacking, Fleeing }
	public enum TargetStrategy { Nearest, Weakest, LeaderAssigned }

	[Header("Data")]
	[SerializeField] private EnemyData _data;

	[Header("AI Settings")]
	[SerializeField] private TargetStrategy _strategy = TargetStrategy.Nearest;
	[SerializeField] private float _fleeHealthPercent = 0.3f;
	[SerializeField] private float _targetUpdateInterval = 0.5f;

	[Header("Flee Settings")]
	[SerializeField] private float _healPerSecond = 5f;
	[SerializeField] private float _fleeReturnHealth = 0.6f;
	[SerializeField] private float _fleeTimeout = 8f;

	private HealthSystem _health;
	private NavMeshAgent _agent;
	private Animator _animator;

	public EnemyState CurrentState { get; private set; }
	private Transform _target;
	private float _nextTargetUpdate;
	private float _nextAttackTime;
	private float _fleeTimer = 0f;
	private bool _isFleeing = false;

	private static List<EnemyAI> _allEnemies = new List<EnemyAI>();
	public static EnemyAI Leader { get; private set; }
	public Transform AssignedTarget { get; set; }

	private void Awake()
	{
		_health = GetComponent<HealthSystem>();
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponentInChildren<Animator>();
	}

	private void OnEnable() => _allEnemies.Add(this);
	private void OnDisable()
	{
		_allEnemies.Remove(this);
		if (Leader == this) Leader = null;
	}

	private void Start()
	{
		_health.OnDeath.AddListener(OnDeath);
		if (Leader == null) Leader = this;

		_agent.speed = _data != null ? _data.MoveSpeed : 3f;
		_agent.stoppingDistance = _data != null ? _data.AttackRange * 0.8f : 1f;

		FindTarget();
	}

	private void Update()
	{
		if (_health.IsDead) return;

		if (Time.time >= _nextTargetUpdate)
		{
			_nextTargetUpdate = Time.time + _targetUpdateInterval;
			FindTarget();
			UpdateState();
		}

		ExecuteState();
		UpdateAnimation();
	}

	void UpdateState()
	{
		float healthPercent = _health.CurrentHealth / _health.MaxHealth;

		if (healthPercent < _fleeHealthPercent && !_isFleeing)
		{
			_isFleeing = true;
			_fleeTimer = 0f;
			CurrentState = EnemyState.Fleeing;
			return;
		}

		if (_isFleeing)
		{
			_fleeTimer += _targetUpdateInterval;

			if (healthPercent >= _fleeReturnHealth || _fleeTimer >= _fleeTimeout)
			{
				_isFleeing = false;
				_fleeTimer = 0f;
			}

			if (_isFleeing)
			{
				CurrentState = EnemyState.Fleeing;
				return;
			}
		}

		if (_target != null)
		{
			float dist = Vector3.Distance(transform.position, _target.position);
			CurrentState = dist <= _data.AttackRange ? EnemyState.Attacking : EnemyState.Chasing;
		}
	}

	void ExecuteState()
	{
		switch (CurrentState)
		{
			case EnemyState.Chasing:
				_agent.isStopped = false;
				if (_target != null) _agent.SetDestination(_target.position);
				break;
			case EnemyState.Attacking:
				_agent.isStopped = true;
				if (_target != null)
				{
					Vector3 dir = (_target.position - transform.position).normalized;
					dir.y = 0;
					if (dir != Vector3.zero)
						transform.rotation = Quaternion.Slerp(transform.rotation,
							Quaternion.LookRotation(dir), 10f * Time.deltaTime);
				}
				TryAttack();
				break;
			case EnemyState.Fleeing:
				Flee();
				break;
		}
	}

	void Flee()
	{
		_health.Heal(_healPerSecond * Time.deltaTime);

		var player = GameObject.FindWithTag("Player");
		if (player == null) return;

		Vector3 fleeDir = (transform.position - player.transform.position).normalized;
		Vector3 fleeTarget = transform.position + fleeDir * 10f;

		_agent.isStopped = false;
		_agent.speed = _data.MoveSpeed * 1.5f;
		_agent.SetDestination(fleeTarget);
	}

	void TryAttack()
	{
		if (_target == null) return;
		if (Time.time < _nextAttackTime) return;
		_nextAttackTime = Time.time + _data.AttackCooldown;

		if (_data.AttackType == AttackType.Melee)
			MeleeAttack();
		else
			RangedAttack();
	}

	void MeleeAttack()
	{
		if (_target.TryGetComponent<HealthSystem>(out var health))
			health.TakeDamage(_data.AttackDamage);
	}

	void RangedAttack()
	{
		if (_data.ProjectilePrefab == null) return;
		Vector3 dir = (_target.position - transform.position).normalized;
		dir.y = 0;

		GameObject proj = Instantiate(_data.ProjectilePrefab,
			transform.position + Vector3.up, Quaternion.identity);

		if (proj.TryGetComponent<EnemyProjectile>(out var ep))
			ep.Init(dir, _data.ProjectileSpeed, _data.AttackDamage);
	}

	void FindTarget()
	{
		switch (_strategy)
		{
			case TargetStrategy.Nearest:
				_target = FindNearest();
				break;
			case TargetStrategy.Weakest:
				_target = FindWeakest();
				break;
			case TargetStrategy.LeaderAssigned:
				_target = (Leader != null && Leader.AssignedTarget != null)
					? Leader.AssignedTarget : FindNearest();
				break;
		}
	}

	Transform FindNearest()
	{
		Transform nearest = null;
		float minDist = float.MaxValue;

		var player = GameObject.FindWithTag("Player");
		if (player != null)
		{
			float d = Vector3.Distance(transform.position, player.transform.position);
			if (d < minDist) { minDist = d; nearest = player.transform; }
		}

		foreach (var b in GameObject.FindGameObjectsWithTag("Building"))
		{
			float d = Vector3.Distance(transform.position, b.transform.position);
			if (d < minDist) { minDist = d; nearest = b.transform; }
		}

		return nearest;
	}

	Transform FindWeakest()
	{
		Transform weakest = null;
		float minHealth = float.MaxValue;

		foreach (var b in GameObject.FindGameObjectsWithTag("Building"))
		{
			if (b.TryGetComponent<HealthSystem>(out var h) && h.CurrentHealth < minHealth)
			{
				minHealth = h.CurrentHealth;
				weakest = b.transform;
			}
		}

		return weakest ?? GameObject.FindWithTag("Player")?.transform;
	}

	void UpdateAnimation()
	{
		if (_animator == null) return;
		float speed = _agent.velocity.magnitude;
		_animator.SetFloat("Speed", speed);
	}

	void OnDeath()
	{
		ResourceManager.Instance.Add(ResourceType.Carbonium, _data.CarboniumDrop);
		ResourceManager.Instance.Add(ResourceType.Metal, _data.MetalDrop);
		Destroy(gameObject, 0.1f);
	}

	public void Init(EnemyData data)
	{
		_data = data;
		if (_agent != null) _agent.speed = data.MoveSpeed;
	}

	public static void LeaderAssignTarget(Transform target)
	{
		if (Leader != null) Leader.AssignedTarget = target;
	}
}