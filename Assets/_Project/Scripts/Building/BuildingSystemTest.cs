using UnityEngine;

/// <summary>
/// Script test t?m — nh?n B ?? ch?n building ??u tiên.
/// Xóa sau khi test xong.
/// </summary>
public class BuildingSystemTest : MonoBehaviour
{
	[SerializeField] private BuildingData _testBuilding;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.B))
		{
			BuildingPlacer.Instance.SelectBuilding(_testBuilding);
			Debug.Log("[Test] Selected: " + _testBuilding.BuildingName);
		}
	}
}