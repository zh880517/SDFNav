using SDFNav;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SampleManager : MonoBehaviour
{
    public GameObject AgentPrefab;
    public TextAsset SDFAsset;
    public float AgentRadius = 0.5f;
    public float Speed = 2;
    public float CloseDistance = 0.5f;
    public Dictionary<int, GameObject> AgentPrefabs = new Dictionary<int, GameObject>();
    public List<MoveableAgent> Agents = new List<MoveableAgent>();
    private int keyIndex;
    private OperatePanel panel = new OperatePanel();
    private SDFNavContext Context;
    private void Awake()
    {
        panel.Manager = this;
        using (MemoryStream stream = new MemoryStream(SDFAsset.bytes))
        {
            using(BinaryReader reader = new BinaryReader(stream))
            {
                var sdf = new SDFData();
                sdf.Read(reader);
                Context = new SDFNavContext();
                Context.Init(sdf);
                panel.ValidRect.min = sdf.Origin;
                panel.ValidRect.width = sdf.Width * sdf.Grain;
                panel.ValidRect.height = sdf.Height * sdf.Grain;
                SDFDebugUtil.CreateVisableObj(sdf);
            }
        }
    }


    public int CreateAgent(Vector2 pos)
    {
        float scale = AgentRadius / 0.5f;
        MoveableAgent agent = new MoveableAgent();
        agent.ID = ++keyIndex;
        pos = Context.FindNearestValidPoint(pos, AgentRadius);
        agent.Position = pos;
        agent.Radius = AgentRadius;
        agent.Speed = Speed;
        Agents.Add(agent);

        var go = Instantiate(AgentPrefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
        go.transform.localScale = new Vector3(scale, 1, scale);
        go.name = $"Agent_{agent.ID}";
        AgentPrefabs.Add(agent.ID, go);
        return agent.ID;
    }

    public void DeleteAgent(int id)
    {
        if (AgentPrefabs.TryGetValue(id, out var go))
        {
            Destroy(go);
            AgentPrefabs.Remove(id);
            Agents.RemoveAll(it=>it.ID == id);
        }
    }

    public void MoveAgent(MoveableAgent agent, Vector2 pos)
    {
        if (agent.Type == MoveType.Straight)
        {
            Vector2 dir = pos - agent.Position;
            agent.StraightDir = dir.normalized;
            agent.NavPath.Clear();
        }
        else if (agent.Type == MoveType.Path)
        {
            agent.NavPath.Clear();
            pos = Context.FindNearestValidPoint(pos, agent.Radius);
            if (!Context.PathFinder.Find(agent.Position, pos, agent.Radius, agent.NavPath.Path))
            {
                agent.NavPath.Path.Add(pos);
                Debug.LogError("寻路失败");
            }
        }
    }

    private void UpdateMove(float dt)
    {
        foreach (var agent in Agents)
        {
            if (agent.Type == MoveType.None)
                continue;
            MoveAgentInfo info = new MoveAgentInfo
            {
                Radius = agent.Radius,
                Position = agent.Position,
                MoveDistance = agent.Speed * dt,
            };
            Context.Clear();
            if (agent.Type == MoveType.Straight)
            {
                SelectNeighbor(agent, CloseDistance, dt, Context.Neighbors);
                info.Direction = agent.StraightDir;
                Context.AdjustMoveByNeighbor(ref info);
                Context.AdjustMoveByObstacle(ref info);
            }
            else if(agent.Type == MoveType.Path && !agent.NavPath.HasFinished)
            {
                Context.OptimizePath(info, agent.NavPath, CloseDistance);
                if (!agent.NavPath.HasFinished)
                {
                    Vector2 nextPoint = agent.NavPath.Path[^1];
                    float selectRange = CloseDistance;
                    if (agent.NavPath.HasAdjustDirection)
                        selectRange = Mathf.Max(selectRange, Vector2.Distance(nextPoint, info.Position) - info.Radius);

                    SelectNeighbor(agent, selectRange, dt, Context.Neighbors);
                    Context.NavDestinationCheck(info, agent.NavPath, CloseDistance);
                    Context.AdjustMoveByPathMove(ref info, agent.NavPath, CloseDistance);
                }
            }
            if (info.MoveDistance > 0)
            {
                info.MoveDistance = Context.ColliderMoveByObstacle(info);
                info.MoveDistance = Context.ColliderMoveByNeighbor(info);
                agent.Position += info.Direction * info.MoveDistance;
                agent.MoveDir = info.Direction;
            }
        }
    }
    public void SelectNeighbor(MoveableAgent agent, float closeDistance, float dt, List<NeighborAgentInfo> neighbors)
    {
        foreach (var target in Agents)
        {
            if (agent == target)
                continue;
            Vector2 offset = target.Position - agent.Position;
            float sqrMagnitude = offset.sqrMagnitude;
            if (NavigationUtil.Sqr(agent.Radius + target.Radius + closeDistance + target.Speed * dt) < sqrMagnitude)
                continue;
            float magnitude = Mathf.Sqrt(sqrMagnitude);
            if (sqrMagnitude <= NavigationUtil.Epsilon)
            {
                offset = Vector2.zero;
                magnitude = 0;
            }
            else
            {
                offset /= magnitude;
            }
            NeighborAgentInfo neighbor = new NeighborAgentInfo
            {
                ID = target.ID,
                Direction = offset,
                Radius = target.Radius,
                Distance = magnitude,
                MoveDistance = target.Speed * dt,
            };
            int insertIdx = neighbors.Count;
            for (int i = 0; i < neighbors.Count; ++i)
            {
                if (neighbors[i].Distance > magnitude)
                {
                    insertIdx = i;
                    break;
                }
            }
            neighbors.Insert(insertIdx, neighbor);
        }
    }

    private void Update()
    {
        UpdateMove(Time.deltaTime);
        foreach (var angent in Agents)
        {
            if (AgentPrefabs.TryGetValue(angent.ID, out var go))
            {
                go.transform.position = new Vector3(angent.Position.x, 0, angent.Position.y);
                if (angent.MoveDir.x != 0 && angent.MoveDir.y != 0)
                {
                    go.transform.forward = new Vector3(angent.MoveDir.x, 0, angent.MoveDir.y);
                }
            }
        }
    }

    private void OnGUI()
    {
        panel.OnGUI();
    }

    private void OnDrawGizmos()
    {
        panel?.OnGizmos();
    }
}
