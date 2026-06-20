using UnityEngine;

public enum AttackType { Melee, Ranged }

[CreateAssetMenu(fileName = "EnemyData", menuName = "VoidNightfall/Enemy Data")]
public class EnemyData : ScriptableObject
{
	[Header("Info")]
	public string EnemyName;
	public GameObject Prefab;

	[Header("Stats")]
	public float MaxHealth = 50f;
	public float MoveSpeed = 3f;
	public float AttackRange = 1.5f;
	public float AttackDamage = 10f;
	public float AttackCooldown = 1f;

	[Header("Attack")]
	public AttackType AttackType = AttackType.Melee;
	public GameObject ProjectilePrefab;
	public float ProjectileSpeed = 15f;

	[Header("AI Strategy")]
	public EnemyAI.TargetStrategy Strategy = EnemyAI.TargetStrategy.Nearest;

	[Header("Reward")]
	public int CarboniumDrop = 10;
	public int MetalDrop = 5;
}