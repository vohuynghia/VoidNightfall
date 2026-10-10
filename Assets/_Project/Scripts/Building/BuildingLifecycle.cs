using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum BuildingState
{
	Constructing, // Đang xây dựng
	Active,       // Đang hoạt động bình thường
	Ruined        // Đã bị phá hủy/cháy hỏng
}

[RequireComponent(typeof(PlacedBuildingInfo))]
public class BuildingLifecycle : MonoBehaviour
{
	[Header("Visual Settings")]
	[SerializeField] private Material _hologramMaterial;
	[SerializeField] private Color _ruinedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
	[SerializeField] private GameObject _ruinedFireVfxPrefab;

	[Header("Power Settings")]
	[SerializeField] private GameObject _noPowerIcon; // Icon tia sét đỏ hiển thị khi mất điện (nếu có)

	public BuildingState State { get; private set; } = BuildingState.Constructing;
	public float ConstructionProgress { get; private set; }
	public bool IsPowered { get; private set; } = true;

	private PlacedBuildingInfo _buildingInfo;
	private HealthSystem _healthSystem;

	private struct RendererData
	{
		public Renderer renderer;
		public Material[] originalMaterials;
	}
	private List<RendererData> _renderersData = new List<RendererData>();
	private GameObject _activeFireVfx;
	private TextMeshPro _worldCountdownText;

	private void Awake()
	{
		_buildingInfo = GetComponent<PlacedBuildingInfo>();
		_healthSystem = GetComponent<HealthSystem>();
		CacheRenderers();
	}

	private void CacheRenderers()
	{
		_renderersData.Clear();
		foreach (var r in GetComponentsInChildren<Renderer>(true))
		{
			if (r is MeshRenderer || r is SkinnedMeshRenderer)
			{
				_renderersData.Add(new RendererData
				{
					renderer = r,
					originalMaterials = r.sharedMaterials
				});
			}
		}
	}

	private void Start()
	{
		if (_healthSystem != null)
		{
			_healthSystem.OnDeath.AddListener(OnBuildingDestroyed);
		}
	}

	private void OnDestroy()
	{
		if (_healthSystem != null)
		{
			_healthSystem.OnDeath.RemoveListener(OnBuildingDestroyed);
		}

		// Rút khỏi mạng lưới điện khi bị bán hoặc xóa
		if (PowerGridManager.Instance != null)
		{
			PowerGridManager.Instance.UnregisterBuilding(this);
		}
	}

	/// <summary>
	/// Dùng khi di chuyển công trình hoặc load game: Bỏ qua đếm ngược và hoạt động luôn.
	/// </summary>
	public void SetActiveInstantly()
	{
		CacheRenderers();
		State = BuildingState.Active;
		RestoreOriginalVisual();
		SetFunctionalityEnabled(true);

		if (PowerGridManager.Instance != null)
			PowerGridManager.Instance.RegisterBuilding(this);
	}

	// ================== GIAI ĐOẠN 1: XÂY DỰNG ==================

	public void InitLifecycle(BuildingData data)
	{
		CacheRenderers();
		StartConstruction(data.BuildTime);
	}

	public void StartConstruction(float duration)
	{
		State = BuildingState.Constructing;
		SetFunctionalityEnabled(false);
		ApplyHologramVisual();
		CreateCountdownUI();

		StartCoroutine(ConstructionRoutine(duration));
	}

	private IEnumerator ConstructionRoutine(float totalTime)
	{
		float timer = 0f;

		while (timer < totalTime)
		{
			timer += Time.deltaTime;
			ConstructionProgress = Mathf.Clamp01(timer / totalTime);

			if (_worldCountdownText != null)
			{
				float remain = Mathf.Max(0f, totalTime - timer);
				_worldCountdownText.text = $"{remain:F1}s";
			}

			yield return null;
		}

		CompleteConstruction();
	}

	private void CompleteConstruction()
	{
		State = BuildingState.Active;
		RestoreOriginalVisual();
		SetFunctionalityEnabled(true);

		if (_worldCountdownText != null)
			Destroy(_worldCountdownText.gameObject);

		// Đăng ký vào mạng lưới điện khi bắt đầu hoạt động
		if (PowerGridManager.Instance != null)
			PowerGridManager.Instance.RegisterBuilding(this);

		Debug.Log($"[BuildingLifecycle] {gameObject.name} hoàn tất xây dựng!");
	}

