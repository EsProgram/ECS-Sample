using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Es.EcsJobSystem.Sample._04.Data;

namespace Es.EcsJobSystem.Sample._04.System
{
    [UpdateAfter(typeof(MoveRotateSystem))]
    public class DrawMeshSystem : ComponentSystem
    {
        public struct DrawMeshGroup
        {
            [ReadOnly]
            public SharedComponentDataArray<DrawMesh> drawMesh;
            public ComponentDataArray<Position> position;
            public ComponentDataArray<Rotation> rotation;
            public int Length;
        }

        [Inject]
        DrawMeshGroup drawMeshGroup;

        protected override void OnUpdate()
        {
            for(int i = 0; i < drawMeshGroup.Length; ++i)
            {
                Graphics.DrawMesh(drawMeshGroup.drawMesh[i].mesh,
                                  drawMeshGroup.position[i].Value,
                                  drawMeshGroup.rotation[i].Value,
                                  drawMeshGroup.drawMesh[i].material,
                                  0
                );
            }
        }
    }
}
