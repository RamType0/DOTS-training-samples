using Unity.Entities;

namespace MetroECS.Trains
{
    public struct Train : IComponentData
    {
        public const float MAX_SPEED = 0.01f;
        // public const float MAX_SPEED = 0.1f;
        
        public int ID;
        public float Position;
        public float deltaPos;
        public int TargetIndex;
        public int CarriageCount;
        public Entity Path;
    }
}