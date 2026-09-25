using UnityEngine;

public class ToolbarToggler : MonoBehaviour
{
	[SerializeField] private BuildingToolbarManager _toolbar;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.B))
		{
			bool wasOpen = _toolbar.IsOpen;
			bool targetState = !wasOpen;

			_toolbar.ToggleToolbar(targetState);

			// Báo cho HUDManager hoán đổi hiển thị ở góc dưới trái
			if (HUDManager.Instance != null)
				HUDManager.Instance.SetBuildMode(targetState);

			if (wasOpen && BuildingInfoPanel.Instance != null)
				BuildingInfoPanel.Instance.Hide();
		}
	}
}