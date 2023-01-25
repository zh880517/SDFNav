using SDFNav;
using System.Collections.Generic;
using UnityEngine;

public class MoveManager
{
    public SDFData SDF = new SDFData();
    public float CloseDistance = 0.5f;
    public List<MoveableAgent> Agents = new List<MoveableAgent>();
    private MoveBlockAngle MoveBlock = new MoveBlockAngle();
    private int keyIndex;
    public MoveableAgent CreateAgent(Vector2 pos, float radius, float speed)
    {
        MoveableAgent agent = new MoveableAgent();
        agent.ID = ++keyIndex;
        pos = SDF.FindNearestValidPoint(pos, radius);
        agent.Position = pos;
        agent.Radius = radius;
        agent.Speed = speed;
        Agents.Add(agent);
        return agent;
    }

    public void UpdateMoveDir(float dt)
    {
        foreach (var agent in Agents)
        {
            agent.IsMoving = false;
            if (agent.Type == MoveType.None)
                continue;
            bool isMoving = agent.Type == MoveType.Straight;
            if (agent.Type == MoveType.Path && agent.Path.Count > 0)
            {
                Vector2 diff = agent.Path[agent.Path.Count - 1] - agent.Position;
                if (diff.sqrMagnitude < Sqr(agent.Speed * dt))
                {
                    agent.Path.RemoveAt(agent.Path.Count - 1);
                }
                if (agent.Path.Count >= 2)
                {
                    if (SDF.CheckStraightMove(agent.Position, agent.Path[agent.Path.Count - 2], agent.Radius))
                    {
                        agent.Path.RemoveAt(agent.Path.Count - 1);
                    }
                }
                else if(agent.Path.Count > 0)
                {
                    if (Vector2.Distance(agent.Position, agent.Path[0]) < agent.Speed * dt)
                    {
                        agent.Path.Clear();
                    }
                }
                if (agent.Path.Count > 0)
                {
                    isMoving = true;
                    agent.StraightDir = (agent.Path[agent.Path.Count - 1] - agent.Position).normalized;
                }
            }
            if (isMoving)
            {
                agent.IsMoving = true;
                agent.MoveDir = agent.StraightDir;
                MoveBlock.Clear();

                float moveDistance = dt * agent.Speed;
                if (agent.Type == MoveType.Straight)
                {
                    BuildStraightMoveBlockAngles(agent, agent.StraightDir, moveDistance);
                    float adjustAngle = MoveBlock.GetMinOffsetAngle();
                    if (!Mathf.Approximately(0, adjustAngle) && adjustAngle > -90 && adjustAngle < 90)
                    {
                        agent.MoveDir = RotateRadians(agent.StraightDir, adjustAngle * Mathf.Deg2Rad);
                    }
                }
                else if(agent.Type == MoveType.Path)
                {
                    BuildPathFindMoveBlockAngles(agent, agent.StraightDir, moveDistance, dt);
                    float adjustAngle = MoveBlock.GetMinOffsetAngle();
                    if (adjustAngle < -179 || adjustAngle > 179)
                    {
                        agent.IsMoving = false;
                    }
                    else if (!Mathf.Approximately(0, adjustAngle))
                    {
                        agent.MoveDir = RotateRadians(agent.StraightDir, adjustAngle * Mathf.Deg2Rad);
                    }
                }
            }
        }
    }

    private void BuildStraightMoveBlockAngles(MoveableAgent agent, Vector2 dir, float distance)
    {
        foreach (var target in Agents)
        {
            if (agent == target)
                continue;
            Vector2 offset = target.Position - agent.Position;
            float sqrMagnitude = offset.sqrMagnitude;
            if (sqrMagnitude <= 1E-05f)
                continue;//几乎重叠就允许移动直接忽略
            if (sqrMagnitude >= Sqr(distance + agent.Radius + target.Radius))
                continue;//不会发生碰撞就不处理
            if (Sqr(agent.Radius - target.Radius) > sqrMagnitude)
                continue;//一个在另外的内部就允许移动，让其走出去
            float magnitude = Mathf.Sqrt(sqrMagnitude);
            Vector2 normal = offset / magnitude;
            //如果已经发生碰撞，则屏蔽其左右两侧90度
            float offsetAngle = 90;
            float targetAngle = Vector2.SignedAngle(dir, normal);
            //magnitude <= agent.Radius + target.Radius;说明发生碰撞，直接屏蔽180度方向
            if (magnitude > agent.Radius + target.Radius)
            {
                offsetAngle = Mathf.Asin((target.Radius + agent.Radius) / magnitude) * Mathf.Rad2Deg;
            }
            MoveBlock.AddAngle(targetAngle, offsetAngle);
        }
    }

