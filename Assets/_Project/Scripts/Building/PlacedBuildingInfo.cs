using UnityEngine;

/// <summary>
/// Gắn tự động lên mỗi công trình khi đặt xuống — lưu lại BuildingData gốc
/// và vị trí grid, để tính năng Move/Sell tìm lại được thông tin qua raycast.
/// </summary>
public class PlacedBuildingInfo : MonoBehaviour
{
	public BuildingData Data { get; private set; }
	public Vector3Int GridPosition { get; private set; }

	public void Init(BuildingData data, Vector3Int gridPosition)
	{
		Data = data;
		GridPosition = gridPosition;
	}
}