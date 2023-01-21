using UnityEngine;

namespace SDFNav
{
    public static class SDFDataUtil
    {
        public static bool CheckStraightMove(this SDFData data, Vector2 from, Vector2 to, float moveRadius)
        {
            Vector2 diff = to - from;
            float distance = diff.magnitude;
            if (distance <= 1E-05f)
                return true;

            diff /= distance;

            float t = Mathf.Min(data.Grain, moveRadius);

            while (true)
            {
                if (data.Sample(from + diff * t) < moveRadius)
                    return false;
                t += data.Grain;
                if (t >= distance)
                    return true;
            }
        }
        //距离最近障碍物的方向
        public static Vector2 Gradiend(this SDFData data, Vector2 point)
        {
            float delta = 1;
            Vector2 v1 = new Vector2(point.x + delta, point.y);
            Vector2 v2 = new Vector2(point.x - delta, point.y);
            Vector2 v3 = new Vector2(point.x, point.y + delta);
            Vector2 v4 = new Vector2(point.x, point.y - delta);
            return 0.5f * new Vector2(data.Sample(v1) - data.Sample(v2),
                data.Sample(v3) - data.Sample(v4));
        }

        public static Vector2 FindNearestValidPoint(this SDFData data, Vector2 point, float radius)
        {
            Vector2 newPos = point;
            for (int i=0; i<3; ++i)
            {
                float sdf = data.Sample(newPos);
                if (sdf >= radius)
                   break;
                newPos += Gradiend(data, newPos).normalized * (radius - sdf);
            }
            return newPos;

        }

        public static float DiskCast(this SDFData data, Vector2 origin, Vector2 dir, float radius, float maxDistance)
        {
            float t = 0;
            while (true)
            {
                Vector2 p = origin + dir * t;
                float sd = data.Sample(p);
                if (sd <= radius)
                    return t;
                t += (sd - radius);
                if (t >= maxDistance)
                    return maxDistance;
            }
        }
    }
}
