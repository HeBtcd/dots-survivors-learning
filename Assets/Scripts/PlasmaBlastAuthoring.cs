using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace TMG.Survivors
{
    public struct PlasmaBlastData : IComponentData
    {
        public float MoveSpeed;
        public int AttackDamage;
    }
    
    public class PlasmaBlastAuthoring : MonoBehaviour
    {
        public float moveSpeed;
        public int attackDamage;
        
        private class Baker : Baker<PlasmaBlastAuthoring>
        {
            public override void Bake(PlasmaBlastAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlasmaBlastData
                {
                    MoveSpeed = authoring.moveSpeed,
                    AttackDamage = authoring.attackDamage
                });
            }
        }
    }
    
    public partial struct MovePlasmaBlastSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlasmaBlastData>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, data) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlasmaBlastData>>())
            {
                transform.ValueRW.Position += data.ValueRO.MoveSpeed * transform.ValueRO.Right() * deltaTime;
            }
        }
    }
}