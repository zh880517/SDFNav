using System.Collections.Generic;
using UnityEngine;

public class MovePath
{
    public List<Vector2> Path = new List<Vector2>();
    public int NodeIndex;
    public bool NeedSmoothPath;//是否需要平滑路径
}