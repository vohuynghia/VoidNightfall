using UnityEngine;
using UnityEngine.EventSystems;

public enum PlacementMode
{
	None,
	Placing,
	Moving,
	Selling
}

public class BuildingPlacer : MonoBehaviour
{
	public static BuildingPlacer Instance { get; private set; }

	[Header("Settings")]
	[SerializeField] private LayerMask _groundLayer;
	[SerializeField] private Material _previewValidMaterial;
	[SerializeField] private Material _previewInvalidMaterial;

	[Header("Move / Sell")]
	[SerializeField] private GameObject _auraPrefab;
	[SerializeField] private Color _moveAuraColor = Color.red;
	[SerializeField] private Color _sellAuraColor = new Color(1f, 0.5f, 0f);
	[SerializeField, Range(0f, 1f)] private float _sellRefundPercent = 0.5f;

	private BuildingData _selectedBuilding;
	private GameObject _previewObject;
	private PlacementMode _mode = PlacementMode.None;
	private Camera _camera;

	private PlacedBuildingInfo _movingBuildingInfo;

	private GameObject _auraInstance;
	private Renderer _auraRenderer;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		_camera = Camera.main;

		if (_auraPrefab != null)
		{
			_auraInstance = Instantiate(_auraPrefab);
			_auraRenderer = _auraInstance.GetComponentInChildren<Renderer>();
			_auraInstance.SetActive(false);
		}
	}

	private void Update()
	{
		switch (_mode)
		{
			case PlacementMode.Placing: UpdatePlacing(); break;
			case PlacementMode.Moving: UpdateMoving(); break;
			case PlacementMode.Selling: UpdateSelling(); break;
		}
	}

	// ---------- PLACING ----------

	public void SelectBuilding(BuildingData data)
	{
		_selectedBuilding = data;
		EnterPlacingMode();
	}

	void EnterPlacingMode()
	{
		_mode = PlacementMode.Placing;
		SetAuraActive(false);
		SpawnPreview(_selectedBuilding);
		Debug.Log($"[BuildingPlacer] Enter placing mode: {_selectedBuilding.BuildingName}");
	}

	void UpdatePlacing()
	{
		UpdatePreviewPosition();
		bool pointerOverUI = IsPointerOverUI();

		if (Input.GetMouseButtonDown(0) && !pointerOverUI)
			TryPlaceNewBuilding();

		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
			ExitMode();
	}

	void TryPlaceNewBuilding()
	{
		Vector3Int gridPos = GetGridPositionFromMouse();
		if (gridPos == Vector3Int.one * -999) return;

		if (!GridManager.Instance.CanPlace(gridPos))
		{
			Debug.Log("[BuildingPlacer] Cannot place: cell occupied");
			return;
		}

		if (!ResourceManager.Instance.SpendMultiple(_selectedBuilding.GetCosts()))
		{
			Debug.Log("[BuildingPlacer] Cannot place: not enough resources");
			return;
		}

		SpawnBuildingAt(gridPos, _selectedBuilding);

		EventBus.Publish(new BuildingPlacedEvent
		{
			BuildingType = _selectedBuilding.BuildingName,
			GridPosition = gridPos
		});

		Debug.Log($"[BuildingPlacer] Placed {_selectedBuilding.BuildingName} at {gridPos}");
	}

	// ---------- MOVING ----------

	public void EnterMoveMode()
	{
		_mode = PlacementMode.Moving;
		_movingBuildingInfo = null;
		ClearPreview();
		SetAuraActive(true, _moveAuraColor);
		Debug.Log("[BuildingPlacer] Enter move mode — click a building to pick it up");
	}

	void UpdateMoving()
	{
		bool pointerOverUI = IsPointerOverUI();

		if (_movingBuildingInfo == null)
		{
			UpdateAuraOnGround();
			if (Input.GetMouseButtonDown(0) && !pointerOverUI)
				TryPickBuildingToMove();
		}
		else
		{
			UpdatePreviewPosition();
			if (Input.GetMouseButtonDown(0) && !pointerOverUI)
				TryDropMovedBuilding();
		}

		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
			ExitMode();
	}

	void TryPickBuildingToMove()
	{
		var info = RaycastForBuilding();
		if (info == null) return;

		_movingBuildingInfo = info;
		_selectedBuilding = info.Data;

		GridManager.Instance.RemoveFromGrid(info.GridPosition);
		Destroy(info.gameObject);

		SetAuraActive(false);
		SpawnPreview(_selectedBuilding);

		Debug.Log($"[BuildingPlacer] Picked up {_selectedBuilding.BuildingName} to move");
	}

	void TryDropMovedBuilding()
	{
		Vector3Int gridPos = GetGridPositionFromMouse();
		if (gridPos == Vector3Int.one * -999) return;

		if (!GridManager.Instance.CanPlace(gridPos))
		{
			Debug.Log("[BuildingPlacer] Cannot move here: cell occupied");
			return;
		}

		SpawnBuildingAt(gridPos, _selectedBuilding);
		Debug.Log($"[BuildingPlacer] Moved {_selectedBuilding.BuildingName} to {gridPos}");

		_movingBuildingInfo = null;
		_selectedBuilding = null;
		ClearPreview();
		SetAuraActive(true, _moveAuraColor);
	}

	// ---------- SELLING ----------

	public void EnterSellMode()
	{
		_mode = PlacementMode.Selling;
		ClearPreview();
		SetAuraActive(true, _sellAuraColor);
		Debug.Log("[BuildingPlacer] Enter sell mode — click a building to sell it");
	}

	void UpdateSelling()
	{
		UpdateAuraOnGround();
		bool pointerOverUI = IsPointerOverUI();

		if (Input.GetMouseButtonDown(0) && !pointerOverUI)
			TrySellBuilding();

		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
			ExitMode();
	}

	void TrySellBuilding()
	{
		var info = RaycastForBuilding();
		if (info == null) return;

		var data = info.Data;
		var gridPos = info.GridPosition;

		GridManager.Instance.RemoveFromGrid(gridPos);
		Destroy(info.gameObject);

		foreach (var kvp in data.GetCosts())
		{
			int refund = Mathf.RoundToInt(kvp.Value * _sellRefundPercent);
			if (refund > 0)
				ResourceManager.Instance.Add(kvp.Key, refund); // TODO: xác nhận đúng tên hàm cộng resource
		}

		Debug.Log($"[BuildingPlacer] Sold {data.BuildingName} at {gridPos}, refunded {_sellRefundPercent * 100}%");
	}

	// ---------- Dùng chung ----------

	void SpawnBuildingAt(Vector3Int gridPos, BuildingData data)
	{
		Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
		GameObject obj = Instantiate(data.Prefab, worldPos, Quaternion.identity);

		var info = obj.AddComponent<PlacedBuildingInfo>();
		info.Init(data, gridPos);

		GridManager.Instance.PlaceOnGrid(gridPos, obj, data);
		ClearPreview();
	}

	PlacedBuildingInfo RaycastForBuilding()
	{
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, 200f))
			return hit.collider.GetComponentInParent<PlacedBuildingInfo>();
		return null;
	}

	void SpawnPreview(BuildingData data)
	{
		if (_previewObject != null) Destroy(_previewObject);
		_previewObject = Instantiate(data.Prefab);

		foreach (var col in _previewObject.GetComponentsInChildren<Collider>())
			col.enabled = false;
		foreach (var mono in _previewObject.GetComponentsInChildren<MonoBehaviour>())
			mono.enabled = false;
		foreach (var rb in _previewObject.GetComponentsInChildren<Rigidbody>())
			rb.isKinematic = true;
	}

	void ClearPreview()
	{
		if (_previewObject != null) Destroy(_previewObject);
		_previewObject = null;
	}

	void UpdatePreviewPosition()
	{
		if (_previewObject == null) return;

		Vector3Int gridPos = GetGridPositionFromMouse();
		if (gridPos == Vector3Int.one * -999) return;

		Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
		_previewObject.transform.position = worldPos + Vector3.up * 0.1f;

		bool canPlace = GridManager.Instance.CanPlace(gridPos) &&
						(_mode == PlacementMode.Moving ||
						 ResourceManager.Instance.HasEnough(ResourceType.Carbonium, _selectedBuilding.CarboniumCost));

		SetPreviewColor(canPlace);
	}

	void UpdateAuraOnGround()
	{
		if (_auraInstance == null) return;
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, 200f, _groundLayer))
			_auraInstance.transform.position = hit.point + Vector3.up * 0.05f;
	}

	void SetAuraActive(bool active, Color? color = null)
	{
		if (_auraInstance == null) return;
		_auraInstance.SetActive(active);
		if (active && color.HasValue && _auraRenderer != null)
			_auraRenderer.material.color = color.Value;
	}

	void ExitMode()
	{
		_mode = PlacementMode.None;
		_selectedBuilding = null;
		_movingBuildingInfo = null;
		ClearPreview();
		SetAuraActive(false);

		Debug.Log("[BuildingPlacer] Exit mode");
		if (BuildingToolbarManager.Instance != null)
			BuildingToolbarManager.Instance.Deselect();
	}

	public void CancelPlacement()
	{
		if (_mode != PlacementMode.None) ExitMode();
	}

	bool IsPointerOverUI()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
	}

	Vector3Int GetGridPositionFromMouse()
	{
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, 200f, _groundLayer))
			return GridManager.Instance.WorldToGrid(hit.point);

		return Vector3Int.one * -999;
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