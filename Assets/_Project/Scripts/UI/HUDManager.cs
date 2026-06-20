using UnityEngine;
using TMPro;

/// <summary>
/// Cap nhat HUD khi tài nguyên thay doi qua EventBus.
/// </summary>
public class HUDManager : MonoBehaviour
{
	[Header("Resource Texts")]
	[SerializeField] private TextMeshProUGUI _carboniumText;
	[SerializeField] private TextMeshProUGUI _metalText;
	[SerializeField] private TextMeshProUGUI _energyText;

	[Header("Wave Info")]
	[SerializeField] private TextMeshProUGUI _waveText;
	[SerializeField] private TextMeshProUGUI _countdownText;

	private void OnEnable()
	{
		EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
		EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
		EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
	}

	private void OnDisable()
	{
		EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
		EventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
		EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
	}

	private void Start()
	{
		// Hi?n th? giá tr? ban ??u
		UpdateText(ResourceType.Carbonium, ResourceManager.Instance.Get(ResourceType.Carbonium));
		UpdateText(ResourceType.Metal, ResourceManager.Instance.Get(ResourceType.Metal));
		UpdateText(ResourceType.Energy, ResourceManager.Instance.Get(ResourceType.Energy));
	}

	void OnResourceChanged(ResourceChangedEvent e)
	{
		if (System.Enum.TryParse(e.ResourceType, out ResourceType type))
			UpdateText(type, e.NewAmount);
	}

	void UpdateText(ResourceType type, int amount)
	{
		switch (type)
		{
			case ResourceType.Carbonium:
				if (_carboniumText) _carboniumText.text = $"? Carbonium: {amount}";
				break;
			case ResourceType.Metal:
				if (_metalText) _metalText.text = $"? Metal: {amount}";
				break;
			case ResourceType.Energy:
				if (_energyText) _energyText.text = $"? Energy: {amount}";
				break;
		}
	}
	private void Update()
	{
		// C?p nh?t ??m ng??c
		if (WaveManager.Instance == null || _countdownText == null) return;

		if (!WaveManager.Instance.IsWaveActive)
		{
			int seconds = Mathf.CeilToInt(WaveManager.Instance.TimeUntilNextWave);
			_countdownText.text = $"Next wave in: {seconds}s";
		}
		else
		{
			_countdownText.text = "";
		}
	}

	void OnWaveStarted(WaveStartedEvent e)
	{
		if (_waveText) _waveText.text = $"Wave {e.WaveNumber}";
		Debug.Log($"[HUD] Wave {e.WaveNumber} started!");
	}

	void OnWaveCompleted(WaveCompletedEvent e)
	{
		if (_waveText) _waveText.text = $"Wave {e.WaveNumber} Complete!";
	}
}