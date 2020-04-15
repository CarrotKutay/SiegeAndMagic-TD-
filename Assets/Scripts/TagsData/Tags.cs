using Unity.Entities;

public struct InitializeGridTag : IComponentData { public bool Value; }
public struct VisualPathDebugTag : IComponentData { public bool Value; }
public struct PerformingMovement : IComponentData { public bool Value; }