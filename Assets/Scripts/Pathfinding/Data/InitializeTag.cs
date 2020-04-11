using Unity.Entities;

public struct InitializeGridTag : IComponentData { public bool Value; }
public struct FindingPathTag : IComponentData { public bool Value; }
public struct SetupPathfindingTag : IComponentData { public bool Value; }