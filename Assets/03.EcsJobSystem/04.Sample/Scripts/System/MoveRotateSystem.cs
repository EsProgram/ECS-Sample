using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Es.EcsJobSystem.Sample._04.Data;

namespace Es.EcsJobSystem.Sample._04.System
{
    public class MoveRotateSystem : JobComponentSystem
    {
        public struct MoveRotateGroup
        {
            public ComponentDataArray<Position> position;
            public ComponentDataArray<Rotation> rotation;
            public ComponentDataArray<Speed> speed;
            public int Length;
        }

        public struct MoveRotateJob : IJobParallelFor
        {
            public MoveRotateGroup moveRotateGroup;
            public float deltaTime;

            public void Execute(int i)
            {
                var pos = moveRotateGroup.position[i];
                pos.Value.y -= moveRotateGroup.speed[i].Value * deltaTime;
                moveRotateGroup.position[i] = pos;

                var rot = moveRotateGroup.rotation[i];
                rot.Value = math.mul(math.normalize(rot.Value), math.axisAngle(math.up(), moveRotateGroup.speed[i].Value * deltaTime));
                moveRotateGroup.rotation[i] = rot;
            }
        }

        [Inject]
        private MoveRotateGroup moveRotateGroup;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new MoveRotateJob()
            {
                moveRotateGroup = moveRotateGroup,
                deltaTime = Time.deltaTime
            };
            return job.Schedule(moveRotateGroup.Length, 32, inputDeps);
        }
    }
}