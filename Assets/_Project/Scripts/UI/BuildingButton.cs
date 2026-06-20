using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Button ??i di?n cho 1 lo?i building trong toolbar.
/// Click vào ?? ch?n building ?ó và vào build mode.
/// </summary>
public class BuildingButton : MonoBehaviour
{
    [SerializeField] private BuildingData _buildingData;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _costText;

	private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }


	public void Init(BuildingData data)
	{
		_buildingData = data;

		if (_icon && data.Icon)
			_icon.sprite = data.Icon;

		if (_nameText)
			_nameText.text = data.BuildingName;
	}

	private void Start()
    {
        // Hi?n th? thông tin building
        if (_buildingData == null) return;

        if (_icon && _buildingData.Icon)
            _icon.sprite = _buildingData.Icon;

        if (_nameText)
            _nameText.text = _buildingData.BuildingName;

        if (_costText)
            _costText.text = $"{_buildingData.CarboniumCost}C {_buildingData.MetalCost}M";
    }

	void OnClick()
	{
		if (_buildingData == null) return;
		BuildingPlacer.Instance.SelectBuilding(_buildingData);
		BuildingInfoPanel.Instance.Show(_buildingData);
		Debug.Log($"[BuildingButton] Selected: {_buildingData.BuildingName}");
	}
}