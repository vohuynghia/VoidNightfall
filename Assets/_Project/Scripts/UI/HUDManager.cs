using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
	public static HUDManager Instance { get; private set; }

	[Header("Panels Switch (Góc dưới trái)")]
	[SerializeField] private GameObject _combatHUDPanel;        // Panel chứa Máu, Giáp, Hotbar/Skill
	[SerializeField] private GameObject _buildingDetailPanel;   // Panel hiển thị thông tin chi tiết trụ

	[Header("Health & Shield UI")]
	[SerializeField] private Slider _healthSlider;
	[SerializeField] private TextMeshProUGUI _healthText;
	[SerializeField] private Slider _shieldSlider;
	[SerializeField] private TextMeshProUGUI _shieldText;

	[Header("Skill / Dash UI")]
	[SerializeField] private Image _dashCooldownOverlay;        // Image dạng Filled (Radial 360) phủ lên icon
	[SerializeField] private TextMeshProUGUI _dashCooldownText;

	[Header("Resource Texts")]
	[SerializeField] private TextMeshProUGUI _carboniumText;
	[SerializeField] private TextMeshProUGUI _metalText;
	[SerializeField] private TextMeshProUGUI _energyText;

	[Header("Wave Info")]
	[SerializeField] private TextMeshProUGUI _waveText;
	[SerializeField] private TextMeshProUGUI _countdownText;

	private HealthSystem _playerHealth;
	private ShieldSystem _playerShield;
	private PlayerController _playerController;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	private void Start()
	{
		// Tìm player trong scene
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			_playerHealth = player.GetComponent<HealthSystem>();
			_playerShield = player.GetComponent<ShieldSystem>();
			_playerController = player.GetComponent<PlayerController>();

			if (_playerHealth != null)
			{
				_playerHealth.OnHealthChanged.AddListener(UpdateHealthUI);
				UpdateHealthUI(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
			}

			if (_playerShield != null)
			{
				_playerShield.OnShieldChanged.AddListener(UpdateShieldUI);
				UpdateShieldUI(_playerShield.CurrentShield, _playerShield.MaxShield);
			}
		}

		// Giá trị tài nguyên ban đầu
		if (ResourceManager.Instance != null)
		{
			UpdateText(ResourceType.Carbonium, ResourceManager.Instance.Get(ResourceType.Carbonium));
			UpdateText(ResourceType.Metal, ResourceManager.Instance.Get(ResourceType.Metal));
			UpdateText(ResourceType.Energy, ResourceManager.Instance.Get(ResourceType.Energy));
		}

		// Mặc định ban đầu: Combat HUD bật, Building detail tắt
		SetBuildMode(false);
	}

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

	private void Update()
	{
		// Cập nhật Wave Countdown
		if (WaveManager.Instance != null && _countdownText != null)
		{
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

		// Cập nhật Cooldown Dash UI
		UpdateDashUI();
	}

	/// <summary>
	/// Chuyển đổi qua lại giữa Combat UI và Building Detail Panel
	/// </summary>
	public void SetBuildMode(bool isBuilding)
	{
		if (_combatHUDPanel != null)
			_combatHUDPanel.SetActive(!isBuilding);

		if (_buildingDetailPanel != null)
			_buildingDetailPanel.SetActive(isBuilding);
	}

	void UpdateHealthUI(float current, float max)
	{
		if (_healthSlider != null) _healthSlider.value = current / max;
		if (_healthText != null) _healthText.text = $"{Mathf.CeilToInt(current)}";
	}

	void UpdateShieldUI(float current, float max)
	{
		if (_shieldSlider != null) _shieldSlider.value = max > 0 ? (current / max) : 0;
		if (_shieldText != null) _shieldText.text = $"{Mathf.CeilToInt(current)}";
	}

	void UpdateDashUI()
	{
		if (_playerController == null || _dashCooldownOverlay == null) return;

		float remaining = _playerController.DashCooldownRemaining;
		float total = _playerController.DashCooldownDuration;

		if (remaining > 0f)
		{
			_dashCooldownOverlay.fillAmount = remaining / total;
			if (_dashCooldownText != null)
				_dashCooldownText.text = remaining.ToString("F1");
		}
		else
		{
			_dashCooldownOverlay.fillAmount = 0f;
			if (_dashCooldownText != null)
				_dashCooldownText.text = "";
		}
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
				if (_carboniumText) _carboniumText.text = $"Carbonium: {amount}";
				break;
			case ResourceType.Metal:
				if (_metalText) _metalText.text = $"Metal: {amount}";
				break;
			case ResourceType.Energy:
				if (_energyText) _energyText.text = $"Energy: {amount}";
				break;
		}
	}

	void OnWaveStarted(WaveStartedEvent e)
	{
		if (_waveText) _waveText.text = $"Wave {e.WaveNumber}";
	}

	void OnWaveCompleted(WaveCompletedEvent e)
	{
		if (_waveText) _waveText.text = $"Wave {e.WaveNumber} Complete!";
	}
}