using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingToolbarManager : MonoBehaviour
{
	public static BuildingToolbarManager Instance { get; private set; }

	[Header("Categories")]
	[SerializeField] private Button _tabMainBase;
	[SerializeField] private Button _tabAttack;
	[SerializeField] private Button _tabDefense;
	[SerializeField] private Button _tabResource;
	[SerializeField] private Button _tabRepair;

	[Header("Repair Actions")]
	[SerializeField] private GameObject _moveButtonObj;
	[SerializeField] private GameObject _sellButtonObj;

	[Header("Building Panel")]
	[SerializeField] private GameObject _buildingPanel;
	[SerializeField] private GameObject _buildingButtonPrefab;

	[Header("All Buildings")]
	[SerializeField] private List<BuildingData> _allBuildings;

	[Header("Colors")]
	[SerializeField] private Color _selectedTabColor = new Color(0.3f, 0.6f, 1f, 1f);
	[SerializeField] private Color _normalTabColor = new Color(0.2f, 0.2f, 0.2f, 1f);

	[Header("Slide Animation")]
	[SerializeField] private RectTransform _panelRect;
	[SerializeField] private float _slideDistance = 250f;
	[SerializeField] private float _slideDuration = 0.25f;
	[SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

	private BuildingCategory _currentCategory = BuildingCategory.Attack;
	private Button _selectedTab;
	private CanvasGroup _canvasGroup;
	private Vector2 _shownPos;
	private Vector2 _hiddenPos;
	private Coroutine _slideRoutine;

	public bool IsOpen { get; private set; } = false;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;

		if (_panelRect == null)
			_panelRect = GetComponent<RectTransform>();

		_canvasGroup = GetComponent<CanvasGroup>();
		if (_canvasGroup == null)
			_canvasGroup = gameObject.AddComponent<CanvasGroup>();

		_shownPos = _panelRect.anchoredPosition;
		_hiddenPos = _shownPos + Vector2.down * _slideDistance;

		_panelRect.anchoredPosition = _hiddenPos;
		SetInteractable(false);
	}

	private void Start()
	{
		_tabMainBase.onClick.AddListener(() => SelectCategory(BuildingCategory.MainBase, _tabMainBase));
		_tabAttack.onClick.AddListener(() => SelectCategory(BuildingCategory.Attack, _tabAttack));
		_tabDefense.onClick.AddListener(() => SelectCategory(BuildingCategory.Defense, _tabDefense));
		_tabResource.onClick.AddListener(() => SelectCategory(BuildingCategory.Resource, _tabResource));
		_tabRepair.onClick.AddListener(() => SelectCategory(BuildingCategory.Repair, _tabRepair));

		if (_moveButtonObj != null)
			_moveButtonObj.GetComponent<Button>().onClick.AddListener(() => BuildingPlacer.Instance.EnterMoveMode());
		if (_sellButtonObj != null)
			_sellButtonObj.GetComponent<Button>().onClick.AddListener(() => BuildingPlacer.Instance.EnterSellMode());

		SelectCategory(BuildingCategory.MainBase, _tabMainBase);
	}

	public void ToggleToolbar(bool show)
	{
		if (IsOpen == show) return;
		IsOpen = show;

		if (!show)
			BuildingPlacer.Instance.CancelPlacement();

		if (_slideRoutine != null) StopCoroutine(_slideRoutine);
		_slideRoutine = StartCoroutine(SlideRoutine(show));
	}

	IEnumerator SlideRoutine(bool show)
	{
		Vector2 from = _panelRect.anchoredPosition;
		Vector2 to = show ? _shownPos : _hiddenPos;

		if (show) SetInteractable(true);

		float t = 0f;
		while (t < _slideDuration)
		{
			t += Time.deltaTime;
			float lerpT = _slideCurve.Evaluate(Mathf.Clamp01(t / _slideDuration));
			_panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, lerpT);
			yield return null;
		}
		_panelRect.anchoredPosition = to;

		if (!show) SetInteractable(false);
	}

	void SetInteractable(bool value)
	{
		_canvasGroup.interactable = value;
		_canvasGroup.blocksRaycasts = value;
	}

	void SelectCategory(BuildingCategory category, Button tab)
	{
		_currentCategory = category;

		if (_selectedTab != null)
			_selectedTab.GetComponent<Image>().color = _normalTabColor;
		_selectedTab = tab;
		_selectedTab.GetComponent<Image>().color = _selectedTabColor;

		bool isRepair = category == BuildingCategory.Repair;
		if (_moveButtonObj != null) _moveButtonObj.SetActive(isRepair);
		if (_sellButtonObj != null) _sellButtonObj.SetActive(isRepair);

		RefreshBuildingPanel();
		BuildingPlacer.Instance.CancelPlacement();
	}

	void RefreshBuildingPanel()
	{
		foreach (Transform child in _buildingPanel.transform)
			Destroy(child.gameObject);

		if (_currentCategory == BuildingCategory.Repair) return; // Repair dùng 2 nút riêng, không tạo building button

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