using System;
using System.Collections.Generic;

/// <summary>
/// Hệ thống giao tiếp giữa các module qua events.
/// Các hệ thống không gọi nhau trực tiếp mà publish/subscribe qua đây.
/// </summary>
public static class EventBus
{
	private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

	// Đăng ký lắng nghe một event
	public static void Subscribe<T>(Action<T> callback)
	{
		var type = typeof(T);
		if (!_subscribers.ContainsKey(type))
			_subscribers[type] = new List<Delegate>();

		_subscribers[type].Add(callback);
	}

	// Hủy đăng ký
	public static void Unsubscribe<T>(Action<T> callback)
	{
		var type = typeof(T);
		if (_subscribers.ContainsKey(type))
			_subscribers[type].Remove(callback);
	}

	// Phát ra event
	public static void Publish<T>(T eventData)
	{
		var type = typeof(T);
		if (!_subscribers.ContainsKey(type)) return;

		foreach (var subscriber in _subscribers[type].ToArray())
			(subscriber as Action<T>)?.Invoke(eventData);
	}

	// Xóa toàn bộ (gọi khi load scene mới)
	public static void ClearAll()
	{
		_subscribers.Clear();
	}
}