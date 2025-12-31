using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace TMG.Survivors
{
    public struct GemTag : IComponentData
    {
    }

    public class GemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<GemAuthoring>
        {
            public override void Bake(GemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GemTag());
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }

    public partial struct CollectGemSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<GemTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var collectGemJob = new CollectGemJob
            {
                GemLookup           = SystemAPI.GetComponentLookup<GemTag>(true),
                GemsCollectedLookup = SystemAPI.GetComponentLookup<GemsCollectedCount>(),
                DestroyEntityLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>(),
                UpdateGemUiLookup   = SystemAPI.GetComponentLookup<UpdateGemUiFlag>(),
            };
            
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>(); 
            state.Dependency = collectGemJob.Schedule(simulationSingleton, state.Dependency);
        }
    }

    [BurstCompile]
    public struct CollectGemJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<GemTag> GemLookup;
        public ComponentLookup<GemsCollectedCount> GemsCollectedLookup;
        public ComponentLookup<DestroyEntityFlag> DestroyEntityLookup;
        public ComponentLookup<UpdateGemUiFlag> UpdateGemUiLookup;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity gemEntity;
            Entity playerEntity;

            if (GemLookup.HasComponent(triggerEvent.EntityA) &&
                GemsCollectedLookup.HasComponent(triggerEvent.EntityB))
            {
                gemEntity    = triggerEvent.EntityA;
                playerEntity = triggerEvent.EntityB;
            }
            else if (GemLookup.HasComponent(triggerEvent.EntityB) &&
                     GemsCollectedLookup.HasComponent(triggerEvent.EntityA))
            {
                gemEntity    = triggerEvent.EntityB;
                playerEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            var gemsCollected = GemsCollectedLookup[playerEntity];
            gemsCollected.Value++;
            GemsCollectedLookup[playerEntity] = gemsCollected;
            
            UpdateGemUiLookup.SetComponentEnabled(playerEntity, true);
            
            DestroyEntityLookup.SetComponentEnabled(gemEntity, true);
        }
    }
}