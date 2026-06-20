using UnityEngine;

/// <summary>
/// Dữ liệu của một ô trong lưới.
/// </summary>
public class GridCell
{
	public Vector3Int GridPosition { get; private set; }  // tọa độ ô (x, y, z)
	public Vector3 WorldPosition { get; private set; }    // tọa độ thực tế trong scene
	public bool IsOccupied { get; private set; }          // có building chưa
	public bool IsWalkable { get; set; } = true;          // enemy có đi qua được không

	public GridCell(Vector3Int gridPos, Vector3 worldPos)
	{
		GridPosition = gridPos;
		WorldPosition = worldPos;
		IsOccupied = false;
	}

	public void SetOccupied(bool occupied)
	{
		IsOccupied = occupied;
		IsWalkable = !occupied; // có building thì enemy không đi qua được
	}
}