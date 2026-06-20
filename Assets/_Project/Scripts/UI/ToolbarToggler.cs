using UnityEngine;

public class ToolbarToggler : MonoBehaviour
{
    [SerializeField] private BuildingToolbarManager _toolbar;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            bool isActive = _toolbar.gameObject.activeSelf;
            _toolbar.ToggleToolbar(!isActive);

            if (isActive)
                BuildingInfoPanel.Instance.Hide();
        }
    }
}