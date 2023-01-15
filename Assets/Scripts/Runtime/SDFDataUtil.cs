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
