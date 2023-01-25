using UnityEngine;

namespace SDFNav
{
    public static class SDFDataUtil
    {
        public static bool CheckStraightMove(this SDFData data, Vector2 from, Vector2 to, float moveRadius)
        {
            Vector2 diff = to - from;
            float distance = diff.sqrMagnitude;
            if (distance <= 1E-05f)
                return true;
            distance = Mathf.Sqrt(distance);

            diff /= distance;

            float t = 0;

            while (true)
            {
                float sd = data.Sample(from + diff * t);
                float step = sd - moveRadius;
                if (step <= -0.001f)
                    return false;
                t += Mathf.Max(Mathf.Abs(step), 0.001f);
                if (t >= distance)
                    return true;
            }
        }
        //梯度：远离障碍物的方向
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
            for (int i=0; i<10; ++i)
            {
                float sdf = data.Sample(newPos);
                if (sdf >= radius)
                   break;
                float t = radius - sdf;
                if (t > 0)
                    t = Mathf.Max(t, 0.001f);

                newPos += Gradiend(data, newPos).normalized * t;
            }
            return newPos;

        }

        public static float TryMoveTo(this SDFData data, Vector2 origin, Vector2 dir, float radius, float maxDistance)
        {
            float sd = data.Sample(origin);
            if (sd < 0)
                return 0;
            float t = 0;
            if (radius > sd)
                radius = sd;
            while (true)
            {
                Vector2 p = origin + dir * (t + 0.001f);
                sd = data.Sample(p);
                float step = sd - radius;
                if (step <= -0.001f)
                    return t;
                t += Mathf.Max(Mathf.Abs(step), 0.001f);
                if (t >= maxDistance)
                    return maxDistance;
            }
        }

        public static float DiskCast(this SDFData data, Vector2 origin, Vector2 dir, float radius, float maxDistance)
        {
            float sdStart = data.Sample(origin);
            if (sdStart < 0)
                return 0;
            float t = 0;
            if (sdStart <= radius)
            {
                //这里是为了防止起点与边界的距离过短导致直接返回失败，如果需要限制起点的范围，需要额外处理
                t = radius - sdStart;
            }
            while (true)
            {
                Vector2 p = origin + dir * (t + 0.001f);
                float sd = data.Sample(p);
                float step = sd - radius;
                if (step <= -0.001f)
                    return t;
                t += Mathf.Max(Mathf.Abs(step), 0.001f);
                if (t >= maxDistance)
                    return maxDistance;
            }
        }
    }
}
