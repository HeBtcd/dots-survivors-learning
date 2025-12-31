using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace TMG.Survivors
{
    public struct EnemySpawnData : IComponentData
    {
        public Entity EnemyPrefab;
        public float SpawnInterval;
        public float SpawnDistance;
    }

    public struct EnemySpawnState : IComponentData
    {
        public float SpawnTimer;
        public Random Random;
    }
    
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public float spawnInterval;
        public float spawnDistance;
        public uint randomSeed;
            
        private class Baker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnData
                {
                    EnemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                    SpawnInterval = authoring.spawnInterval,
                    SpawnDistance = authoring.spawnDistance,
                });
                AddComponent(entity, new EnemySpawnState
                {
                    SpawnTimer = authoring.spawnInterval,
                    Random = new Random(authoring.randomSeed)
                });
            }
        }
    }

    public partial struct EnemySpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var playerEntity   = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.xy;

            var deltaTime = SystemAPI.Time.DeltaTime;   
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach (var (spawnState, spawnData) in SystemAPI.Query<RefRW<EnemySpawnState>, RefRO<EnemySpawnData>>())
            {
                spawnState.ValueRW.SpawnTimer -= deltaTime;

                if (spawnState.ValueRO.SpawnTimer > 0) continue;
                
                spawnState.ValueRW.SpawnTimer = spawnData.ValueRO.SpawnInterval;
                var newEnemy      = ecb.Instantiate(spawnData.ValueRO.EnemyPrefab);
                var spawnAngle = spawnState.ValueRW.Random.NextFloat(0f, math.TAU);
                var spawnPoint = new float2
                {
                    x = math.cos(spawnAngle),
                    y = math.sin(spawnAngle)
                };
                spawnPoint *= spawnData.ValueRO.SpawnDistance;
                spawnPoint += playerPosition;
                
                ecb.SetComponent(newEnemy, LocalTransform.FromPosition(new float3(spawnPoint, 0f)));
            }
        }
    }
}