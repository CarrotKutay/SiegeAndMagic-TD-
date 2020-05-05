using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public class ConvertPaladinPrefabToEntity : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject PrefabGameObject;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {

        //Entity prefabEntity = conversionSystem.GetPrimaryEntity(PrefabGameObject);
        Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(PrefabGameObject, conversionSystem.ForkSettings(1));
        dstManager.SetName(prefabEntity, PrefabGameObject.name);
        dstManager.AddComponentData(prefabEntity, new PaladinUnit { Value = true });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(PrefabGameObject);
    }
}

public struct EntitySpawnJob : IJob
{
    public EntityCommandBuffer.Concurrent ECS_Concurrent;
    public Entity EntityPrefab;

    public void Execute()
    {
        UnityEngine.Debug.Log("Spawning Entity Object");
        ECS_Concurrent.Instantiate(0, EntityPrefab);
    }
}
