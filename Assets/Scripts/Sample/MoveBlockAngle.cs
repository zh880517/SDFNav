using UnityEngine;
using System.Collections.Generic;

public class MoveBlockAngle
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

    public void AddRange(float min, float max)
    {
        if (min < -180)
        {
            AddToRange(min + 360, 180, RightRange);
            AddToRange(-180, max, LeftRange);
            return;
        }
        if (max > 180)
        {
            AddToRange(-180, max - 360,LeftRange);
            AddToRange(min, 180, RightRange);
        }
        if (min < 0 && max > 0)
        {
            AddToRange(min, 0, LeftRange);
            AddToRange(0, max, RightRange);
        }
        else if (max < 0)
        {
            AddToRange(min, max, LeftRange);
        }
        else
        {
            AddToRange(min, max, RightRange);
        }
    }

    private void AddToRange(float min, float max, List<Range> range)
    {
        int insertIdx = 0;
        for (int i=0; i<range.Count; ++i)
        {
            var r = range[i];
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
            range[i] = r;
            return;
        }
        range.Insert(insertIdx, new Range { Min = min, Max = max });
    }
}
