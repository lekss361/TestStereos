namespace SearchOrchestrator.Domain.Entities;

public class Source
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastIndexedAt { get; private set; }

    private Source() { }

    public static Source Create(string name, string location)
    {
        return new Source
        {
            Id = Guid.NewGuid(),
            Name = name,
            Location = location,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkIndexed()
    {
        LastIndexedAt = DateTimeOffset.UtcNow;
    }
}
