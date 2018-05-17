using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Es.EcsJobSystem.Sample._03
{
    public struct SpeedData : ISharedComponentData
    {
        public float Value;
        public SpeedData(float value)
        {
            Value = value;
        }
    }

    public struct SampleGroup
    {
        public ComponentDataArray<Position> position;
        public ComponentDataArray<Rotation> rotation;
        [ReadOnly]
        public SharedComponentDataArray<SpeedData> speed;
        public int Length;
    }

    struct MoveRotateJob : IJobParallelFor
    {
        public ComponentDataArray<Position> position;
        public ComponentDataArray<Rotation> rotation;
        public SharedComponentDataArray<SpeedData> speed;
        public float deltaTime;

        public void Execute(int i)
        {
            var newPos = position[i];
            newPos.Value.y -= speed[i].Value * deltaTime;
            position[i] = newPos;

            var newRot = rotation[i];
            newRot.Value = math.mul(math.normalize(newRot.Value), math.axisAngle(math.up(), speed[i].Value * deltaTime));
            rotation[i] = newRot;
        }
    }

    // Entityからデータを得る作業を並列化するためのJob
    struct GetDataJob : IJobParallelFor
    {
        public NativeArray<Position> position;
        public NativeArray<Rotation> rotation;
        [ReadOnly]
        public NativeArray<Entity> entity;

        public void Execute(int i)
        {
            //! 静的データへのアクセス。
            //! 通常時に実行はできたが、Burstコンパイラで最適化をかけたら実行時エラーが出た。
            //! 今後静的解析でこういったことが禁止される可能性もあるので
            //! こういった方法はグレー。
            var entityManager = World.Active.GetOrCreateManager<EntityManager>();
            position[i] = entityManager.GetComponentData<Position>(entity[i]);
            rotation[i] = entityManager.GetComponentData<Rotation>(entity[i]);
        }
    }

    public class SampleSystem : JobComponentSystem
    {
        [Inject] private SampleGroup sampleGroup;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new MoveRotateJob()
            {
                position = sampleGroup.position,
                rotation = sampleGroup.rotation,
                speed = sampleGroup.speed,
                deltaTime = Time.deltaTime
            };
            var handle = job.Schedule(sampleGroup.Length, 32, inputDeps);
            return handle;
        }
    }

    public class EcsJobSystemSample03 : MonoBehaviour
    {
        public Mesh mesh;
        public Material material;
        public int createEntityPerFrame = 100;

        private EntityManager entityManager;
        private EntityArchetype archetype;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private void Start()
        {
            entityManager = World.Active.GetOrCreateManager<EntityManager>();

            archetype = entityManager.CreateArchetype(
                typeof(Position),
                typeof(Rotation),
                typeof(SpeedData)
            );
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                for (int i = 0; i < createEntityPerFrame; i++)
                {
                    var entity = entityManager.CreateEntity(archetype);

                    entityManager.SetComponentData(entity, new Position
                    {
                        Value = new float3(Random.Range(-20, 20), 20, Random.Range(-20, 20))
                    });
                    entityManager.SetComponentData(entity, new Rotation
                    {
                        Value = Quaternion.Euler(0, Random.Range(0, 180), 90)
                    });
                    entityManager.SetSharedComponentData(entity, new SpeedData(Random.Range(5, 20)));
                }
            }

            // TODO:素直にこのJob(Entityいらないver)をMoveRotateJobに連結したらどうなる？
            var entities = entityManager.GetAllEntities();
            var job = new GetDataJob()
            {
                position = new NativeArray<Position>(entities.Length, Allocator.Temp),
                rotation = new NativeArray<Rotation>(entities.Length, Allocator.Temp),
                entity = entities
            };
            var jobHandle = job.Schedule(entities.Length, 32);
            jobHandle.Complete();

            for (int i = 0; i < entities.Length; ++i)
                Graphics.DrawMesh(mesh, job.position[i].Value, job.rotation[i].Value, material, 0);
            job.position.Dispose();
            job.rotation.Dispose();
            entities.Dispose();
        }
    }
}