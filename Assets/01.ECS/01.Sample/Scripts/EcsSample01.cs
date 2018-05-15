using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Es.Ecs.Sample._01
{
    //=================================================================================================/
    // Groupを定義。
    // ComponentDataArrayは要求するコンポーネント(のポインタ)。
    // Lengthには要求するComponentDataを持つEntityの数が格納される。
    // IComponentDataを実装したデータが要求データとなる。
    //=================================================================================================/
    public struct SampleGroup
    {
        public ComponentDataArray<Position> postion;
        public ComponentDataArray<Rotation> rotation;
        public int Length;
    }

    //=================================================================================================/
    // ComponentSystemを継承したクラスを作ることで
    // GroupがEntityの持つ型と一致する場合に処理を実行するSystemを作ることができる。
    // EntityとGroupのもつDataが
    //=================================================================================================/
    public class SampleSystem : ComponentSystem
    {
        // Inject属性で要求するグループを指定できる
        [Inject] private SampleGroup sampleGroup;

        private EcsSample01 sample;

        // Systemが毎フレーム呼び出す処理
        //TODO: 外部の値を使わないようにする(SystemのoverrideメソッドではGroupの値のみを使って計算をするように)
        protected override void OnUpdate ()
        {
            if (sample == null)
                sample = GameObject.FindObjectOfType<EcsSample01> ();

            for (int i = 0; i < sampleGroup.Length; i++)
            {
                // 落下させる
                var newPos = sampleGroup.postion[i];
                newPos.Value.y -= sample.delta * Time.deltaTime;
                sampleGroup.postion[i] = newPos;

                // 回転させる
                var newRot = sampleGroup.rotation[i];
                newRot.Value = math.mul (math.normalize (newRot.Value), math.axisAngle (math.up (), sample.delta * Time.deltaTime));
                sampleGroup.rotation[i] = newRot;

                // 描画を行う
                Graphics.DrawMesh (sample.mesh, newPos.Value, newRot.Value, sample.material, 0);
            }
        }
    }

    //=================================================================================================/
    // ECSを利用するサンプルクラス
    //=================================================================================================/
    public class EcsSample01 : MonoBehaviour
    {
        public Mesh mesh;
        public Material material;
        public int createEntityPerFrame = 100;
        // 落下速度
        public float delta = 10f;

        private EntityManager entityManager;
        private EntityArchetype archetype;

        [RuntimeInitializeOnLoadMethod (RuntimeInitializeLoadType.AfterSceneLoad)]
        private void Start ()
        {
            // Entityの管理者を取得
            entityManager = World.Active.GetOrCreateManager<EntityManager> ();

            // アーキタイプ(EntityがもつDataタイプの配列)の登録
            archetype = entityManager.CreateArchetype (
                typeof (TransformMatrix),
                typeof (Position),
                typeof (Rotation)
                // GPU Instancingを利用できる場合に指定
                // typeof (MeshInstanceRenderer)
            );
        }

        private void Update ()
        {
            if (Input.GetKey (KeyCode.Space))
            {
                for (int i = 0; i < createEntityPerFrame; i++)
                {
                    // 管理者にEntityを生成して管理してもらう
                    var entity = entityManager.CreateEntity (archetype);

                    // GPU Instancingを行う場合にはMeshInstanceRendererが使える
                    // 管理者にさっき生成したEntityに対して、各Entity間で共有できるDataを登録してもらう
                    // MeshInstanceRendererはISharedComponentDataを実装している
                    // entityManager.SetSharedComponentData (entity, new MeshInstanceRenderer
                    // {
                    //     mesh = mesh,
                    //     material = material,
                    // });

                    // 管理者にさっき生成したEntityに対して、コンポーネントを登録してもらう
                    // PositionはIComponentDataを継承している
                    entityManager.SetComponentData (entity, new Position
                    {
                        Value = new float3 (Random.Range (-20.0f, 20.0f), 20, Random.Range (-20.0f, 20.0f))
                    });
                    entityManager.SetComponentData (entity, new Rotation
                    {
                        Value = Quaternion.Euler (0f, Random.Range (0.0f, 180.0f), 90f)
                    });
                }
            }

            // DrawMeshで描画を行う
            // エンティティの Position / Rotation を取得しつつメッシュを描画
            // var entities = entityManager.GetAllEntities();
            // foreach(var entity in entities)
            // {
            //     var position = entityManager.GetComponentData<Position>(entity);
            //     var rotation = entityManager.GetComponentData<Rotation>(entity);
            //     Graphics.DrawMesh(mesh, position.Value, rotation.Value, material, 0);
            // }
            // GetAllEntitiesで取得したNativeArrayは明示的に破棄する。
            // また、GetAllEntityではAllocatorを指定できるが、デフォルトのTempだと
            // フレームをまたいで生存しているとメモリリークするので注意。
            // entities.Dispose();
        }
    }
}