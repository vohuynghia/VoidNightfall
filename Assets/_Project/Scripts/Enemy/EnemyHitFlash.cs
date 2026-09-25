using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class EnemyHitFlash : MonoBehaviour
{
	[Header("Flash Settings")]
	[SerializeField, ColorUsage(true, true)]
	private Color _flashColor = Color.white * 3f; // Dùng màu HDR phát sáng rực (x3 cường độ)
	[SerializeField] private float _flashDuration = 0.08f;

	private Renderer[] _renderers;
	private MaterialPropertyBlock _propBlock;
	private HealthSystem _healthSystem;
	private Coroutine _flashCoroutine;

	// Các biến Shader chuẩn của Universal Render Pipeline / Lit
	private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
	private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

	private void Awake()
	{
		_renderers = GetComponentsInChildren<Renderer>();
		_propBlock = new MaterialPropertyBlock();
		_healthSystem = GetComponent<HealthSystem>();
	}

	private void OnEnable()
	{
		if (_healthSystem != null)
			_healthSystem.OnDamaged.AddListener(TriggerFlash);
	}

	private void OnDisable()
	{
		if (_healthSystem != null)
			_healthSystem.OnDamaged.RemoveListener(TriggerFlash);
	}

	private void TriggerFlash(float damage)
	{
		if (_flashCoroutine != null)
			StopCoroutine(_flashCoroutine);

		_flashCoroutine = StartCoroutine(FlashRoutine());
	}

	private IEnumerator FlashRoutine()
	{
		// 1. Bật nháy trắng cực mạnh bằng cả BaseColor và Emission
		foreach (var r in _renderers)
		{
			r.GetPropertyBlock(_propBlock);
			_propBlock.SetColor(BaseColorID, Color.white);
			_propBlock.SetColor(EmissionColorID, _flashColor);
			r.SetPropertyBlock(_propBlock);
		}

		yield return new WaitForSeconds(_flashDuration);

		// 2. Trả lại trạng thái vật liệu ban đầu
		foreach (var r in _renderers)
		{
			r.SetPropertyBlock(null);
		}

		_flashCoroutine = null;
	}
}