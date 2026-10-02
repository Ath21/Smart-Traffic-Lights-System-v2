using System;

namespace TrafficLightControllerStore.Domain;

public class TrafficLightContext
    {
    public int Id { get; }
    public string Name { get; }

    public TrafficLightContext(int id, string name)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

