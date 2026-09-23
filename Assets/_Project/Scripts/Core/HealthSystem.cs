using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Qu?n lý máu. Dùng cho c? Player, Enemy và Công trình.
/// </summary>
public class HealthSystem : MonoBehaviour
{
	[Header("Stats")]
	[SerializeField] private float _maxHealth = 100f;

	public float CurrentHealth { get; private set; }
	public float MaxHealth => _maxHealth;
	public bool IsDead { get; private set; }

	/// <summary>
	/// Tr?ng thái b?t t? (mi?n nhi?m sát th??ng khi Dash ho?c có khiên b?o v?)
	/// </summary>
	public bool IsInvulnerable { get; set; }

	[Header("Events")]
	public UnityEvent<float, float> OnHealthChanged; // (current, max)
	public UnityEvent<float> OnDamaged;              // L??ng sát th??ng v?a nh?n (dùng ?? rung l?c/ch?p ??)
	public UnityEvent OnDeath;

	private void Awake()
	{
		CurrentHealth = _maxHealth;
	}

	public void TakeDamage(float amount)
	{
		// B? qua n?u ?ã ch?t ho?c ?ang ? tr?ng thái b?t t? (I-Frames)
		if (IsDead || IsInvulnerable) return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
		OnDamaged?.Invoke(amount);

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