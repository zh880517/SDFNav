using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OperatePanel
{
    public enum Mode
    {
        AddAgent,
        MoveAgent,
    }
    public SampleManager Manager;
    public Mode OpMode;
    public HashSet<int> SelectAgents = new HashSet<int>();
    private Plane zeroPlane = new Plane(Vector3.up, Vector3.zero);
    public Rect ValidRect;
    public void OnGUI()
    {
        var width = Screen.width;
        var height = Screen.height;
        using(new GUILayout.AreaScope(new Rect(0, 0, width*0.2f, height), "", "Box"))
        {
            DrawMode();
            DrawAgentList();
        }
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            if (e.button == 0)
            {
                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (zeroPlane.Raycast(mouseRay, out var distance))
                {
                    Vector3 pos = mouseRay.GetPoint(distance);
                    Vector2 v2 = new Vector2(pos.x, pos.z);
                    if (ValidRect.Contains(v2))
                    {
                        switch (OpMode)
                        {
                            case Mode.AddAgent:
                                var newId = Manager.CreateAgent(v2);
                                SelectAgents.Add(newId);
                        	    break;
                            case Mode.MoveAgent:
                                foreach (var agent in Manager.Move.Agents)
                                {
                                    if (SelectAgents.Contains(agent.ID))
                                    {
                                        Manager.MoveAgent(agent, v2);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }


    private void DrawMode()
    {
        using(new GUILayout.VerticalScope())
        {
            GUILayout.Label("选择模式");
            if (GUILayout.Toggle(OpMode == Mode.AddAgent, "创建Agent"))
                OpMode = Mode.AddAgent;
            if (GUILayout.Toggle(OpMode == Mode.MoveAgent, "移动Agent"))
                OpMode = Mode.MoveAgent;
        }
    }
    private Vector2 scrollPos;
    private void DrawAgentList()
    {
        GUILayout.Label("列表");
        using(var scroll = new GUILayout.ScrollViewScope(scrollPos))
        {
            scrollPos = scroll.scrollPosition;
            foreach (var agent in Manager.Move.Agents)
            {
                bool isSelect = SelectAgents.Contains(agent.ID);
                bool res = GUILayout.Toggle(isSelect, $"{agent.ID}");
                if (res != isSelect)
                {
                    if (res)
                        SelectAgents.Add(agent.ID);
                    else
                        SelectAgents.Remove(agent.ID);
                }
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(agent.Type == MoveType.None, "无"))
                        agent.Type = MoveType.None;
                    if (GUILayout.Toggle(agent.Type == MoveType.Straight, "直线"))
                        agent.Type = MoveType.Straight;
                    if (GUILayout.Toggle(agent.Type == MoveType.Path, "寻路"))
                        agent.Type = MoveType.Path;
                }
            }
        }
        using(new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("全选"))
            {
                foreach (var agent in Manager.Move.Agents)
                {
                    SelectAgents.Add(agent.ID);
                }
            }
            if (GUILayout.Button("取消所有"))
            {
                SelectAgents.Clear();
            }
        }
        if (GUILayout.Button("删除选择的Agent"))
        {
            foreach (var id in SelectAgents)
            {
                Manager.DeleteAgent(id);
            }
            SelectAgents.Clear();
        }
    }

    public void OnGizmos()
    {
        if (Manager == null || Manager.Move == null)
            return;
        Color color = Gizmos.color;
        Gizmos.color = Color.red;
        foreach (var agent in Manager.Move.Agents)
        {
            if (agent.Type == MoveType.Path && agent.Path.Count > 0)
            {
                Vector2 pos = agent.Position;
                for (int i = agent.Path.Count - 1; i>=0; --i)
                {
                    var pt = agent.Path[i];
                    Gizmos.DrawLine(new Vector3(pos.x, 0.5f, pos.y), new Vector3(pt.x, 0.5f, pt.y));
                    pos = pt;
                }
            }
        }
        Gizmos.color = color;
    }
}