	// ================== GIAI ĐOẠN 2: BỊ PHÁ HỦY / CHÁY HỎNG ==================

	private void OnBuildingDestroyed()
	{
		State = BuildingState.Ruined;
		SetFunctionalityEnabled(false);
		ApplyRuinedVisual();

		if (_ruinedFireVfxPrefab != null && _activeFireVfx == null)
		{
			_activeFireVfx = Instantiate(_ruinedFireVfxPrefab, transform.position, Quaternion.identity, transform);
		}

		// Rút khỏi mạng lưới điện khi công trình sập
		if (PowerGridManager.Instance != null)
			PowerGridManager.Instance.UnregisterBuilding(this);

		Debug.Log($"[BuildingLifecycle] {gameObject.name} đã sập! Chuyển sang trạng thái phế tích.");
	}

	public void ReviveFromRuin()
	{
		if (State != BuildingState.Ruined) return;

		State = BuildingState.Active;
		RestoreOriginalVisual();
		SetFunctionalityEnabled(true);

		if (_activeFireVfx != null)
		{
			Destroy(_activeFireVfx);
			_activeFireVfx = null;
		}

		// Đăng ký lại vào mạng lưới điện khi hồi sinh
		if (PowerGridManager.Instance != null)
			PowerGridManager.Instance.RegisterBuilding(this);

		Debug.Log($"[BuildingLifecycle] {gameObject.name} đã được phục hồi thành công!");
	}

	// ================== GIAI ĐOẠN 3: NĂNG LƯỢNG / BẬT TẮT ==================

	/// <summary>
	/// Được gọi bởi PowerGridManager khi trạng thái cấp điện thay đổi
	/// </summary>
	public void SetPowered(bool powered)
	{
		IsPowered = powered;

		if (_noPowerIcon != null)
			_noPowerIcon.SetActive(!powered && State == BuildingState.Active);

		SetFunctionalityEnabled(powered && State == BuildingState.Active);
	}

	private void SetFunctionalityEnabled(bool isEnabled)
	{
		if (TryGetComponent<Turret>(out var turret))
			turret.enabled = isEnabled;

		if (TryGetComponent<ResourceCollector>(out var collector))
			collector.enabled = isEnabled;

		if (TryGetComponent<Collider>(out var col))
			col.enabled = (State != BuildingState.Ruined);
	}

	private void ApplyHologramVisual()
	{
		if (_hologramMaterial == null) return;

		foreach (var data in _renderersData)
		{
			if (data.renderer == null) continue;
			Material[] holoMats = new Material[data.originalMaterials.Length];
			for (int i = 0; i < holoMats.Length; i++)
				holoMats[i] = _hologramMaterial;
			data.renderer.sharedMaterials = holoMats;
		}
	}

	private void ApplyRuinedVisual()
	{
		foreach (var data in _renderersData)
		{
			if (data.renderer == null) continue;
			foreach (var mat in data.renderer.materials)
			{
				if (mat.HasProperty("_BaseColor"))
					mat.SetColor("_BaseColor", _ruinedColor);
				else if (mat.HasProperty("_Color"))
					mat.SetColor("_Color", _ruinedColor);
			}
		}
	}

	private void RestoreOriginalVisual()
	{
		foreach (var data in _renderersData)
		{
			if (data.renderer != null)
				data.renderer.sharedMaterials = data.originalMaterials;
		}
	}

	private void CreateCountdownUI()
	{
		GameObject textObj = new GameObject("Build_Countdown_Text");
		textObj.transform.SetParent(transform);
		textObj.transform.localPosition = Vector3.up * 2f;

		_worldCountdownText = textObj.AddComponent<TextMeshPro>();
		_worldCountdownText.alignment = TextAlignmentOptions.Center;
		_worldCountdownText.fontSize = 4;
		_worldCountdownText.color = Color.cyan;

		textObj.AddComponent<FaceCamera>();
	}
}