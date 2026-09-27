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
	[SerializeField] private GameObject _ruinedFireVfxPrefab; // Prefab lửa khói (nếu có, để trống nếu chưa có)

	public BuildingState State { get; private set; } = BuildingState.Constructing;
	public float ConstructionProgress { get; private set; } 

	private PlacedBuildingInfo _buildingInfo;
	private HealthSystem _healthSystem;

	// Quản lý Renderers và Material gốc
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
		// KHÔNG gọi StartConstruction() để tránh bị null Data
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
	}

	// ================== GIAI ĐOẠN 1: XÂY DỰNG ==================
	/// <summary>
	/// Hàm khởi tạo tiến trình xây dựng được gọi trực tiếp từ BuildingPlacer
	/// </summary>
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

		Debug.Log($"[BuildingLifecycle] {gameObject.name} hoàn tất xây dựng!");
	}

	// ================== GIAI ĐOẠN 2: BỊ PHÁ HỦY / CHÁY HỎNG ==================

	private void OnBuildingDestroyed()
	{
		State = BuildingState.Ruined;
		SetFunctionalityEnabled(false);
		ApplyRuinedVisual();

		// Sinh lửa khói
		if (_ruinedFireVfxPrefab != null && _activeFireVfx == null)
		{
			_activeFireVfx = Instantiate(_ruinedFireVfxPrefab, transform.position, Quaternion.identity, transform);
		}

		Debug.Log($"[BuildingLifecycle] {gameObject.name} đã sập! Chuyển sang trạng thái phế tích.");
	}

	/// <summary>
	/// Gọi hàm này từ tia sửa chữa (Repair Beam) của Player khi công trình đang sập
	/// </summary>
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

		Debug.Log($"[BuildingLifecycle] {gameObject.name} đã được phục hồi thành công!");
	}

	// ================== CÁC TIỆN ÍCH BẬT / TẮT CHỨC NĂNG ==================

	private void SetFunctionalityEnabled(bool isEnabled)
	{
		// 1. Tắt/Bật Turret bắn đạn
		if (TryGetComponent<Turret>(out var turret))
			turret.enabled = isEnabled;

		// 2. Tắt/Bật bộ hút tài nguyên
		if (TryGetComponent<ResourceCollector>(out var collector))
			collector.enabled = isEnabled;

		// 3. Không cho quái target nếu đang trong trạng thái phế tích sập hoàn toàn
		// (Nếu muốn quái vẫn có thể đánh lúc đang xây dựng thì chỉ tắt khi Ruined)
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
		textObj.transform.localPosition = Vector3.up * 2f; // Hiển thị phía trên công trình

		_worldCountdownText = textObj.AddComponent<TextMeshPro>();
		_worldCountdownText.alignment = TextAlignmentOptions.Center;
		_worldCountdownText.fontSize = 4;
		_worldCountdownText.color = Color.cyan;

		// Luôn xoay chữ hướng về Camera để người chơi dễ đọc
		textObj.AddComponent<FaceCamera>();
	}
}