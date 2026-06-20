/// <summary>
/// Tất cả các event trong game định nghĩa ở đây.
/// Mỗi event là một struct đơn giản chứa dữ liệu cần thiết.
/// </summary>

// === RESOURCE EVENTS ===
public struct ResourceChangedEvent
{
	public string ResourceType;  // "Wood", "Metal", "Energy"
	public int NewAmount;
	public int Delta;            // thay đổi bao nhiêu (+ hoặc -)
}

// === BUILDING EVENTS ===
public struct BuildingPlacedEvent
{
	public string BuildingType;
	public UnityEngine.Vector3Int GridPosition;
}

public struct BuildingDestroyedEvent
{
	public string BuildingType;
	public UnityEngine.Vector3Int GridPosition;
}

// === WAVE EVENTS ===
public struct WaveStartedEvent
{
	public int WaveNumber;
	public int EnemyCount;
}

public struct WaveCompletedEvent
{
	public int WaveNumber;
}

// === GAME STATE EVENTS ===
public struct GameOverEvent
{
	public bool IsVictory;
}