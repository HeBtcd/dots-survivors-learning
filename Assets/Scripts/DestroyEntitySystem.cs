using Unity.Burst;
using Unity.Entities;

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
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var endEcb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach (var (_, entity) in SystemAPI.Query<RefRO<DestroyEntityFlag>>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    GameUIController.Instance.ShowGameOverUI();
                }
                
                endEcb.DestroyEntity(entity);
            }
        }
    }
}