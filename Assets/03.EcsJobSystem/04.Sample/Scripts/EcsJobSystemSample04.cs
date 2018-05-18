using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Es.EcsJobSystem.Sample._04.Data;

namespace Es.EcsJobSystem.Sample._04
{
    public class EcsJobSystemSample04 : MonoBehaviour
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
                typeof(Speed),
                typeof(DrawMesh)
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
                    entityManager.SetComponentData(entity, new Speed(Random.Range(5, 20)));
                    entityManager.SetSharedComponentData(entity, new DrawMesh(mesh, material));
                }
            }
        }
    }
}