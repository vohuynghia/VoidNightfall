using UnityEngine;

public class ToolbarToggler : MonoBehaviour
{
	[SerializeField] private BuildingToolbarManager _toolbar;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.B))
		{
			bool wasOpen = _toolbar.IsOpen;
			_toolbar.ToggleToolbar(!wasOpen);

			if (wasOpen)
				BuildingInfoPanel.Instance.Hide();
		}
	}
}