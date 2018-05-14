using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

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
    // 落下速度
    private float delta = 0.1f;

    // Inject属性で要求するグループを指定できる
    [Inject] private SampleGroup sampleGroup;

    private Sample sample;

    // Systemが毎フレーム呼び出す処理
    protected override void OnUpdate ()
    {
        for (int i = 0; i < sampleGroup.Length; i++)
        {
            var newPos = sampleGroup.postion[i];
            newPos.Value.y -= delta;
            sampleGroup.postion[i] = newPos;

            // ここに記述しても描画を行える
            // if(sample == null)
            //     sample = GameObject.FindObjectOfType<Sample>();
            // Graphics.DrawMesh(sample.mesh, newPos.Value, sampleGroup.rotation[i].Value, sample.material, 0);
        }
    }
}

//=================================================================================================/
// ECSを利用するサンプルクラス
//=================================================================================================/
public class Sample : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int createEntityPerFrame = 100;

    private EntityManager entityManager;
    private EntityArchetype snowArchetype;

    [RuntimeInitializeOnLoadMethod (RuntimeInitializeLoadType.AfterSceneLoad)]
    private void Start ()
    {
        // Entityの管理者を取得
        entityManager = World.Active.GetOrCreateManager<EntityManager> ();

        // アーキタイプ(EntityがもつDataタイプの配列)の登録
        snowArchetype = entityManager.CreateArchetype (
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
                var snowEntity = entityManager.CreateEntity (snowArchetype);

                // GPU Instancingを行う場合にはMeshInstanceRendererが使える
                // 管理者にさっき生成したEntityに対して、各Entity間で共有できるDataを登録してもらう
                // MeshInstanceRendererはISharedComponentDataを実装している
                // entityManager.SetSharedComponentData (snowEntity, new MeshInstanceRenderer
                // {
                //     mesh = snowMesh,
                //     material = snowMaterial,
                // });

                // 管理者にさっき生成したEntityに対して、コンポーネントを登録してもらう
                // PositionはIComponentDataを継承している
                entityManager.SetComponentData (snowEntity, new Position
                {
                    Value = new float3 (Random.Range (-20.0f, 20.0f), 20, Random.Range (-20.0f, 20.0f))
                });
                entityManager.SetComponentData (snowEntity, new Rotation
                {
                    Value = Quaternion.Euler(0f, Random.Range(0.0f, 180.0f), 90f)
                });
            }
        }

        // GPU Instancingを利用しないで描画を行う
        // エンティティの Position / Rotation を取得しつつメッシュを描画
        foreach(var entity in entityManager.GetAllEntities())
        {
            var position = entityManager.GetComponentData<Position>(entity);
            var rotation = entityManager.GetComponentData<Rotation>(entity);
            Graphics.DrawMesh(mesh, position.Value, rotation.Value, material, 0);
        }
    }
}