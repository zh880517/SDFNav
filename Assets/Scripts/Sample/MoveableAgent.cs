using SDFNav;
using System.Collections.Generic;
using UnityEngine;

public enum MoveType
{
    None,
    Straight,
    Path,
}

public class MoveableAgent
{
    public int ID;
    public Vector2 Position;
    public float Radius;
    public float Speed;
    public MoveType Type;
    public Vector2 StraightDir;
    public NavPathMoveInfo NavPath = new NavPathMoveInfo();
    //移动信息
    public Vector2 MoveDir; 
}
