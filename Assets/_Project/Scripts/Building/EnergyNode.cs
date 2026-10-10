using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đại diện cho một điểm nút trong mạng lưới điện (Cột điện, Máy phát điện, Nhà chính).
/// Tự động tìm kiếm các Node lân cận và vẽ tia năng lượng (LineRenderer) nối liền.
/// </summary>
public class EnergyNode : MonoBehaviour
{
	[Header("Node Settings")]
	[Tooltip("Khoảng cách tối đa để nối dây điện tới node khác")]
	[SerializeField] private float _connectionRange = 8f;
	[Tooltip("Độ cao điểm cắm dây trên đỉnh trụ (tính từ gốc toạ độ công trình)")]
	[SerializeField] private float _beamHeightOffset = 1.2f;

	[Header("Visual Beam")]
	[SerializeField] private Material _energyBeamMaterial;
	[SerializeField] private float _beamWidth = 0.08f;
	[SerializeField] private Color _beamColor = new Color(0f, 0.85f, 1f, 0.9f);

	public float ConnectionRange => _connectionRange;
	public List<EnergyNode> ConnectedNeighbors { get; private set; } = new();

	private readonly Dictionary<EnergyNode, LineRenderer> _beamLines = new();
	private BuildingLifecycle _lifecycle;

	private void Awake()
	{
		_lifecycle = GetComponent<BuildingLifecycle>();
	}

	private void Start()
	{
		// Khi công trình đã hoàn tất xây dựng hoặc khởi tạo, đăng ký vào PowerGridManager
		RegisterToGrid();
	}

	public void RegisterToGrid()
	{
		if (PowerGridManager.Instance != null)
		{
			PowerGridManager.Instance.RegisterNode(this);
		}
	}

	private void OnDisable()
	{
		DisconnectAll();
		if (PowerGridManager.Instance != null)
		{
			PowerGridManager.Instance.UnregisterNode(this);
		}
	}

	/// <summary>
	/// Tìm kiếm và nối dây điện tới các node lân cận trong tầm
	/// </summary>
	public void ConnectToNearbyNodes(List<EnergyNode> allNodes)
	{
		foreach (var other in allNodes)
		{
			if (other == null || other == this) continue;

			// Kiểm tra cả 2 node có đang ở trạng thái Active không (nếu có Lifecycle)
			if (_lifecycle != null && _lifecycle.State != BuildingState.Active) continue;
			if (other._lifecycle != null && other._lifecycle.State != BuildingState.Active) continue;

			float maxDist = Mathf.Min(this._connectionRange, other.ConnectionRange);
			float dist = Vector3.Distance(transform.position, other.transform.position);

			if (dist <= maxDist)
			{
				ConnectWith(other);
			}
		}
	}

	private void ConnectWith(EnergyNode other)
	{
		if (!ConnectedNeighbors.Contains(other))
		{
			ConnectedNeighbors.Add(other);
			if (!other.ConnectedNeighbors.Contains(this))
				other.ConnectedNeighbors.Add(this);

			// Chỉ một trong hai node tạo LineRenderer để tránh vẽ đè 2 lần tia điện
			if (GetInstanceID() < other.GetInstanceID())
			{
				CreateBeamTo(other);
			}
		}
	}

	private void CreateBeamTo(EnergyNode target)
	{
		if (_beamLines.ContainsKey(target)) return;

		GameObject lineObj = new GameObject($"Beam_{gameObject.name}_To_{target.gameObject.name}");
		lineObj.transform.SetParent(transform);

		LineRenderer lr = lineObj.AddComponent<LineRenderer>();
		lr.material = _energyBeamMaterial != null ? _energyBeamMaterial : new Material(Shader.Find("Sprites/Default"));
		lr.startColor = _beamColor;
		lr.endColor = _beamColor;
		lr.startWidth = _beamWidth;
		lr.endWidth = _beamWidth;
		lr.positionCount = 2;
		lr.useWorldSpace = true;

		Vector3 startPos = transform.position + Vector3.up * _beamHeightOffset;
		Vector3 endPos = target.transform.position + Vector3.up * target._beamHeightOffset;
		lr.SetPosition(0, startPos);
		lr.SetPosition(1, endPos);

		_beamLines[target] = lr;
	}

	public void DisconnectAll()
	{
		foreach (var neighbor in ConnectedNeighbors)
		{
			if (neighbor != null)
			{
				neighbor.ConnectedNeighbors.Remove(this);
				neighbor.RemoveBeamTo(this);
			}
		}
		ConnectedNeighbors.Clear();
		ClearAllBeams();
	}

	public void RemoveBeamTo(EnergyNode target)
	{
		if (_beamLines.TryGetValue(target, out var lr))
		{
			if (lr != null) Destroy(lr.gameObject);
			_beamLines.Remove(target);
		}
	}

	private void ClearAllBeams()
	{
		foreach (var pair in _beamLines)
		{
			if (pair.Value != null) Destroy(pair.Value);
		}
		_beamLines.Clear();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, _connectionRange);
	}
}