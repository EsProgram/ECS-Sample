using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Es.EcsJobSystem.Sample._02
{
    // IJobProcessComponentDataを実装する際にジェネリックパラメータには
    // ISharedComponentDataを指定できない。IComponentDataを実装したDataのみ指定できる。
    // そもそもJobのフィールドに共有用のフィールドが置けるので、ISharedComponentDataは指定できる必要がない。
    // SpeedDataをMoveRotateJobのフィールドとして入力するようにしても良いが、今回のサンプルでは説明上の都合で
    // IComponentDataを実装したDataとしてJobに渡すようにする。
    public struct SpeedData : IComponentData
    {
        public float Value;
        public SpeedData(float value)
        {
            Value = value;
        }
    }

    public struct SampleGroup
    {
        public ComponentDataArray<Position> postion;
        public ComponentDataArray<Rotation> rotation;

        [ReadOnly]
        public ComponentDataArray<SpeedData> speed;
        public int Length;
    }

    // 移動と回転処理を行うJobを定義。
    // IJobProcessComponentDataを実装することで、Genericパラメータに指定したDataを
    // 対象とするJobを定義することができる。(依存性の解決はIJobProcessComponentData.Scheduleで行なってくれる)
    struct MoveRotateJob : IJobProcessComponentData<Position, Rotation, SpeedData>
    {
        public float deltaTime;

        public void Execute(ref Position position, ref Rotation rotation, [ReadOnly] ref SpeedData speed)
        {
            var newPos = position;
            newPos.Value.y -= speed.Value * deltaTime;
            position = newPos;

            var newRot = rotation;
            newRot.Value = math.mul(math.normalize(newRot.Value), math.axisAngle(math.up(), speed.Value * deltaTime));
            rotation = newRot;
        }
    }

    // Injectが不要になっていることに注目
    public class SampleSystem : JobComponentSystem
    {
        // SystemではJobを生成し、ScheduleしてJobHandleを返す
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new MoveRotateJob()
            {
                deltaTime = Time.deltaTime
            };
            // IJobProcessComponentData(より詳しくはIBaseJobProcessComponentData)
            // を実装するJobは、拡張メソッドとして、第一引数にJobComponentSystem
            // (より詳しくはComponentSystemBase)の実装を取るScheduleが定義されている。
            // このJobのインスタンスにはIJobProcessComponentDataで指定した型データと
            // Systemのインスタンスの情報が入力されたことになる。
            var handle = job.Schedule(this, 32, inputDeps);
            return handle;
        }
    }

    public class EcsJobSystemSample02 : MonoBehaviour
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
                    entityManager.SetComponentData(entity, new SpeedData(Random.Range(5, 20)));
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