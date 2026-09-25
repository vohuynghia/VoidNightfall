using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý máu. Dùng cho cả Player, Enemy và Công trình.
/// </summary>
public class HealthSystem : MonoBehaviour
{
	[Header("Stats")]
	[SerializeField] private float _maxHealth = 100f;

	public float CurrentHealth { get; private set; }
	public float MaxHealth => _maxHealth;
	public bool IsDead { get; private set; }

	/// <summary>
	/// Trạng thái bất tử (miễn nhiễm sát thương khi Dash hoặc có khiên bảo vệ)
	/// </summary>
	public bool IsInvulnerable { get; set; }

	[Header("Events")]
	public UnityEvent<float, float> OnHealthChanged; // (current, max)
	public UnityEvent<float> OnDamaged;              // Lượng sát thương vừa nhận (dùng để rung lắc/chớp đỏ)
	public UnityEvent OnDeath;
	private ShieldSystem _shield;

	private void Awake()
	{
		CurrentHealth = _maxHealth;
	}

	public void TakeDamage(float amount)
	{
		if (IsDead || IsInvulnerable) return;

		// Trừ qua giáp trước nếu tồn tại ShieldSystem
		if (_shield != null)
		{
			amount = _shield.AbsorbDamage(amount);
			if (amount <= 0) return;
		}

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