using UnityEngine;

/// <summary>
/// Gắn lên mỗi công trình khi đặt xuống — lưu lại BuildingData gốc
/// và vị trí grid, để tính năng Move/Sell tìm lại được thông tin qua raycast.
/// </summary>
public class PlacedBuildingInfo : MonoBehaviour
{
	[SerializeField] private BuildingData _data;
	[SerializeField] private Vector3Int _gridPosition;

	public BuildingData Data => _data;
	public Vector3Int GridPosition => _gridPosition;

	public void Init(BuildingData data, Vector3Int gridPosition)
	{
		_data = data;
		_gridPosition = gridPosition;
	}
}