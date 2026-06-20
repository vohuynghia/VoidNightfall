using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Qu?n lý máu. Dùng cho c? Player và Enemy.
/// </summary>
public class HealthSystem : MonoBehaviour
{
	[Header("Stats")]
	[SerializeField] private float _maxHealth = 100f;

	public float CurrentHealth { get; private set; }
	public float MaxHealth => _maxHealth;
	public bool IsDead { get; private set; }

	public UnityEvent<float, float> OnHealthChanged; // current, max
	public UnityEvent OnDeath;

	private void Awake()
	{
		CurrentHealth = _maxHealth;
	}

	public void TakeDamage(float amount)
	{
		if (IsDead) return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);

		if (CurrentHealth <= 0)
			Die();
	}

	public void Heal(float amount)
	{
		if (IsDead) return;
		CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
		OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
	}

	void Die()
	{
		IsDead = true;
		OnDeath?.Invoke();
		Debug.Log($"[HealthSystem] {gameObject.name} died!");
	}
}