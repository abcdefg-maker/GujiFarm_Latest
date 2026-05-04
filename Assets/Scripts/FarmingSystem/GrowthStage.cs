namespace FarmingSystem
{
    /// <summary>
    /// Growth stage enumeration for crops
    /// </summary>
    public enum GrowthStage
    {
        Seed = 0,           // Just planted
        Sprout = 1,         // Sprouting
        Growing = 2,        // Growing
        Mature = 3,         // Almost mature
        Harvestable = 4     // Ready to harvest
    }
}
