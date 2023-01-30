using UnityEngine;
namespace SDFNav
{
    public struct NeighborAgentInfo
    {
        public int ID;
        public Vector2 Direction;//相对自己的方向
        public float Radius;
        public float Distance;//圆心距离
        public Vector2 MoveDirection;//移动方向
        public float MoveDistance;
    }
}