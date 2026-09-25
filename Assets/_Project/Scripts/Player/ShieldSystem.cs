using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý giáp/khiên năng lượng cho Player.
/// Khiên tự hồi phục sau một khoảng thời gian không nhận sát thương.
/// </summary>
public class ShieldSystem : MonoBehaviour
{
	[Header("Shield Settings")]
	[SerializeField] private float _maxShield = 100f;
	[SerializeField] private float _rechargeRate = 15f;      // Lượng giáp hồi mỗi giây
	[SerializeField] private float _rechargeDelay = 4f;       // Thời gian chờ (giây) không dính đòn trước khi bắt đầu hồi

	public float CurrentShield { get; private set; } = 100f;
	public float MaxShield => _maxShield;

	public UnityEvent<float, float> OnShieldChanged; // (current, max)

	private float _lastDamageTime;
	private HealthSystem _healthSystem;

	private void Awake()
	{
		CurrentShield = _maxShield;
		_healthSystem = GetComponent<HealthSystem>();
	}

	private void Update()
	{
		// Tự động hồi khiên nếu không bị tấn công trong khoảng rechargeDelay
		if (CurrentShield < _maxShield && Time.time >= _lastDamageTime + _rechargeDelay)
		{
			CurrentShield = Mathf.Min(_maxShield, CurrentShield + _rechargeRate * Time.deltaTime);
			OnShieldChanged?.Invoke(CurrentShield, _maxShield);
		}
	}

	/// <summary>
	/// Giảm sát thương vào giáp trước. Trả về lượng sát thương dư thừa xuyên vào máu.
	/// </summary>
	public float AbsorbDamage(float incomingDamage)
	{
		_lastDamageTime = Time.time;

		if (CurrentShield <= 0)
			return incomingDamage;

		if (CurrentShield >= incomingDamage)
		{
			CurrentShield -= incomingDamage;
			OnShieldChanged?.Invoke(CurrentShield, _maxShield);
			return 0f; // Đã cản hoàn toàn
		}
		else
		{
			float remainingDamage = incomingDamage - CurrentShield;
			CurrentShield = 0f;
			OnShieldChanged?.Invoke(CurrentShield, _maxShield);
			return remainingDamage; // Lượng sát thương tràn vào máu
		}
	}
}