using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace TMG.Survivors
{
    public struct PlayerTag : IComponentData
    {
        
    }

    public struct CameraTarget : IComponentData
    {
        public UnityObjectRef<Transform> CameraTransform;
    }

    public struct InitializeCameraTargetTag : IComponentData
    {
        
    }
    
    [MaterialProperty("_AnimationIndex")]
    public struct AnimationIndexOverride : IComponentData
    {
        public float Value;
    }

    public enum PlayerAnimationIndex : byte
    {
        Movement = 0,
        Idle = 1,
        
        None = byte.MaxValue
    }

    public struct PlayerAttackData : IComponentData
    {
        public Entity AttackPrefab;
        public float CooldownTime;
    }

    public struct PlayerCooldownExpirationTimestamp : IComponentData
    {
        public double Value;
    }
    
    public class PlayerAuthoring : MonoBehaviour
    {
        public GameObject attackPrefab;
        public float cooldownTime;
        
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
                AddComponent<InitializeCameraTargetTag>(entity);
                AddComponent<CameraTarget>(entity);
                AddComponent<AnimationIndexOverride>(entity);
                AddComponent(entity , new PlayerAttackData
                {
                    AttackPrefab = GetEntity(authoring.attackPrefab, TransformUsageFlags.Dynamic),
                    CooldownTime = authoring.cooldownTime
                });
                AddComponent<PlayerCooldownExpirationTimestamp>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CameraInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeCameraTargetTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!CameraTargetSingleton.Instance) return;
            var cameraTargetTransform = CameraTargetSingleton.Instance.transform;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (cameraTarget, entity) in SystemAPI.Query<RefRW<CameraTarget>>().WithAll<InitializeCameraTargetTag, PlayerTag>().WithEntityAccess())
            {
                cameraTarget.ValueRW.CameraTransform = cameraTargetTransform;
                ecb.RemoveComponent<InitializeCameraTargetTag>(entity);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct MoveCameraSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, cameraTarget) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<CameraTarget>>().WithAll<PlayerTag>().WithNone<InitializeCameraTargetTag>())
            {
                cameraTarget.ValueRW.CameraTransform.Value.position = transform.ValueRO.Position;
            }
        }
    }
    
    public partial class PlayerInputSystem : SystemBase
    {
        private SurvivorsInput _input;

        protected override void OnCreate()
        {
            _input = new SurvivorsInput();
            _input.Enable();
        }

        protected override void OnUpdate()
        {
            var currentInput = (float2)_input.Player.Move.ReadValue<Vector2>();
            foreach (var direction in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<PlayerTag>())
            {
                direction.ValueRW.Value = currentInput;
            }
        }
    }

    public partial struct PlayerAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (expirationTimestamp, attackData, transform) 
                     in SystemAPI.Query<RefRW<PlayerCooldownExpirationTimestamp>, RefRO<PlayerAttackData>, RefRO<LocalTransform>>())
            {
                if (expirationTimestamp.ValueRO.Value > elapsedTime) continue;
                
                var spawnPosition = transform.ValueRO.Position;
                var newAttack = ecb.Instantiate(attackData.ValueRO.AttackPrefab);
                ecb.SetComponent(newAttack, LocalTransform.FromPosition(spawnPosition));
                
                expirationTimestamp.ValueRW.Value = elapsedTime + attackData.ValueRO.CooldownTime;
            }
        }
    }
}