    private void BuildPathFindMoveBlockAngles(MoveableAgent agent, Vector2 dir, float distance, float dt)
    {
        foreach (var target in Agents)
        {
            if (agent == target)
                continue;
            Vector2 offset = target.Position - agent.Position;
            float sqrMagnitude = offset.sqrMagnitude;
            if (sqrMagnitude <= 1E-05f)
                continue;//几乎重叠就允许移动直接忽略
            if (Sqr(agent.Radius - target.Radius) > sqrMagnitude)
                continue;//一个在另外的内部就允许移动，让其走出去
            float targetMoveDistance = target.IsMoving ? target.Speed * dt : 0;
            if (sqrMagnitude >= Sqr(distance + agent.Radius + target.Radius + targetMoveDistance + CloseDistance))
                continue;//不会发生碰撞就不处理
            float magnitude = Mathf.Sqrt(sqrMagnitude);
            Vector2 normal = offset / magnitude;
            //如果已经发生碰撞，则屏蔽其左右两侧90度
            float targetAngle = Vector2.SignedAngle(dir, normal);
            float targetRadius = target.Radius + targetMoveDistance + CloseDistance;
            float selfRadius = agent.Radius + distance;
            if (magnitude <= selfRadius + targetRadius)
            {
                //说明发生碰撞，直接屏蔽90 + 目标中心到两圆交点 与两个圆心连线的夹角
                float a = Mathf.Acos((targetRadius * targetRadius + sqrMagnitude - selfRadius* selfRadius)/(2*targetRadius * magnitude));
                MoveBlock.AddAngle(targetAngle, 90 + a);
            }
            else //if (targetMoveDistance <= 0)
            {
                //如果目标没有移动，计算当前可能发生碰撞的移动方向范围
                float offsetAngle = Mathf.Asin((targetRadius + selfRadius) / magnitude) * Mathf.Rad2Deg;
                MoveBlock.AddAngle(targetAngle, offsetAngle);
            }
        }
    }

    public void Update(float dt)
    {
        UpdateMoveDir(dt);
        //移动更新
        foreach (var agent in Agents)
        {
            if (!agent.IsMoving)
                continue;
            float moveDistance = agent.Speed * dt;
            Vector2 moveDir = agent.MoveDir;
            //擦墙调整
            Vector2 newPos = agent.Position + moveDir * moveDistance;
            float sd = SDF.Sample(newPos);
            if (sd < (agent.Radius + 0.001f))
            {
                var gradient = SDF.Gradiend(newPos).normalized;
                newPos = newPos + (agent.Radius - sd + 0.001f) * gradient;
                Vector2 diff = newPos - agent.Position;
                float magnitude = diff.magnitude;
                if (magnitude > 0.0001f)
                {
                    moveDir = diff / magnitude;
                    if (magnitude <= moveDistance)
                        moveDistance = magnitude;
                }
            }
            if (moveDistance >= agent.Radius || SDF.Sample(agent.Position) < (agent.Radius + moveDistance))
            {
                moveDistance = SDF.TryMoveTo(agent.Position, moveDir, agent.Radius, moveDistance);
            }
            moveDistance = ColliderMoveDistance(agent, moveDir, moveDistance);
            Vector2 offset = moveDistance * moveDir;
            agent.Position += offset;
        }
    }

    public float ColliderMoveDistance(MoveableAgent agent, Vector2 dir, float distance)
    {
        foreach (var target in Agents)
        {
            if (agent == target)
                continue;
            Vector2 offset = target.Position - agent.Position;
            float sqrMagnitude = offset.sqrMagnitude;
            if (sqrMagnitude <= 1E-05f)
                continue;//几乎重叠就允许移动
            if (sqrMagnitude >= Sqr(distance + agent.Radius + target.Radius))
                continue;//不会发生碰撞就不处理
            if (Sqr(agent.Radius - target.Radius) > sqrMagnitude)
                continue;//一个在另外的内部就允许移动，让其走出去
            float magnitude = Mathf.Sqrt(sqrMagnitude);
            Vector2 normal = offset / magnitude;
            float dot = Vector2.Dot(normal, dir);
            dot = Mathf.Clamp(dot, -1, 1);
            if (dot <= 1E-05f)
                continue;//目标在自己后方也不处理
            float sqrTwoRadius = Sqr(target.Radius + agent.Radius);
            if (sqrMagnitude < sqrTwoRadius)
                return 0;//已经产生碰撞就不移动了
            //自己中心点A，目标中心点B，B投影到自己移动路径上的点C，组成直角三角形，斜边为AB
            float ac = dot * magnitude;//AC的长度
            float bc = Mathf.Sqrt(sqrMagnitude - Sqr(ac));//BC的长度
            if (bc >= (agent.Radius + target.Radius))
                continue;//BC的距离大于两者半径之和说明移动过程中不会产生碰撞
            //此时把A设为碰撞是自己中心点的位置，BC不变AB则是两者半径之和,
            float newac = Mathf.Sqrt(sqrTwoRadius - Sqr(bc));
            float collisionDistance = ac - newac;//产生碰撞时移动的距离
            if (collisionDistance < distance)
                distance = collisionDistance;
        }
        return distance;
    }
    public static Vector2 RotateRadians(Vector2 v, float radians)
    {
        var ca = Mathf.Cos(radians);
        var sa = Mathf.Sin(radians);
        return new Vector2(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
    }
    private static float Sqr(float v)
    {
        return v * v;
    }

}