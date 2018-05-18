using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Es.EcsJobSystem.Sample._01
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

    // 移動と回転処理を行うJobを定義。
    // Job内で宣言が可能なのはNativeContainer及びBlittable型のみなことに注意
    struct MoveRotateJob : IJobParallelFor
    {
        public SampleGroup sampleGroup;
        public float deltaTime;

        public void Execute(int i)
        {
            var newPos = sampleGroup.position[i];
            newPos.Value.y -= sampleGroup.speed[i].Value * deltaTime;
            sampleGroup.position[i] = newPos;

            var newRot = sampleGroup.rotation[i];
            newRot.Value = math.mul(math.normalize(newRot.Value), math.axisAngle(math.up(), sampleGroup.speed[i].Value * deltaTime));
            sampleGroup.rotation[i] = newRot;
        }
    }

    // JobComponentSystemは抽象クラス。
    // IJobProcessComponentDataを利用するとInjectが不要になったりと
    // スマートに書けるようになるが、本サンプルでは愚直にSystemをJob化する
    public class SampleSystem : JobComponentSystem
    {
        [Inject] private SampleGroup sampleGroup;

        // SystemではJobを生成し、ScheduleしてJobHandleを返す
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new MoveRotateJob()
            {
                sampleGroup = sampleGroup,
                deltaTime = Time.deltaTime
            };
            var handle = job.Schedule(sampleGroup.Length, 32, inputDeps);
            return handle;
        }
    }

    // ECS + JobSystemを利用するサンプルクラス。
    public class EcsJobSystemSample01 : MonoBehaviour
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
                    entityManager.SetSharedComponentData(entity, new SpeedData(10));
                }
            }

            var entities = entityManager.GetAllEntities();
            foreach (var entity in entities)
            {
                var position = entityManager.GetComponentData<Position>(entity);
                var rotation = entityManager.GetComponentData<Rotation>(entity);
                Graphics.DrawMesh(mesh, position.Value, rotation.Value, material, 0);
            }
            entities.Dispose();
        }
    }
}