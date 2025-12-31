using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace TMG.Survivors
{
    public struct DestroyEntityFlag : IComponentData, IEnableableComponent
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct DestroyEntitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var endEcb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var beginEcb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<DestroyEntityFlag>>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    GameUIController.Instance.ShowGameOverUI();
                }

                if (SystemAPI.HasComponent<GemPrefab>(entity))
                {
                    var gemPrefab = SystemAPI.GetComponentRW<GemPrefab>(entity).ValueRW.Value;
                    beginEcb.Instantiate(gemPrefab);

                    var spawnPosition = SystemAPI.GetComponent<LocalToWorld>(entity).Position;
                    beginEcb.SetComponent(entity, LocalTransform.FromPosition(spawnPosition));
                }

                endEcb.DestroyEntity(entity);
            }
        }
    }
}