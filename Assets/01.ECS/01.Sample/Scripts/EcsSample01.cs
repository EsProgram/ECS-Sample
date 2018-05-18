using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Es.Ecs.Sample._01
{
    // 独自のDataを定義する場合、IComponentDataかISharedComponentDataを実装します。
    // IComponentDataは、座標情報などのEntityごとに異なるデータに適しています。
    // ISharedComponentDataは、多くのEntityに共通するものがある場合に適しています。
    public struct SpeedData : ISharedComponentData
    {
        public float Value;
        public SpeedData(float value)
        {
            Value = value;
        }
    }

    // Group(Systemに渡されるEntityの纏まり。つまり要求されるデータの配列のようなもの)を定義。
    // IComponentDataかISharedComponentDataを実装したDataがSystemに要求されるデータになります。
    // Lengthには要求するDataを持つEntityの数が格納されます。
    public struct SampleGroup
    {
        // ComponentDataArrayはNativeContainer属性が付加されているので
        // Thread間でデータを共有できます。
        public ComponentDataArray<Position> postion;
        public ComponentDataArray<Rotation> rotation;

        // SharedComponentDataArrayはReadOnlyを指定しないとエラーになります。
        // SharedComponentDataArrayはEntity間で共通の値であり、NativeContainerではないため、
        // 値の代入行為が不適切であるからです。
        // SharedComponentDataArrayは、Systemで計算に使う値を格納する用途で使います。
        [ReadOnly]
        public SharedComponentDataArray<SpeedData> speed;
        public int Length;
    }

    // ComponentSystemを継承したクラスを作ることで
    // GroupがEntityの持つ型と一致する場合に処理を実行するSystemを作ることができる。
    public class SampleSystem : ComponentSystem
    {
        // Inject属性で要求するグループを指定する
        // (Systemに特定のDataへの依存性を注入する)
        [Inject] private SampleGroup sampleGroup;

        float deltaTime;

        // Systemが毎フレーム呼び出す処理
        protected override void OnUpdate()
        {
            deltaTime = Time.deltaTime;

            for (int i = 0; i < sampleGroup.Length; i++)
            {
                // 落下させる
                var newPos = sampleGroup.postion[i];
                newPos.Value.y -= sampleGroup.speed[i].Value * deltaTime;
                sampleGroup.postion[i] = newPos;

                // 回転させる
                var newRot = sampleGroup.rotation[i];
                newRot.Value = math.mul(math.normalize(newRot.Value), math.axisAngle(math.up(), sampleGroup.speed[i].Value * deltaTime));
                sampleGroup.rotation[i] = newRot;
            }
        }
    }

    // ECSを利用するサンプルクラス。
    // JobSystemを利用していないため、MainThreadで動く。
    // このサンプルでは、大量のMeshの移動と回転を行い、描画する。
    public class EcsSample01 : MonoBehaviour
    {
        public Mesh mesh;
        public Material material;
        public int createEntityPerFrame = 100;

        private EntityManager entityManager;
        private EntityArchetype archetype;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private void Start()
        {
            // Entityの管理者を取得
            entityManager = World.Active.GetOrCreateManager<EntityManager>();

            // アーキタイプ(EntityがもつDataタイプの配列)の登録
            archetype = entityManager.CreateArchetype(
                typeof(Position), // Unity.Transformでデフォルトで定義してくれている「位置」を表すData
                typeof(Rotation), // Unity.Transformでデフォルトで定義してくれている「回転」を表すData
                typeof(SpeedData) // 独自定義した「微小な値」を表すData
            );
        }

        private void Update()
        {
            // Spaceキーが押さていたらMeshを生成
            if (Input.GetKey(KeyCode.Space))
            {
                for (int i = 0; i < createEntityPerFrame; i++)
                {
                    // 管理者にEntityの生成と管理をお願いする
                    var entity = entityManager.CreateEntity(archetype);

                    // 生成したEntityに対して、Dataを登録してもらう
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

            //=================================================================================================/
            // HACK:
            //    本来であれば Mesh / Position / Rotation / Material を持つEntityを
            //    描画するようなSystemを作るべきですが、サンプルコードが冗長になるためここに描画処理を書いてあります。
            //=================================================================================================/
            // DrawMeshで描画を行う
            // エンティティの Position / Rotation を取得しつつメッシュを描画
            var entities = entityManager.GetAllEntities();
            foreach (var entity in entities)
            {
                var position = entityManager.GetComponentData<Position>(entity);
                var rotation = entityManager.GetComponentData<Rotation>(entity);
                Graphics.DrawMesh(mesh, position.Value, rotation.Value, material, 0);
            }
            // GetAllEntitiesで取得したNativeArrayは明示的に破棄する。
            // また、GetAllEntityではAllocatorを指定できるが、デフォルトのTempだと
            // フレームをまたいで生存しているとメモリリークするので注意。
            entities.Dispose();
        }
    }
}