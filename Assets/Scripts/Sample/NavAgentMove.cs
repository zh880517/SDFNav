using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavAgentMove : MonoBehaviour
{
    public Transform TargetPoint;
    public List<NavMeshAgent> Agents = new List<NavMeshAgent>();

    [ContextMenu("移动")]
    public void Move()
    {
        if (!TargetPoint)
            return;
        foreach (var agent in Agents)
        {
            if (agent)
            {
                agent.destination = TargetPoint.position;
            }
        }
    }
    
}
