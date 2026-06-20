using UnityEngine;

/// <summary>
/// T? ??ng thu th?p tài nguyên theo th?i gian.
/// </summary>
public class ResourceCollector : MonoBehaviour
{
	[SerializeField] private ResourceType _resourceType;
	[SerializeField] private int _amountPerTick = 10;
	[SerializeField] private float _tickInterval = 5f; // giây m?i l?n thu th?p

	private float _nextTickTime;

	private void Update()
	{
		if (Time.time < _nextTickTime) return;
		_nextTickTime = Time.time + _tickInterval;

		ResourceManager.Instance.Add(_resourceType, _amountPerTick);
		Debug.Log($"[ResourceCollector] +{_amountPerTick} {_resourceType}");
	}
}