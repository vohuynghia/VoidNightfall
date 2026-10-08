using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public enum PlacementMode
{
	None,
	Placing,
	Moving,
	Selling,
	Repairing
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
	[SerializeField] private Color _moveHighlightColor = new Color(0.2f, 0.6f, 1f);
	[SerializeField] private Color _sellHighlightColor = Color.red;

	[Header("Repair System")]
	[SerializeField] private Color _repairHighlightColor = Color.green;
	[SerializeField] private int _repairAreaSize = 5; // Kích thước lưới quét mặc định 5x5
	[SerializeField] private GameObject _repairGridVisualPrefab; // Prefab hiển thị khung lưới xanh trên mặt đất
	[SerializeField] private GameObject _repairDronePrefab;       // Drone Prefab
	[SerializeField] private GameObject _worldHealthBarPrefab;    // Prefab WorldHealthBar
	[SerializeField] private TextMeshProUGUI _repairCostText;     // Text hiển thị chi phí sửa chữa góc dưới trái

	private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
	private static readonly int LegacyColorID = Shader.PropertyToID("_Color");

	private BuildingData _selectedBuilding;
	private GameObject _previewObject;
	private PlacementMode _mode = PlacementMode.None;
	private Camera _camera;

	private PlacedBuildingInfo _movingBuildingInfo;
	private bool _isHoldingMovedBuilding = false;
	private PlacedBuildingInfo _hoveredBuilding;

	// Dữ liệu quét sửa chữa
	private GameObject _repairGridVisualInstance;
	private List<PlacedBuildingInfo> _buildingsInRepairArea = new List<PlacedBuildingInfo>();
	private Dictionary<PlacedBuildingInfo, GameObject> _tempHealthBars = new Dictionary<PlacedBuildingInfo, GameObject>();
	private Dictionary<ResourceType, int> _totalRepairCost = new Dictionary<ResourceType, int>();

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
			case PlacementMode.Repairing: UpdateRepairing(); break;
		}
	}

	// ---------- REPAIR MODE ----------

	public void EnterRepairMode()
	{
		_mode = PlacementMode.Repairing;
		ClearPreview();
		ClearHoverHighlight();

		if (_repairGridVisualPrefab != null && _repairGridVisualInstance == null)
		{
			_repairGridVisualInstance = Instantiate(_repairGridVisualPrefab);
		}
		UpdateRepairGridScale();
		Debug.Log("[BuildingPlacer] Enter Repair Mode — Lăn chuột để thay đổi vùng quét");
	}

	void UpdateRepairing()
	{
		// 1. Lăn con trỏ chuột để thay đổi diện tích lưới (3, 5, 7, 9)
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll > 0.05f && _repairAreaSize < 9)
		{
			_repairAreaSize += 2;
			UpdateRepairGridScale();
		}
		else if (scroll < -0.05f && _repairAreaSize > 3)
		{
			_repairAreaSize -= 2;
			UpdateRepairGridScale();
		}

		// 2. Cập nhật vị trí khung lưới theo con trỏ chuột
		Vector3Int centerGridPos = GetGridPositionFromMouse();
		if (centerGridPos != Vector3Int.one * -999)
		{
			Vector3 worldPos = GridManager.Instance.GridToWorld(centerGridPos);
			if (_repairGridVisualInstance != null)
			{
				_repairGridVisualInstance.SetActive(true);
				_repairGridVisualInstance.transform.position = worldPos + Vector3.up * 0.05f;
			}

			// 3. Quét các công trình trong diện tích lưới
			ScanBuildingsInRepairArea(centerGridPos);
		}
		else
		{
			if (_repairGridVisualInstance != null) _repairGridVisualInstance.SetActive(false);
			ClearRepairAreaVisuals();
		}

		if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
		{
			TryExecuteRepair();
		}

		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
		{
			ExitMode();
		}
	}

	void UpdateRepairGridScale()
	{
		if (_repairGridVisualInstance != null)
		{
			_repairGridVisualInstance.transform.localScale = new Vector3(_repairAreaSize, _repairAreaSize, 1f);
		}
	}

	void ScanBuildingsInRepairArea(Vector3Int center)
	{
		int radius = _repairAreaSize / 2;
		HashSet<PlacedBuildingInfo> currentFound = new HashSet<PlacedBuildingInfo>();
		_totalRepairCost.Clear();

		for (int x = -radius; x <= radius; x++)
		{
			for (int z = -radius; z <= radius; z++)
			{
				Vector3Int checkPos = new Vector3Int(center.x + x, center.y, center.z + z);
				var cell = GridManager.Instance.GetCell(checkPos);
				if (cell != null && cell.PlacedObject != null)
				{
					if (cell.PlacedObject.TryGetComponent<PlacedBuildingInfo>(out var info))
					{
						var health = info.GetComponent<HealthSystem>();
						// Chỉ tác động vào công trình mất máu hoặc đã bị sập
						if (health != null && (health.CurrentHealth < health.MaxHealth || health.IsDead))
						{
							currentFound.Add(info);
						}
					}
				}
			}
		}

		// Dọn dẹp những công trình vừa ra khỏi vùng quét
		List<PlacedBuildingInfo> toRemove = new List<PlacedBuildingInfo>();
		foreach (var b in _buildingsInRepairArea)
		{
			if (!currentFound.Contains(b))
			{
				toRemove.Add(b);
				RemoveHighlight(b);
				if (_tempHealthBars.TryGetValue(b, out var bar))
				{
					Destroy(bar);
					_tempHealthBars.Remove(b);
				}
			}
		}
		foreach (var r in toRemove) _buildingsInRepairArea.Remove(r);

		// Thêm và cập nhật những công trình nằm trong vùng quét
		foreach (var b in currentFound)
		{
			if (!_buildingsInRepairArea.Contains(b))
			{
				_buildingsInRepairArea.Add(b);
				ApplyHighlight(b, _repairHighlightColor);

				if (_worldHealthBarPrefab != null && !_tempHealthBars.ContainsKey(b))
				{
					var bar = Instantiate(_worldHealthBarPrefab, b.transform.position + Vector3.up * 2.5f, Quaternion.identity);
					_tempHealthBars[b] = bar;
				}
			}

			// Cập nhật giá trị thanh máu tạm
			if (_tempHealthBars.TryGetValue(b, out var hpBar))
			{
				var slider = hpBar.GetComponentInChildren<UnityEngine.UI.Slider>();
				var h = b.GetComponent<HealthSystem>();
				if (slider != null && h != null)
					slider.value = h.CurrentHealth / h.MaxHealth;
			}

			// Tính toán chi phí tài nguyên theo tỷ lệ máu mất
			CalculateRepairCostForBuilding(b);
		}

		UpdateRepairCostDisplayUI();
	}

	void CalculateRepairCostForBuilding(PlacedBuildingInfo b)
	{
		var health = b.GetComponent<HealthSystem>();
		if (health == null || b.Data == null) return;

		float missingPercent = 1f - (health.CurrentHealth / health.MaxHealth);
		if (missingPercent <= 0) return;

		foreach (var kvp in b.Data.GetCosts())
		{
			// Tỷ lệ sửa: mất bao nhiêu % máu thì tốn bấy nhiêu % tài nguyên gốc (ít nhất 1 đơn vị)
			int cost = Mathf.Max(1, Mathf.RoundToInt(kvp.Value * missingPercent * 0.6f));
			if (_totalRepairCost.ContainsKey(kvp.Key))
				_totalRepairCost[kvp.Key] += cost;
			else
				_totalRepairCost[kvp.Key] = cost;
		}
	}

	void UpdateRepairCostDisplayUI()
	{
		if (_repairCostText == null) return;

		if (_buildingsInRepairArea.Count == 0 || _totalRepairCost.Count == 0)
		{
			_repairCostText.text = "";
			return;
		}

		string text = "<color=#00FF88>Chi phí sửa chữa:</color>\n";
		foreach (var kvp in _totalRepairCost)
		{
			bool hasEnough = ResourceManager.Instance.HasEnough(kvp.Key, kvp.Value);
			string colorCode = hasEnough ? "#FFFFFF" : "#FF4444";
			text += $"<color={colorCode}>{kvp.Key}: {kvp.Value}</color> ";
		}
		_repairCostText.text = text;
	}

	void TryExecuteRepair()
	{
		if (_buildingsInRepairArea.Count == 0) return;

		// Kiểm tra đủ tài nguyên không
		foreach (var kvp in _totalRepairCost)
		{
			if (!ResourceManager.Instance.HasEnough(kvp.Key, kvp.Value))
			{
				Debug.Log("[BuildingPlacer] Không đủ tài nguyên để sửa chữa!");
				return;
			}
		}

		// Trừ tài nguyên
		ResourceManager.Instance.SpendMultiple(_totalRepairCost);

		// Tìm vị trí lưng người chơi để phóng Drone
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		Vector3 droneSpawnPos = player != null ? player.transform.position + Vector3.up * 1.5f - player.transform.forward * 0.5f : transform.position;

		// Phóng Drone đến từng công trình cần sửa
		foreach (var b in _buildingsInRepairArea)
		{
			RemoveHighlight(b);

			if (_repairDronePrefab != null)
			{
				GameObject droneObj = Instantiate(_repairDronePrefab, droneSpawnPos, Quaternion.identity);
				var drone = droneObj.GetComponent<RepairDrone>();
				drone.Launch(droneSpawnPos, b, _worldHealthBarPrefab);
			}
			else
			{
				// Fallback nếu chưa làm prefab Drone: hồi đầy máu ngay lập tức
				var h = b.GetComponent<HealthSystem>();
				if (h != null) h.Heal(h.MaxHealth);
				if (b.TryGetComponent<BuildingLifecycle>(out var lc) && lc.State == BuildingState.Ruined)
					lc.ReviveFromRuin();
			}
		}

		// Xóa các thanh máu preview và reset danh sách
		foreach (var pair in _tempHealthBars)
		{
			if (pair.Value != null) Destroy(pair.Value);
		}
		_tempHealthBars.Clear();
		_buildingsInRepairArea.Clear();
		_totalRepairCost.Clear();
		UpdateRepairCostDisplayUI();

		Debug.Log("[BuildingPlacer] Đã phóng Drone nano sửa chữa toàn bộ khu vực!");
	}

	void ClearRepairAreaVisuals()
	{
		foreach (var b in _buildingsInRepairArea)
		{
			RemoveHighlight(b);
		}
		_buildingsInRepairArea.Clear();

		foreach (var pair in _tempHealthBars)
		{
			if (pair.Value != null) Destroy(pair.Value);
		}
		_tempHealthBars.Clear();
		_totalRepairCost.Clear();
		UpdateRepairCostDisplayUI();
	}

	// ---------- CÁC HÀM CŨ (PLACING, MOVING, SELLING) ----------

	public void SelectBuilding(BuildingData data)
	{
		_selectedBuilding = data;
		EnterPlacingMode();
	}

	void EnterPlacingMode()
	{
		_mode = PlacementMode.Placing;
		ClearHoverHighlight();
		ClearRepairAreaVisuals();
		SpawnPreview(_selectedBuilding);
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

		if (!GridManager.Instance.CanPlace(gridPos)) return;

		if (!ResourceManager.Instance.SpendMultiple(_selectedBuilding.GetCosts())) return;

		SpawnBuildingAt(gridPos, _selectedBuilding, true);

		EventBus.Publish(new BuildingPlacedEvent
		{
			BuildingType = _selectedBuilding.BuildingName,
			GridPosition = gridPos
		});
	}

	public void EnterMoveMode()
	{
		_mode = PlacementMode.Moving;
		_movingBuildingInfo = null;
		_isHoldingMovedBuilding = false;
		ClearPreview();
		ClearHoverHighlight();
		ClearRepairAreaVisuals();
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

		if (info.TryGetComponent<BuildingLifecycle>(out var lifecycle) && lifecycle.State == BuildingState.Constructing)
			return;

		if (info.Data == null) return;

		ClearHoverHighlight();
		_movingBuildingInfo = info;
		_selectedBuilding = info.Data;
		_isHoldingMovedBuilding = true;

		GridManager.Instance.RemoveFromGrid(info.GridPosition);
		Destroy(info.gameObject);

		SpawnPreview(_selectedBuilding);
	}

	void TryDropMovedBuilding()
	{
		Vector3Int gridPos = GetGridPositionFromMouse();
		if (gridPos == Vector3Int.one * -999) return;

		if (!GridManager.Instance.CanPlace(gridPos)) return;

		SpawnBuildingAt(gridPos, _selectedBuilding, false);

		_movingBuildingInfo = null;
		_isHoldingMovedBuilding = false;
		_selectedBuilding = null;
		ClearPreview();
	}

	public void EnterSellMode()
	{
		_mode = PlacementMode.Selling;
		ClearPreview();
		ClearHoverHighlight();
		ClearRepairAreaVisuals();
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

		if (info.TryGetComponent<BuildingLifecycle>(out var lifecycle) && lifecycle.State == BuildingState.Constructing)
			return;

		if (info.Data == null)
		{
			GridManager.Instance.RemoveFromGrid(info.GridPosition);
			Destroy(info.gameObject);
			ClearHoverHighlight();
			return;
		}

		var data = info.Data;
		var gridPos = info.GridPosition;

		ClearHoverHighlight();
		GridManager.Instance.RemoveFromGrid(gridPos);
		Destroy(info.gameObject);

		foreach (var kvp in data.GetCosts())
		{
			int refund = Mathf.RoundToInt(kvp.Value * _sellRefundPercent);
			if (refund > 0)
				ResourceManager.Instance.Add(kvp.Key, refund);
		}
	}

	void UpdateHoverHighlight(Color highlightColor)
	{
		var info = RaycastForBuilding();

		if (info != null && info.TryGetComponent<BuildingLifecycle>(out var lifecycle) && lifecycle.State == BuildingState.Constructing)
		{
			ClearHoverHighlight();
			return;
		}

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

	void RemoveHighlight(PlacedBuildingInfo info)
	{
		if (info == null) return;
		foreach (var renderer in info.GetComponentsInChildren<Renderer>())
		{
			int materialCount = renderer.sharedMaterials.Length;
			for (int i = 0; i < materialCount; i++)
				renderer.SetPropertyBlock(null, i);
		}
	}

	void ClearHoverHighlight()
	{
		if (_hoveredBuilding == null) return;
		RemoveHighlight(_hoveredBuilding);
		_hoveredBuilding = null;
	}

	void SpawnBuildingAt(Vector3Int gridPos, BuildingData data, bool isNewConstruction = true)
	{
		Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
		GameObject obj = Instantiate(data.Prefab, worldPos, Quaternion.identity);

		if (!obj.TryGetComponent<PlacedBuildingInfo>(out var info))
			info = obj.AddComponent<PlacedBuildingInfo>();
		info.Init(data, gridPos);

		if (!obj.TryGetComponent<BuildingLifecycle>(out var lifecycle))
			lifecycle = obj.AddComponent<BuildingLifecycle>();

		if (isNewConstruction)
			lifecycle.InitLifecycle(data);
		else
			lifecycle.SetActiveInstantly();

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

		foreach (var col in _previewObject.GetComponentsInChildren<Collider>()) col.enabled = false;
		foreach (var mono in _previewObject.GetComponentsInChildren<MonoBehaviour>()) mono.enabled = false;
		foreach (var rb in _previewObject.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
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

		bool hasResources = (_mode == PlacementMode.Moving) || HasEnoughResourcesForSelected();
		bool canPlace = GridManager.Instance.CanPlace(gridPos) && hasResources;

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
		ClearRepairAreaVisuals();

		if (_repairGridVisualInstance != null)
			_repairGridVisualInstance.SetActive(false);

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

	bool HasEnoughResourcesForSelected()
	{
		if (_selectedBuilding == null) return false;
		foreach (var kvp in _selectedBuilding.GetCosts())
		{
			if (!ResourceManager.Instance.HasEnough(kvp.Key, kvp.Value))
				return false;
		}
		return true;
	}
}