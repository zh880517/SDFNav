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
    public MoveManager Move = new MoveManager();
    private OperatePanel panel = new OperatePanel();
    private JPSPathFinder PathFinder;
    private void Awake()
    {
        panel.Manager = this;
        using (MemoryStream stream = new MemoryStream(SDFAsset.bytes))
        {
            using(BinaryReader reader = new BinaryReader(stream))
            {
                Move.SDF.Read(reader);
                SDFDebugUtil.CreateVisableObj(Move.SDF);
                panel.ValidRect.min = Move.SDF.Origin;
                panel.ValidRect.width = Move.SDF.Width * Move.SDF.Grain;
                panel.ValidRect.height = Move.SDF.Height * Move.SDF.Grain;
                PathFinder = new JPSPathFinder(Move.SDF);
            }
        }
    }


    public int CreateAgent(Vector2 pos)
    {
        float scale = AgentRadius / 0.5f;
        var agent = Move.CreateAgent(pos, AgentRadius, Speed);
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
            Move.Agents.RemoveAll(it=>it.ID == id);
        }
    }

    public void MoveAgent(MoveableAgent agent, Vector2 pos)
    {
        if (agent.Type == MoveType.Straight)
        {
            Vector2 dir = pos - agent.Position;
            agent.StraightDir = dir.normalized;
            agent.Path.Clear();
        }
        else if (agent.Type == MoveType.Path)
        {
            pos = Move.SDF.FindNearestValidPoint(pos, agent.Radius);
            agent.Path.Clear();
            if (!PathFinder.Find(agent.Position, pos, agent.Radius, agent.Path))
            {
                agent.Path.Add(pos);
                Debug.LogError("寻路失败");
            }
        }
    }

    private void Update()
    {
        Move.CloseDistance = CloseDistance;
        Move.Update(Time.deltaTime);
        foreach (var angent in Move.Agents)
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
