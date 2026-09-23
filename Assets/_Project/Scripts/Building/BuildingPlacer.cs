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
	[SerializeField, Range(0f, 1f)] private float _sellRefundPercent = 0.5f;
	[SerializeField] private Color _moveHighlightColor = new Color(0.2f, 0.6f, 1f); // xanh dương
	[SerializeField] private Color _sellHighlightColor = Color.red;

	private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
	private static readonly int LegacyColorID = Shader.PropertyToID("_Color");

	private BuildingData _selectedBuilding;
	private GameObject _previewObject;
	private PlacementMode _mode = PlacementMode.None;
	private Camera _camera;

	private PlacedBuildingInfo _movingBuildingInfo;
	private bool _isHoldingMovedBuilding = false;

	private PlacedBuildingInfo _hoveredBuilding;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		_camera = Camera.main;
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
		ClearHoverHighlight();
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
		_isHoldingMovedBuilding = false;
		ClearPreview();
		ClearHoverHighlight();
		Debug.Log("[BuildingPlacer] Enter move mode — hover a building then click to pick it up");
	}

	void UpdateMoving()
	{
		bool pointerOverUI = IsPointerOverUI();

		if (!_isHoldingMovedBuilding)
		{
			UpdateHoverHighlight(_moveHighlightColor);
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

		ClearHoverHighlight(); // bỏ highlight trước khi destroy

		_movingBuildingInfo = info;
		_selectedBuilding = info.Data;
		_isHoldingMovedBuilding = true;

		GridManager.Instance.RemoveFromGrid(info.GridPosition);
		Destroy(info.gameObject);

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
		_isHoldingMovedBuilding = false;
		_selectedBuilding = null;
		ClearPreview();
	}

	// ---------- SELLING ----------

	public void EnterSellMode()
	{
		_mode = PlacementMode.Selling;
		ClearPreview();
		ClearHoverHighlight();
		Debug.Log("[BuildingPlacer] Enter sell mode — hover a building then click to sell it");
	}

	void UpdateSelling()
	{
		UpdateHoverHighlight(_sellHighlightColor);
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

		ClearHoverHighlight(); // bỏ highlight trước khi destroy

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

	// ---------- Hover Highlight ----------

	void UpdateHoverHighlight(Color highlightColor)
	{
		var info = RaycastForBuilding();

		if (info != _hoveredBuilding)
		{
			ClearHoverHighlight();
			_hoveredBuilding = info;
		}

		if (_hoveredBuilding != null)
			ApplyHighlight(_hoveredBuilding, highlightColor);
	}

	void ApplyHighlight(PlacedBuildingInfo info, Color color)
	{
		foreach (var renderer in info.GetComponentsInChildren<Renderer>())
		{
			int materialCount = renderer.sharedMaterials.Length;
			for (int i = 0; i < materialCount; i++)
			{
				var block = new MaterialPropertyBlock();
				renderer.GetPropertyBlock(block, i);

				var mat = renderer.sharedMaterials[i];
				if (mat != null && mat.HasProperty(BaseColorID))
					block.SetColor(BaseColorID, color);
				else if (mat != null && mat.HasProperty(LegacyColorID))
					block.SetColor(LegacyColorID, color);

				renderer.SetPropertyBlock(block, i);
			}
		}
	}

	void ClearHoverHighlight()
	{
		if (_hoveredBuilding == null) return;

		foreach (var renderer in _hoveredBuilding.GetComponentsInChildren<Renderer>())
		{
			int materialCount = renderer.sharedMaterials.Length;
			for (int i = 0; i < materialCount; i++)
				renderer.SetPropertyBlock(null, i);
		}

		_hoveredBuilding = null;
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

	void ExitMode()
	{
		_mode = PlacementMode.None;
		_selectedBuilding = null;
		_movingBuildingInfo = null;
		_isHoldingMovedBuilding = false;
		ClearPreview();
		ClearHoverHighlight();

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