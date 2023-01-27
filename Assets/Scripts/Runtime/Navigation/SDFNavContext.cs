using System.Collections.Generic;

namespace SDFNav
{
    public class SDFNavContext
    {
        public SDFData SDFMap;
        public MoveDirectionRange MoveBlock = new MoveDirectionRange();
        public List<NeighborAgentInfo> Neighbors = new List<NeighborAgentInfo>();
        public PathFinder PathFinder;

        public void Init(SDFData data)
        {
            SDFMap = data;
            PathFinder = new JPSPathFinder(data);
        }

        public void Clear()
        {
            MoveBlock.Clear();
            Neighbors.Clear();
        }
    }
}
