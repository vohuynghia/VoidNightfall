using System.Collections;
using UnityEngine;

/// <summary>
/// Drone nano bay t? sau l?ng ng??i ch?i ??n công trình ?? s?a ch?a.
/// </summary>
public class RepairDrone : MonoBehaviour
{
	[SerializeField] private float _flySpeed = 16f;
	[SerializeField] private float _repairDuration = 3f; // Th?i gian drone s?a công trình

	private PlacedBuildingInfo _targetBuilding;
	private HealthSystem _targetHealth;
	private BuildingLifecycle _lifecycle;
	private GameObject _healthBarObj;

	public void Launch(Vector3 startPos, PlacedBuildingInfo target, GameObject healthBarPrefab)
	{
		transform.position = startPos;
		_targetBuilding = target;
		_targetHealth = target.GetComponent<HealthSystem>();
		_lifecycle = target.GetComponent<BuildingLifecycle>();

		// T?o thanh máu th? gi?i g?n d??i chân công trình n?u ch?a có
		if (healthBarPrefab != null)
		{
			_healthBarObj = Instantiate(healthBarPrefab, target.transform.position + Vector3.up * 0.2f, Quaternion.identity);
			UpdateHealthBarUI();
		}

		StartCoroutine(RepairRoutine());
	}

	private IEnumerator RepairRoutine()
	{
		if (_targetBuilding == null || _targetHealth == null)
		{
			Destroy(gameObject);
			yield break;
		}

		Vector3 targetPos = _targetBuilding.transform.position + Vector3.up * 1.2f;

		// 1. Bay l??n vòng cung ??n công trình
		while (Vector3.Distance(transform.position, targetPos) > 0.3f)
		{
			transform.position = Vector3.MoveTowards(transform.position, targetPos, _flySpeed * Time.deltaTime);
			transform.LookAt(targetPos);
			yield return null;
		}

		// 2. N?u công trình ?ang ? tr?ng thái ph? tích (Ruined), h?i sinh l?i tr??c
		if (_lifecycle != null && _lifecycle.State == BuildingState.Ruined)
		{
			_lifecycle.ReviveFromRuin();
		}

		// 3. Ti?n hành s?a ch?a d?n d?n cho ??n khi ??y máu
		float startHealth = _targetHealth.CurrentHealth;
		float missingHealth = _targetHealth.MaxHealth - startHealth;
		float elapsed = 0f;

		while (elapsed < _repairDuration)
		{
			if (_targetHealth == null) break;

			elapsed += Time.deltaTime;
			float addHp = (missingHealth / _repairDuration) * Time.deltaTime;
			_targetHealth.Heal(addHp);

			UpdateHealthBarUI();
			yield return null;
		}

		// ??m b?o ??y 100% máu
		if (_targetHealth != null)
		{
			_targetHealth.Heal(_targetHealth.MaxHealth);
		}

		// Thu d?n thanh máu và drone
		if (_healthBarObj != null)
			Destroy(_healthBarObj);

		// Drone bi?n m?t ho?c bay ng??c v?
		Destroy(gameObject);
	}

	private void UpdateHealthBarUI()
	{
		if (_healthBarObj != null && _targetHealth != null)
		{
			var slider = _healthBarObj.GetComponentInChildren<UnityEngine.UI.Slider>();
			if (slider != null)
			{
				slider.value = _targetHealth.CurrentHealth / _targetHealth.MaxHealth;
			}
		}
	}

	private void OnDestroy()
	{
		if (_healthBarObj != null)
			Destroy(_healthBarObj);
	}
}