using System.Collections.Generic;
using UnityEngine;

public class PowerGridManager : MonoBehaviour
{
	public static PowerGridManager Instance { get; private set; }

	private readonly List<EnergyNode> _allNodes = new();
	private readonly List<BuildingLifecycle> _registeredBuildings = new();

	public int TotalProduction { get; private set; }
	public int TotalConsumption { get; private set; }
	public int NetEnergy => TotalProduction - TotalConsumption;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	// ================== QUẢN LÝ NODE ĐIỆN & DÂY NỐI ==================

	public void RegisterNode(EnergyNode node)
	{
		if (!_allNodes.Contains(node))
		{
			_allNodes.Add(node);
			node.ConnectToNearbyNodes(_allNodes);
			RecalculatePowerGrid();
		}
	}

	public void UnregisterNode(EnergyNode node)
	{
		if (_allNodes.Remove(node))
		{
			node.DisconnectAll();
			RecalculatePowerGrid();
		}
	}

	// ================== QUẢN LÝ CÔNG TRÌNH ==================

	public void RegisterBuilding(BuildingLifecycle building)
	{
		if (!_registeredBuildings.Contains(building))
		{
			_registeredBuildings.Add(building);

			// Nếu công trình này có EnergyNode thì cũng kết nối mạng lưới
			if (building.TryGetComponent<EnergyNode>(out var node))
			{
				RegisterNode(node);
			}

			RecalculatePowerGrid();
		}
	}

	public void UnregisterBuilding(BuildingLifecycle building)
	{
		if (_registeredBuildings.Remove(building))
		{
			if (building.TryGetComponent<EnergyNode>(out var node))
			{
				UnregisterNode(node);
			}

			RecalculatePowerGrid();
		}
	}

	// ================== TÍNH TOÁN CÁN CÂN ĐIỆN & THÔNG MẠCH ==================

	public void RecalculatePowerGrid()
	{
		TotalProduction = 0;
		TotalConsumption = 0;

		// 1. Tìm các Node đang phát ra điện trực tiếp (MainBase, Generator)
		List<EnergyNode> powerSources = new();

		foreach (var b in _registeredBuildings)
		{
			if (b == null || b.State != BuildingState.Active) continue;
			var data = b.GetComponent<PlacedBuildingInfo>()?.Data;
			if (data == null) continue;

			if (data.EnergyProduction > 0)
			{
				TotalProduction += data.EnergyProduction;
				if (b.TryGetComponent<EnergyNode>(out var node))
				{
					powerSources.Add(node);
				}
			}

			if (data.EnergyUpkeep > 0)
			{
				TotalConsumption += data.EnergyUpkeep;
			}
		}

		// 2. Lan truyền nguồn điện qua mạng lưới dây dẫn (Breadth-First Search / Flood Fill)
		HashSet<EnergyNode> energizedNodes = new();
		Queue<EnergyNode> queue = new Queue<EnergyNode>(powerSources);

		foreach (var src in powerSources)
			energizedNodes.Add(src);

		while (queue.Count > 0)
		{
			var curr = queue.Dequeue();
			foreach (var neighbor in curr.ConnectedNeighbors)
			{
				if (neighbor != null && !energizedNodes.Contains(neighbor))
				{
					energizedNodes.Add(neighbor);
					queue.Enqueue(neighbor);
				}
			}
		}

		// 3. Đánh giá trạng thái từng công trình
		bool hasGlobalSurplus = NetEnergy >= 0;

		foreach (var b in _registeredBuildings)
		{
			if (b == null || b.State != BuildingState.Active) continue;
			var data = b.GetComponent<PlacedBuildingInfo>()?.Data;
			if (data == null) continue;

			// Công trình không tốn điện luôn hoạt động
			if (data.EnergyUpkeep <= 0)
			{
				b.SetPowered(true);
				continue;
			}

			// Công trình cần điện: phải đủ điện tổng VÀ nằm trong tầm phủ sóng của 1 node đang có điện
			bool inCoverage = IsCoveredByEnergizedNode(b.transform.position, energizedNodes);
			b.SetPowered(hasGlobalSurplus && inCoverage);
		}

		// 4. Phát sự kiện cập nhật Resource UI
		EventBus.Publish(new EnergyGridChangedEvent
		{
			Production = TotalProduction,
			Consumption = TotalConsumption,
			Net = NetEnergy
		});
	}

	private bool IsCoveredByEnergizedNode(Vector3 pos, HashSet<EnergyNode> energizedNodes)
	{
		foreach (var node in energizedNodes)
		{
			if (node == null) continue;
			float coverageRadius = node.ConnectionRange;
			if (Vector3.Distance(node.transform.position, pos) <= coverageRadius)
			{
				return true;
			}
		}
		return false;
	}
}
public struct EnergyGridChangedEvent
{
	public int Production;
	public int Consumption;
	public int Net;
}