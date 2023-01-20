using SDFNav;
using System.Collections.Generic;
using UnityEngine;

public class MoveManager
{
    public SDFData SDF = new SDFData();
    public List<MoveableAgent> Agents = new List<MoveableAgent>();
    private int keyIndex;
    public MoveableAgent CreateAgent(Vector2 pos, float radius, float speed)
    {
        MoveableAgent agent = new MoveableAgent();
        agent.ID = ++keyIndex;
        agent.Position = pos;
        agent.Radius = radius;
        agent.Speed = speed;
        Agents.Add(agent);
        return agent;
    }

    public void Update()
    {

    }
}