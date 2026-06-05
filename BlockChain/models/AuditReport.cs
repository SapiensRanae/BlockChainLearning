public class AuditReport
{
    public bool IsChainValid { get; set; }
    public List<int> CompromisedBlockIndexes { get; set; } = new();
    public Dictionary<int, List<string>> ViolationDetails { get; set; } = new();
    // ViolationDetails: ключ = індекс блоку, значення = список рядків з описом порушень
}