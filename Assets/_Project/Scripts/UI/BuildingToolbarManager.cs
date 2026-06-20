using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingToolbarManager : MonoBehaviour
{
	public static BuildingToolbarManager Instance { get; private set; }

	[Header("Categories")]
	[SerializeField] private Button _tabAttack;
	[SerializeField] private Button _tabDefense;
	[SerializeField] private Button _tabResource;
	[SerializeField] private Button _tabRepair;

	[Header("Building Panel")]
	[SerializeField] private GameObject _buildingPanel;
	[SerializeField] private GameObject _buildingButtonPrefab;

	[Header("All Buildings")]
	[SerializeField] private List<BuildingData> _allBuildings;

	[Header("Colors")]
	[SerializeField] private Color _selectedTabColor = new Color(0.3f, 0.6f, 1f, 1f);
	[SerializeField] private Color _normalTabColor = new Color(0.2f, 0.2f, 0.2f, 1f);

	private BuildingCategory _currentCategory = BuildingCategory.Attack;
	private Button _selectedTab;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	private void Start()
	{
		// G?n s? ki?n cho t?ng tab
		_tabAttack.onClick.AddListener(() => SelectCategory(BuildingCategory.Attack, _tabAttack));
		_tabDefense.onClick.AddListener(() => SelectCategory(BuildingCategory.Defense, _tabDefense));
		_tabResource.onClick.AddListener(() => SelectCategory(BuildingCategory.Resource, _tabResource));
		_tabRepair.onClick.AddListener(() => SelectCategory(BuildingCategory.Repair, _tabRepair));

		// M?c ??nh ch?n tab Attack
		SelectCategory(BuildingCategory.Attack, _tabAttack);
	}

	public void ToggleToolbar(bool show)
	{
		gameObject.SetActive(show);
		if (!show)
		{
			BuildingPlacer.Instance.CancelPlacement();
		}
	}

	void SelectCategory(BuildingCategory category, Button tab)
	{
		_currentCategory = category;

		// Highlight tab ???c ch?n
		if (_selectedTab != null)
			_selectedTab.GetComponent<Image>().color = _normalTabColor;
		_selectedTab = tab;
		_selectedTab.GetComponent<Image>().color = _selectedTabColor;

		// Hi?n th? buildings c?a category này
		RefreshBuildingPanel();

		// Thoát build mode khi ??i tab
		BuildingPlacer.Instance.CancelPlacement();
	}

	void RefreshBuildingPanel()
	{
		// Xóa các button c?
		foreach (Transform child in _buildingPanel.transform)
			Destroy(child.gameObject);

		// T?o button cho t?ng building thu?c category
		foreach (var data in _allBuildings)
		{
			if (data.Category != _currentCategory) continue;

			GameObject btnObj = Instantiate(_buildingButtonPrefab, _buildingPanel.transform);
			var btn = btnObj.GetComponent<BuildingButton>();
			if (btn) btn.Init(data);
		}
	}

	public void Deselect()
	{
		BuildingPlacer.Instance.CancelPlacement();
	}
}