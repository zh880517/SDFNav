using UnityEngine;
namespace SDFNav
{
    public static class HexGridUtil
    {
        //内径：中心到边的距离
        //外径：中心到角的距离，也是边长
        /// 内径/外径  3^0.5 / 4 
        public const float OutterToInner = 0.8660254037844386f;
        /// 外径/内径  4 / 3^0.5 
        public const float InnerToOutter = 1.154700538379252f;
        private static readonly float Sqrt3 = Mathf.Sqrt(3);

        public static Vector2 HexCenterToPos(int x, int y, float inner)
        {
            return new Vector2(x*inner*2 + (y%2)*inner, y*inner*InnerToOutter*0.5f);
        }

        public static Vector2Int PosToHexGrid(float x, float y)
        {
            float q = (Sqrt3 / 3 * x) - (1 / 3 * y);
            float r = 2f / 3 * y;
            float s = -q - r;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);
            float q_diff = Mathf.Abs(rq - q);
            float r_diff = Mathf.Abs(rr - r);
            float s_diff = Mathf.Abs(rs - s);
            if (q_diff > r_diff && q_diff > s_diff)
                rq = -rr - rs;
            else if (r_diff > s_diff)
                rr = -rq - rs;
            return new Vector2Int(rq, rr);
        }
        public static Vector2Int ToViewCoordiante(this Vector3Int logicCoordiante)
        {
            int x =(logicCoordiante.x - logicCoordiante.y) / 2;
            int y = logicCoordiante.z;
            return new Vector2Int(x, y);
        }
    }
}