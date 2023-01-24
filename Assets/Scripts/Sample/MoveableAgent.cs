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
    public bool Enable;
    public MoveType Type;
    public Vector2 StraightDir;
    //移动信息
    public bool IsMoving;
    public Vector2 MoveDir; 
}
