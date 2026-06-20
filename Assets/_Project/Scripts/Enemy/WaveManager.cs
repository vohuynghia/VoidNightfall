using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý các đợt enemy spawn.
/// Mỗi wave spawn nhiều enemy hơn và mạnh hơn.
/// </summary>
public class WaveManager : MonoBehaviour
{
	public static WaveManager Instance { get; private set; }

	[Header("Wave Settings")]
	[SerializeField] private List<EnemyData> _enemyTypes;  // danh sách loại enemy
	[SerializeField] private int _baseEnemyCount = 5;      // số enemy wave đầu
	[SerializeField] private float _timeBetweenWaves = 30f; // giây giữa các wave
	[SerializeField] private float _timeBetweenSpawns = 0.5f; // giây giữa mỗi lần spawn

	[Header("Spawn Settings")]
	[SerializeField] private float _spawnRadius = 45f; // spawn ở rìa map

	public int CurrentWave { get; private set; } = 0;
	public bool IsWaveActive { get; private set; } = false;
	public float TimeUntilNextWave { get; private set; }

	private int _enemiesAlive = 0;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	private void Start()
	{
		StartCoroutine(WaveLoop());
	}

	IEnumerator WaveLoop()
	{
		while (true)
		{
			// Đếm ngược đến wave tiếp theo
			TimeUntilNextWave = _timeBetweenWaves;
			while (TimeUntilNextWave > 0)
			{
				TimeUntilNextWave -= Time.deltaTime;
				yield return null;
			}

			// Bắt đầu wave
			yield return StartCoroutine(SpawnWave());

			// Chờ hết enemy rồi mới kết thúc wave
			yield return new WaitUntil(() => _enemiesAlive <= 0);

			// Kết thúc wave
			IsWaveActive = false;
			EventBus.Publish(new WaveCompletedEvent { WaveNumber = CurrentWave });
			Debug.Log($"[WaveManager] Wave {CurrentWave} completed!");
		}
	}

	IEnumerator SpawnWave()
	{
		CurrentWave++;
		IsWaveActive = true;

		// Số enemy tăng theo wave
		int enemyCount = _baseEnemyCount + (CurrentWave - 1) * 3;
		_enemiesAlive = enemyCount;

		EventBus.Publish(new WaveStartedEvent
		{
			WaveNumber = CurrentWave,
			EnemyCount = enemyCount
		});

		Debug.Log($"[WaveManager] Wave {CurrentWave} started! Spawning {enemyCount} enemies");

		for (int i = 0; i < enemyCount; i++)
		{
			SpawnEnemy();
			yield return new WaitForSeconds(_timeBetweenSpawns);
		}
	}

	void SpawnEnemy()
	{
		if (_enemyTypes == null || _enemyTypes.Count == 0) return;

		EnemyData data = _enemyTypes[Random.Range(0, _enemyTypes.Count)];
		if (data.Prefab == null) return;

		Vector3 spawnPos = GetSpawnPosition();

		GameObject enemyObj = Instantiate(data.Prefab, spawnPos, Quaternion.identity);

		if (enemyObj.TryGetComponent<EnemyAI>(out var ai))
			ai.Init(data);

		if (enemyObj.TryGetComponent<HealthSystem>(out var health))
			health.OnDeath.AddListener(() => { _enemiesAlive--; });
	}

	Vector3 GetSpawnPosition()
	{
		float mapCenter = 50f; // điều chỉnh theo map của bạn
		int maxAttempts = 10;

		for (int attempt = 0; attempt < maxAttempts; attempt++)
		{
			int side = Random.Range(0, 4);
			Vector3 candidatePos = side switch
			{
				0 => new Vector3(Random.Range(0f, 100f), 1f, mapCenter + _spawnRadius),
				1 => new Vector3(Random.Range(0f, 100f), 1f, mapCenter - _spawnRadius),
				2 => new Vector3(mapCenter - _spawnRadius, 1f, Random.Range(0f, 100f)),
				_ => new Vector3(mapCenter + _spawnRadius, 1f, Random.Range(0f, 100f)),
			};

			// Kiểm tra vị trí có nằm trên NavMesh hợp lệ không
			if (UnityEngine.AI.NavMesh.SamplePosition(candidatePos, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
			{
				// Kiểm tra không bị vật cản che (tránh spawn trong cliff)
				if (!Physics.CheckSphere(hit.position, 0.5f, LayerMask.GetMask("Default")))
					return hit.position;
			}
		}

		// Fallback nếu không tìm được vị trí tốt sau nhiều lần thử
		Debug.LogWarning("[WaveManager] Could not find valid spawn position, using fallback");
		return new Vector3(mapCenter, 1f, mapCenter + _spawnRadius);
	}
}