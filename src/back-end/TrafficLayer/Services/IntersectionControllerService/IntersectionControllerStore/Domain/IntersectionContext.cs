using System;

namespace IntersectionControllerStore.Domain;

public class IntersectionContext
{
    public int Id { get; }
    public string Name { get; }
    public List<TrafficLightContext> Lights { get; }

    public IntersectionContext(int id, string name, List<TrafficLightContext>? lights = null)
    {
        Id = id;
        Name = name;
        Lights = lights ?? new List<TrafficLightContext>();
    }
}