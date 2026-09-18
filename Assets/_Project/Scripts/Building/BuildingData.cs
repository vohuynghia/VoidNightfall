using System.Collections.Generic;
using UnityEngine;

public enum BuildingCategory
{
    MainBase,
    Attack,
    Defense,
    Resource,
    Repair
}

[CreateAssetMenu(fileName = "BuildingData", menuName = "VoidNightfall/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Info")]
    public string BuildingName;
    public string Description;
    public Sprite Icon;
    public BuildingCategory Category;

    [Header("Size")]
    public int Width = 1;
    public int Height = 1;

    [Header("Cost")]
    public int CarboniumCost;
    public int MetalCost;
    public int EnergyCost;

    [Header("Requirements")]
    public string Requirement = "None";

    [Header("Stats")]
    public float Health = 100f;
    public float Damage;
    public float FireRate;
    public float Range;
    public int EnergyUpkeep;

    [Header("Prefab")]
    public GameObject Prefab;

    public Dictionary<ResourceType, int> GetCosts()
    {
        var costs = new Dictionary<ResourceType, int>();
        if (CarboniumCost > 0) costs[ResourceType.Carbonium] = CarboniumCost;
        if (MetalCost > 0) costs[ResourceType.Metal] = MetalCost;
        if (EnergyCost > 0) costs[ResourceType.Energy] = EnergyCost;
        return costs;
    }
}