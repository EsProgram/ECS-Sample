using Unity.Entities;
using UnityEngine;

namespace Es.EcsJobSystem.Sample._04.Data
{
    public struct DrawMesh : ISharedComponentData
    {
        public Mesh mesh;
        public Material material;
        public DrawMesh(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
        }
    }
}