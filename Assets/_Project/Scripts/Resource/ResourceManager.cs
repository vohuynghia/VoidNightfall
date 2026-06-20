using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý toàn bộ tài nguyên của người chơi.
/// Các hệ thống khác gọi Add/Spend để thay đổi tài nguyên.
/// </summary>
public class ResourceManager : MonoBehaviour
{
	public static ResourceManager Instance { get; private set; }

	[Header("Starting Resources")]
	[SerializeField] private int _startingCarbonium = 200;
	[SerializeField] private int _startingMetal = 100;
	[SerializeField] private int _startingEnergy = 50;

	private Dictionary<ResourceType, int> _resources = new();

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	private void Start()
	{
		// Khởi tạo tài nguyên ban đầu
		_resources[ResourceType.Carbonium] = _startingCarbonium;
		_resources[ResourceType.Metal] = _startingMetal;
		_resources[ResourceType.Energy] = _startingEnergy;

		// Thông báo UI cập nhật
		foreach (var resource in _resources)
			PublishChange(resource.Key, 0);

		Debug.Log($"[ResourceManager] Initialized: " +
				  $"Carbonium={_startingCarbonium}, " +
				  $"Metal={_startingMetal}, " +
				  $"Energy={_startingEnergy}");
	}

	// Lấy số lượng tài nguyên hiện tại
	public int Get(ResourceType type)
	{
		return _resources.TryGetValue(type, out int amount) ? amount : 0;
	}

	// Thêm tài nguyên
	public void Add(ResourceType type, int amount)
	{
		if (amount <= 0) return;
		_resources[type] = Get(type) + amount;
		PublishChange(type, amount);
		Debug.Log($"[ResourceManager] +{amount} {type} → Total: {_resources[type]}");
	}

	// Tiêu tài nguyên — trả về false nếu không đủ
	public bool Spend(ResourceType type, int amount)
	{
		if (!HasEnough(type, amount))
		{
			Debug.Log($"[ResourceManager] Not enough {type}! Need {amount}, have {Get(type)}");
			return false;
		}
		_resources[type] -= amount;
		PublishChange(type, -amount);
		Debug.Log($"[ResourceManager] -{amount} {type} → Total: {_resources[type]}");
		return true;
	}

	// Kiểm tra có đủ tài nguyên không
	public bool HasEnough(ResourceType type, int amount)
	{
		return Get(type) >= amount;
	}

	// Tiêu nhiều loại tài nguyên cùng lúc — kiểm tra đủ hết rồi mới trừ
	public bool SpendMultiple(Dictionary<ResourceType, int> costs)
	{
		// Kiểm tra đủ tất cả trước
		foreach (var cost in costs)
			if (!HasEnough(cost.Key, cost.Value)) return false;

		// Trừ tất cả
		foreach (var cost in costs)
			Spend(cost.Key, cost.Value);

		return true;
	}

	void PublishChange(ResourceType type, int delta)
	{
		EventBus.Publish(new ResourceChangedEvent
		{
			ResourceType = type.ToString(),
			NewAmount = Get(type),
			Delta = delta
		});
	}
}