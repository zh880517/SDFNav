using UnityEngine;

namespace SDFNav.ORCA
{
    public class Obstacle
    {
        public Obstacle Next;
        public Obstacle Previous;
        public Vector2 Direction;
        public Vector2 Point;
        public bool Convex;
    }
}
