using UnityEngine;
using TMPro;

public class BuildingInfoPanel : MonoBehaviour
{
	public static BuildingInfoPanel Instance { get; private set; }

	[SerializeField] private GameObject _panel;
	[SerializeField] private TextMeshProUGUI _nameText;
	[SerializeField] private TextMeshProUGUI _descText;
	[SerializeField] private TextMeshProUGUI _costText;
	[SerializeField] private TextMeshProUGUI _requirementText;
	[SerializeField] private TextMeshProUGUI _statsText;
	[SerializeField] private TextMeshProUGUI _upkeepText;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}
	private void Start()
	{
		_panel.SetActive(false);
	}
	public void Show(BuildingData data)
	{
		_panel.SetActive(true);

		_nameText.text = data.BuildingName;
		_descText.text = data.Description;

		_costText.text = $"Carbonium: {data.CarboniumCost}\n" +
						 $"Metal: {data.MetalCost}\n" +
						 $"Energy: {data.EnergyCost}";

		_requirementText.text = $"Requires: {data.Requirement}";

		_statsText.text = $"HP: {data.Health}\n" +
						  $"Damage: {data.Damage}\n" +
						  $"Fire Rate: {data.FireRate}/sec\n" +
						  $"Range: {data.Range}";

		_upkeepText.text = $"? Energy: -{data.EnergyUpkeep}/sec";
	}

	public void Hide()
	{
		_panel.SetActive(false);
	}
}