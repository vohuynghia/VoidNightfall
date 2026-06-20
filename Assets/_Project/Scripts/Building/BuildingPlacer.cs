using UnityEngine;

/// <summary>
/// Xử lý việc đặt building lên grid.
/// Nhấn B để vào/thoát build mode.
/// Click chuột trái để đặt, chuột phải để hủy.
/// </summary>
public class BuildingPlacer : MonoBehaviour
{
	public static BuildingPlacer Instance { get; private set; }

	[Header("Settings")]
	[SerializeField] private LayerMask _groundLayer;
	[SerializeField] private Material _previewValidMaterial;    // màu xanh = có thể đặt
	[SerializeField] private Material _previewInvalidMaterial;  // màu đỏ = không thể đặt

	private BuildingData _selectedBuilding;
	private GameObject _previewObject;      // object hiển thị preview
	private bool _isInBuildMode = false;
	private Camera _camera;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		_camera = Camera.main;
	}

	private void Update()
	{

		if (!_isInBuildMode) return;

		UpdatePreview();

		// Click trái để đặt
		if (Input.GetMouseButtonDown(0))
			TryPlaceBuilding();

		// Click phải hoặc Escape để hủy
		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
			ExitBuildMode();
	}

	// Gọi từ UI khi người chơi chọn building
	public void SelectBuilding(BuildingData data)
	{
		_selectedBuilding = data;
		EnterBuildMode();
	}

	void EnterBuildMode()
	{
		_isInBuildMode = true;

		// Tạo preview object
		if (_previewObject != null) Destroy(_previewObject);
		_previewObject = Instantiate(_selectedBuilding.Prefab);

		// Tắt collider của preview để không va chạm
		foreach (var col in _previewObject.GetComponentsInChildren<Collider>())
			col.enabled = false;

		// Tắt tất cả script trên preview (Turret, HealthSystem...)
		foreach (var mono in _previewObject.GetComponentsInChildren<MonoBehaviour>())
			mono.enabled = false;

		// Tắt Rigidbody nếu có
		foreach (var rb in _previewObject.GetComponentsInChildren<Rigidbody>())
			rb.isKinematic = true;

		Debug.Log($"[BuildingPlacer] Enter build mode: {_selectedBuilding.BuildingName}");
	}

	void ExitBuildMode()
	{
		_isInBuildMode = false;
		_selectedBuilding = null;
		if (_previewObject != null) Destroy(_previewObject);
		Debug.Log("[BuildingPlacer] Exit build mode");
		// Thông báo toolbar bỏ highlight
		if (BuildingToolbarManager.Instance != null)
			BuildingToolbarManager.Instance.Deselect();
	}

	public void CancelPlacement()
	{
		if (_isInBuildMode) ExitBuildMode();
	}

	void ToggleBuildMode()
	{
		if (_isInBuildMode) ExitBuildMode();
		else EnterBuildMode();
	}

	void UpdatePreview()
	{
		if (_previewObject == null) return;

		Vector3Int gridPos = GetGridPositionFromMouse();
		if (gridPos == Vector3Int.one * -999) return;

		// Di chuyển preview theo vị trí grid
		Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
		_previewObject.transform.position = worldPos + Vector3.up * 0.1f;

		// Đổi màu preview dựa trên có thể đặt không
		bool canPlace = GridManager.Instance.CanPlace(gridPos) &&
						ResourceManager.Instance.HasEnough(
							ResourceType.Carbonium,
							_selectedBuilding.CarboniumCost);

		SetPreviewColor(canPlace);
	}

	void TryPlaceBuilding()
	{
		Vector3Int gridPos = GetGridPositionFromMouse();
		if (gridPos == Vector3Int.one * -999) return;

		if (!GridManager.Instance.CanPlace(gridPos))
		{
			Debug.Log("[BuildingPlacer] Cannot place: cell occupied");
			return;
		}

		// Thử trừ tài nguyên
		if (!ResourceManager.Instance.SpendMultiple(_selectedBuilding.GetCosts()))
		{
			Debug.Log("[BuildingPlacer] Cannot place: not enough resources");
			return;
		}

		// Đặt building thật
		Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
		Instantiate(_selectedBuilding.Prefab, worldPos, Quaternion.identity);
		GridManager.Instance.PlaceOnGrid(gridPos);

		// Publish event
		EventBus.Publish(new BuildingPlacedEvent
		{
			BuildingType = _selectedBuilding.BuildingName,
			GridPosition = gridPos
		});

		Debug.Log($"[BuildingPlacer] Placed {_selectedBuilding.BuildingName} at {gridPos}");
	}

	// Raycast từ chuột xuống mặt đất, trả về tọa độ grid
	Vector3Int GetGridPositionFromMouse()
	{
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, 200f, _groundLayer))
			return GridManager.Instance.WorldToGrid(hit.point);

		return Vector3Int.one * -999; // invalid
	}

	void SetPreviewColor(bool valid)
	{
		if (_previewObject == null) return;
		var material = valid ? _previewValidMaterial : _previewInvalidMaterial;
		if (material == null) return;

		foreach (var renderer in _previewObject.GetComponentsInChildren<Renderer>())
			renderer.material = material;
	}
}