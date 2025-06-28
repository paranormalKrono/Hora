using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace NamespaceOcTree
{

    public struct Rectangle
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public float h;
        public float d;

        public Rectangle(float x, float y, float z, float w, float h, float d)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
            this.h = h;
            this.d = d;
        }

        public Rectangle(float3 pos, float dist)
        {
            x = pos.x - dist;
            y = pos.y - dist;
            z = pos.z - dist;
            w = dist * 2;
            h = dist * 2;
            d = dist * 2;
        }

        public bool Contains(float3 pos)
        {
            return pos.x >= x && pos.x <= x + w &&
                   pos.y >= y && pos.y <= y + h &&
                   pos.z >= z && pos.z <= z + d;
        }

        public bool Intersects(Rectangle other)
        {
            return !(x > other.x + other.w ||
                     x + w < other.x ||
                     y > other.y + other.h ||
                     y + h < other.y ||
                     z > other.z + other.d ||
                     z + d < other.z);
        }
    }

    public class NativeOcTreeNode
    {
        public Rectangle boundary;
        public TransformAccessArray datas;
        public NativeOcTreeNode[] nodes;
        public const int LeafsCount = 8;
        public const int Capacity = 4;

        public NativeOcTreeNode(Rectangle boundary)
        {
            this.boundary = boundary;
            datas = new TransformAccessArray();
            nodes = null;
        }

        public void Insert(Transform point)
        {
            NativeOcTreeNode node = this;
            while (true)
            {
                if (node.datas.length + 1 < Capacity)
                {
                    node.datas.Add(point);
                    break;
                }
                else
                {
                    node.Subdivide();

                    for (int i = 0; i < LeafsCount; ++i)
                    {
                        if (node.nodes[i].boundary.Contains(point.position))
                        {
                            node = node.nodes[i];
                            break;
                        }
                    }
                }
            }
        }

        private void Subdivide()
        {
            float x = boundary.x;
            float y = boundary.y;
            float z = boundary.z;
            float wh = boundary.w / 2;
            float hh = boundary.h / 2;
            float dh = boundary.d / 2;

            nodes = new NativeOcTreeNode[LeafsCount];
            nodes[0] = new NativeOcTreeNode(new Rectangle(x, y, z, wh, hh, dh));
            nodes[1] = new NativeOcTreeNode(new Rectangle(x + wh, y, z, wh, hh, dh));
            nodes[2] = new NativeOcTreeNode(new Rectangle(x, y + hh, z, wh, hh, dh));
            nodes[3] = new NativeOcTreeNode(new Rectangle(x, y, z + dh, wh, hh, dh));
            nodes[4] = new NativeOcTreeNode(new Rectangle(x + wh, y + hh, z, wh, hh, dh));
            nodes[5] = new NativeOcTreeNode(new Rectangle(x, y + hh, z + dh, wh, hh, dh));
            nodes[6] = new NativeOcTreeNode(new Rectangle(x + wh, y, z + dh, wh, hh, dh));
            nodes[7] = new NativeOcTreeNode(new Rectangle(x + wh, y + hh, z + dh, wh, hh, dh));
        }

        public TransformAccessArray Query(Rectangle range)
        {
            TransformAccessArray found = new TransformAccessArray();
            Stack<NativeOcTreeNode> stack = new Stack<NativeOcTreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                NativeOcTreeNode node = stack.Pop();

                if (!node.boundary.Intersects(range))
                    continue;

                if (node.nodes != null)
                {
                    for (int i = 0; i < node.nodes.Length; i++)
                    {
                        stack.Push(node.nodes[i]);
                    }
                }
                else // Leaf
                {
                    for (int i = 0; i < node.datas.length; i++)
                    {
                        Transform point = node.datas[i];
                        if (range.Contains(point.position))
                            found.Add(point);
                    }
                }
            }

            return found;
        }

        public void UpdateConcrete(Transform data)
        {
            Stack<NativeOcTreeNode> stack = new Stack<NativeOcTreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                NativeOcTreeNode node = stack.Pop();
                NativeOcTreeNode[] nodes = node.nodes;

                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].nodes != null)
                    {
                        stack.Push(nodes[i]);
                    }
                    else
                    {
                        ref TransformAccessArray datas = ref nodes[i].datas;
                        for (int j = 0; j < datas.length; i++)
                        {
                            if (datas[j] == data)
                            {
                                datas.RemoveAtSwapBack(j);
                                Insert(data);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public void RemoveConcrete(Transform point)
        {
            Stack<NativeOcTreeNode> stack = new Stack<NativeOcTreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                NativeOcTreeNode node = stack.Pop();
                NativeOcTreeNode[] nodes = node.nodes;

                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].nodes != null)
                    {
                        stack.Push(nodes[i]);
                    }
                    else
                    {
                        ref TransformAccessArray datas = ref nodes[i].datas;
                        for (int j = 0; j < datas.length; i++)
                        {
                            Transform data = datas[j];
                            if (data == point)
                            {
                                datas.RemoveAtSwapBack(i);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public TransformAccessArray FindNeighbors(Vector3 pos, float maxDistance)
        {
            Rectangle range = new Rectangle(pos, maxDistance);
            TransformAccessArray neighbours = Query(range);
            TransformAccessArray result = new TransformAccessArray();
            for (int i = 0; i < neighbours.length; ++i)
            {
                Transform point = neighbours[i];
                if (Vector3.Distance(pos, point.position) <= maxDistance)
                {
                    result.Add(point);
                }
            }
            neighbours.Dispose();

            return result;
        }

        public void Dispose()
        {
            Stack<NativeOcTreeNode> stack = new Stack<NativeOcTreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                NativeOcTreeNode node = stack.Pop();
                NativeOcTreeNode[] nodes = node.nodes;

                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].nodes != null)
                    {
                        stack.Push(nodes[i]);
                    }
                    else
                    {
                        nodes[i].datas.Dispose();
                    }
                }
            }
        }
    }

    public class NativeOcTree
    {
        private NativeOcTreeNode Root;

        public NativeOcTree(Rectangle boundary)
        {
            Root = new NativeOcTreeNode(boundary);
        }

        public void Insert(TransformAccessArray points)
        {
            for (int i = 0; i < points.length; ++i)
            {
                Root.Insert(points[i]);
            }
        }

        public void Insert(Transform point)
        {
            Root.Insert(point);
        }

        public TransformAccessArray Query(Rectangle range)
        {
            return Root.Query(range);
        }

        public TransformAccessArray FindNeighbors(Vector3 pos, float maxDistance)
        {
            return Root.FindNeighbors(pos, maxDistance);
        }

        public void UpdateConcrete(Transform point)
        {
            Root.UpdateConcrete(point);
        }

        public void RemoveConcrete(Transform point)
        {
            Root.RemoveConcrete(point);
        }

        public void Rebuild(TransformAccessArray points)
        {
            Root = new NativeOcTreeNode(Root.boundary);
            Insert(points);
        }

        public void Dispose()
        {
            Root.Dispose();
        }
    }
}