using UnityEngine;

[CreateAssetMenu(fileName = "TurretData", menuName = "VoidNightfall/Turret Data")]
public class TurretData : ScriptableObject
{
	[Header("Combat")]
	public float DetectionRange = 15f;   // tầm phát hiện enemy
	public float FireRate = 1f;          // số viên/giây
	public float BulletSpeed = 20f;
	public float BulletDamage = 15f;

	[Header("Rotation")]
	public float RotateSpeed = 10f;      // tốc độ xoay nòng súng
}