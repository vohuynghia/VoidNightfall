using UnityEngine;

public class GridCell
{
	public Vector3Int GridPosition { get; private set; }
	public Vector3 WorldPosition { get; private set; }
	public bool IsOccupied { get; private set; }
	public bool IsWalkable { get; set; } = true;

	public GameObject PlacedObject { get; private set; }
	public BuildingData PlacedData { get; private set; }

	public GridCell(Vector3Int gridPos, Vector3 worldPos)
	{
		GridPosition = gridPos;
		WorldPosition = worldPos;
		IsOccupied = false;
	}

	public void SetOccupied(bool occupied)
	{
		IsOccupied = occupied;
		IsWalkable = !occupied;

		if (!occupied)
		{
			PlacedObject = null;
			PlacedData = null;
		}
	}

	public void SetPlacedInfo(GameObject obj, BuildingData data)
	{
		PlacedObject = obj;
		PlacedData = data;
	}
}