using System;

namespace IntersectionControllerStore.Domain;

public class TrafficLightContext
{
    public int Id { get; }
    public string Name { get; }

    public TrafficLightContext(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
