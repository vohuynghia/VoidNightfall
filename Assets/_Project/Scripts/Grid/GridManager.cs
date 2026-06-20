using UnityEngine;

/// <summary>
/// Quản lý toàn bộ hệ thống lưới của game.
/// </summary>
public class GridManager : MonoBehaviour
{
	public static GridManager Instance { get; private set; }

	[Header("Grid Settings")]
	[SerializeField] private int _width = 20;
	[SerializeField] private int _height = 20;
	[SerializeField] private float _cellSize = 2f;

	[Header("Visual")]
	[SerializeField] private GameObject _cellIndicatorPrefab; // ô highlight khi hover

	private GridCell[,] _grid;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	private void Start()
	{
		GenerateGrid();
	}

	void GenerateGrid()
	{
		_grid = new GridCell[_width, _height];

		for (int x = 0; x < _width; x++)
		{
			for (int z = 0; z < _height; z++)
			{
				var gridPos = new Vector3Int(x, 0, z);
				var worldPos = GridToWorld(gridPos);
				_grid[x, z] = new GridCell(gridPos, worldPos);
			}
		}

		Debug.Log($"[GridManager] Grid generated: {_width}x{_height}, cell size: {_cellSize}");
	}

	// Chuyển tọa độ grid → tọa độ world
	public Vector3 GridToWorld(Vector3Int gridPos)
	{
		return new Vector3(gridPos.x * _cellSize, 0, gridPos.z * _cellSize);
	}

	// Chuyển tọa độ world → tọa độ grid
	public Vector3Int WorldToGrid(Vector3 worldPos)
	{
		int x = Mathf.FloorToInt(worldPos.x / _cellSize);
		int z = Mathf.FloorToInt(worldPos.z / _cellSize);
		return new Vector3Int(x, 0, z);
	}

	// Lấy thông tin một ô
	public GridCell GetCell(Vector3Int gridPos)
	{
		if (!IsInBounds(gridPos)) return null;
		return _grid[gridPos.x, gridPos.z];
	}

	// Kiểm tra ô có thể đặt building không
	public bool CanPlace(Vector3Int gridPos)
	{
		var cell = GetCell(gridPos);
		return cell != null && !cell.IsOccupied;
	}

	// Đặt building lên ô
	public bool PlaceOnGrid(Vector3Int gridPos)
	{
		if (!CanPlace(gridPos)) return false;
		_grid[gridPos.x, gridPos.z].SetOccupied(true);
		return true;
	}

	// Xóa building khỏi ô
	public void RemoveFromGrid(Vector3Int gridPos)
	{
		var cell = GetCell(gridPos);
		if (cell != null) cell.SetOccupied(false);
	}

	// Kiểm tra tọa độ có nằm trong grid không
	public bool IsInBounds(Vector3Int gridPos)
	{
		return gridPos.x >= 0 && gridPos.x < _width &&
			   gridPos.z >= 0 && gridPos.z < _height;
	}

	// Vẽ grid trong Scene view để debug
	private void OnDrawGizmos()
	{
		if (_grid == null) return;

		for (int x = 0; x < _width; x++)
		{
			for (int z = 0; z < _height; z++)
			{
				var cell = _grid[x, z];
				Gizmos.color = cell.IsOccupied ? Color.red : Color.green;
				var center = cell.WorldPosition + new Vector3(_cellSize / 2, 0, _cellSize / 2);
				Gizmos.DrawWireCube(center, new Vector3(_cellSize, 0.1f, _cellSize));
			}
		}
	}
}