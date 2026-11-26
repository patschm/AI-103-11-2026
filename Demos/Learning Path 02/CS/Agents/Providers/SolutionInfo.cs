namespace Agents.Providers;

public class SolutionInfo
{
    public string? Name { get; set; }
    public string? WorkFolder { get; set; }
    public ICollection<ProjectInfo> Projects { get; set; } = new HashSet<ProjectInfo>();
}
