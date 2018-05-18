using Unity.Entities;

namespace Es.EcsJobSystem.Sample._04.Data
{
    public struct Speed : IComponentData
    {
        public float Value;
        public Speed(float value)
        {
            Value = value;
        }
    }
}