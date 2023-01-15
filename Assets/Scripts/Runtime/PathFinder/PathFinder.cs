using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SDFNav
{
    public struct GridLocation
    {
        public int X;
        public int Y;
        public int Index;

        public bool Valid => Index >= 0;

        public static readonly GridLocation Empty = new GridLocation { Index = -1 };
    }

    public abstract class PathFinder
    {
        public static readonly GridLocation[] DirOffsets = new GridLocation[]
        {
            new GridLocation{ X = 0, Y = 1 },//Top
            new GridLocation{ X = -1, Y = 0 },//Left
            new GridLocation{ X = 0, Y = -1 },//Bottom
            new GridLocation{ X = 1, Y = 0 },//Right
            new GridLocation{ X = -1, Y = 1 },//TopLeft
            new GridLocation{ X = 1, Y = 1 },//TopRight
            new GridLocation{ X = -1, Y = -1 },//BottomLeft
            new GridLocation{ X = 1, Y = -1 },//BottomRight
        };

        protected readonly SDFData SDF;
        private readonly BitArray closeMask;
        private readonly BitArray openMask;
        private readonly Queue<PathNode> nodePool = new Queue<PathNode>();
        private readonly Dictionary<int, PathNode> nodeCache = new Dictionary<int, PathNode>();
        protected FTBinaryHeap<PathNode> openHeap = new FTBinaryHeap<PathNode>();
        protected GridLocation endLocation;
        protected float moveRadius;

        public PathFinder(SDFData sdf)
        {
            SDF = sdf;
            closeMask = new BitArray(sdf.Width * sdf.Height);
            openMask = new BitArray(sdf.Width * sdf.Height);
        }

        public bool Find(Vector2 start, Vector2 end, float radius, List<Vector2> reversePath)
        {
            float startSDF = SDF.Sample(start);
            float endSDF = SDF.Sample(end);
            float sqrMagnitude = (end - start).sqrMagnitude;
            if (sqrMagnitude < startSDF * startSDF //在终点在起点可移动范围内
                || sqrMagnitude < endSDF * endSDF //起点在终点可移动范围内
                || SDF.CheckStraightMove(start, end, radius))
            {
                reversePath.Add(end);
                reversePath.Add(start);
                return true;
            }
            Clear();
            moveRadius = radius - 0.01f;//做个轻微修正，防止部分贴边情况导致的无法计算路径
            var startLocation = GetLocation(start);
            if (!startLocation.Valid || !IsWalkable(startLocation))
                return false;
            endLocation = GetLocation(end);
            if (!endLocation.Valid || !IsWalkable(endLocation))
                return false;
            var startNode = GetNode(startLocation);
            var endNode = GetNode(endLocation);
            if (startNode != null && endNode != null && Search(startNode))
            {
                var node = endNode;
                while (node.parent != null)
                {
                    Vector2 pos = new Vector2(node.location.X * SDF.Grain, node.location.Y * SDF.Grain) + SDF.Origin;
                    reversePath.Add(pos);
                    node = node.parent;
                }
                reversePath.Add(start);
                return true;
            }
            return false;
        }
        private void Clear()
        {
            openMask.SetAll(false);
            closeMask.SetAll(false);
            foreach (var kv in nodeCache)
            {
                kv.Value.Clear();
                nodePool.Enqueue(kv.Value);
            }
            nodePool.Clear();
            openHeap.Clear();
        }

        protected abstract bool Search(PathNode node);

        protected GridLocation GetLocation(Vector2 pos)
        {
            pos -= SDF.Origin;
            pos /= SDF.Grain;
            var result = new GridLocation
            {
                X = Mathf.FloorToInt(pos.x),
                Y = Mathf.FloorToInt(pos.y),
            };
            if (result.X < 0 || result.Y < 0 || result.X > SDF.Width || result.Y > SDF.Height)
            {
                result.Index = -1;
            }
            else
            {
                result.Index = result.X + result.Y * SDF.Width;
            }
            return result;
        }

        protected PathNode GetNode(in GridLocation location)
        {
            if (location.Index < 0)
                return null;
            if (!nodeCache.TryGetValue(location.Index, out var node))
            {
                node = nodePool.Count > 0 ? nodePool.Dequeue() : new PathNode();
                node.location = location;
                nodeCache.Add(location.Index, node);
            }
            return node;
        }

        protected GridLocation GetNeighbor(GridLocation location, Direction dir)
        {
            var offset = DirOffsets[(int)dir];
            var result = new GridLocation
            {
                X = location.X + offset.X,
                Y = location.Y + offset.Y,
            };
            if (result.X < 0 || result.Y < 0 || result.X > SDF.Width || result.Y > SDF.Height)
            {
                result.Index = -1;
            }
            else
            {
                result.Index = result.X + result.Y * SDF.Width;
            }
            return result;
        }

        protected bool IsWalkable(in GridLocation location)
        {
            return location.Index >= 0 && SDF[location.Index] >= moveRadius;
        }

        protected void SetClose(in GridLocation location)
        {
            if (location.Index > 0)
            {
                closeMask.Set(location.Index, true);
                openMask.Set(location.Index, false);
            }
        }
        protected void SetOpen(in GridLocation location)
        {
            if (location.Index > 0)
            {
                closeMask.Set(location.Index, false);
                openMask.Set(location.Index, true);
            }
        }

        protected bool IsClose(in GridLocation location)
        {
            return location.Index >= 0 && closeMask.Get(location.Index);
        }

        protected bool IsOpen(in GridLocation location)
        {
            return location.Index >= 0 && openMask.Get(location.Index);
        }

    }
}
