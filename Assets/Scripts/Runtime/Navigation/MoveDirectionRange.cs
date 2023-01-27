using UnityEngine;
using System.Collections.Generic;
namespace SDFNav
{
    public class MoveDirectionRange
    {
        struct Range
        {
            public float Min;
            public float Max;
        }

        private List<Range> LeftRange = new List<Range>();
        private List<Range> RightRange = new List<Range>();

        public void Clear()
        {
            LeftRange.Clear();
            RightRange.Clear();
        }

        public float GetMinOffsetAngle()
        {
            float rightAngle = 0;
            foreach (var range in RightRange)
            {
                if (range.Min > rightAngle)
                    break;
                rightAngle = range.Max;
            }
            float leftAngle = 0;
            foreach (var range in LeftRange)
            {
                if (range.Max < leftAngle)
                    break;
                leftAngle = range.Min;
            }
            if (-leftAngle > rightAngle)
                return rightAngle;
            return leftAngle;
        }

        public void AddAngle(float angle, float offset)
        {
            float min = angle - offset;
            float max = angle + offset;
            if (angle <= 0)
            {
                AddToLeft(Mathf.Max(min, -180), Mathf.Min(max, 0));
                if (min < -180)
                {
                    AddToRight(360 + min, 180);
                }
                if (max > 0)
                {
                    AddToRight(0, max);
                }
            }
            else
            {
                AddToRight(Mathf.Max(min, 0), Mathf.Max(max, 180));
                if (min < 0)
                {
                    AddToLeft(min, 0);
                }
                if (max > 180)
                {
                    AddToLeft(-180, max - 360);
                }
            }
        }

        private void AddToLeft(float min, float max)
        {
            int insertIdx = 0;
            for (int i = 0; i < LeftRange.Count; ++i)
            {
                var r = LeftRange[i];
                if (r.Max < min)
                {
                    insertIdx = i;
                    break;
                }
                if (max < r.Min)
                {
                    insertIdx++;
                    continue;
                }
                r.Min = Mathf.Min(min, r.Min);
                r.Max = Mathf.Max(max, r.Max);
                LeftRange[i] = r;
                return;
            }
            LeftRange.Insert(insertIdx, new Range { Min = min, Max = max });
        }
        private void AddToRight(float min, float max)
        {
            int insertIdx = 0;
            for (int i = 0; i < RightRange.Count; ++i)
            {
                var r = RightRange[i];
                if (r.Min > max)
                {
                    insertIdx = i;
                    break;
                }
                if (min > r.Max)
                {
                    insertIdx++;
                    continue;
                }
                r.Min = Mathf.Min(min, r.Min);
                r.Max = Mathf.Max(max, r.Max);
                RightRange[i] = r;
                return;
            }
            RightRange.Insert(insertIdx, new Range { Min = min, Max = max });
        }

    }
}