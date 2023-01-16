using UnityEngine;

namespace SDFNav.ORCA
{
    public class Obstacle
    {
        public Obstacle next_;
        public Obstacle previous_;
        public Vector2 direction_;
        public Vector2 point_;
        public bool convex_;
    }
}
