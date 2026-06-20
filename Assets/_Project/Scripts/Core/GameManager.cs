using UnityEngine;

/// <summary>
/// Quản lý trạng thái tổng thể của game (Playing, Building, GameOver...).
/// Singleton — chỉ tồn tại 1 instance duy nhất trong toàn game.
/// </summary>
public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	public enum GameState { Building, Wave, Paused, GameOver }
	public GameState CurrentState { get; private set; }

	private void Awake()
	{
		// Singleton setup
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		ChangeState(GameState.Building);
	}

	public void ChangeState(GameState newState)
	{
		CurrentState = newState;
		Debug.Log($"[GameManager] State changed to: {newState}");

		switch (newState)
		{
			case GameState.Building:
				Time.timeScale = 1f;
				break;
			case GameState.Wave:
				Time.timeScale = 1f;
				break;
			case GameState.Paused:
				Time.timeScale = 0f;
				break;
			case GameState.GameOver:
				Time.timeScale = 0f;
				EventBus.Publish(new GameOverEvent { IsVictory = false });
				break;
		}
	}

	private void OnDestroy()
	{
		EventBus.ClearAll();
	}
}