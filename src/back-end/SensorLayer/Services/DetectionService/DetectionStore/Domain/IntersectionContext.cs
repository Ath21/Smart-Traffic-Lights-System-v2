using System;

namespace DetectionStore.Domain;

public class IntersectionContext
{
    public int Id { get; }
    public string Name { get; }

    public IntersectionContext(int id, string name)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

