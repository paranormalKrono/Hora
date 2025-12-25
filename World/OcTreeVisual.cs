using OcTreeData;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace OcTreeVisual
{
    public class OcTreeVisual : MonoBehaviour
    {
        private Rectangle rootBoundary;
        private NativeArray<OcTreeNode> nodes;

        private bool isInitialized = false;

        public void Initialize(NativeArray<OcTreeNode> nodes, Rectangle rootBoundary)
        {
            this.nodes = nodes;
            this.rootBoundary = rootBoundary;
            this.isInitialized = true;
        }

        public void Stop()
        {
            this.isInitialized = false;
        }

        public void OnDrawGizmos()
        {
            if (!isInitialized) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(rootBoundary.center, rootBoundary.halfSize * 2);

            var nodes = this.nodes;
            var node = nodes[0];
            // var node_id = 0;

            // Stack<int> parent_ids = new ();
            // Stack<uint> octs = new ();

            Rectangle boundary = rootBoundary;
            Path path = new();
            uint oct = 0;

            for (int i = 0; i < nodes.Length * 8 + 1; ++i)
            {
                if (oct < 8)
                {
                    node.GetLeaf(oct, out NodeLeafType type, out int value);
                    if (type == NodeLeafType.Data)
                    {
                        float3 v = boundary.halfSize / 2;
                        v.x *= (oct & 0b001U) != 0 ? -1 : 1;
                        v.y *= (oct & 0b010U) != 0 ? -1 : 1;
                        v.z *= (oct & 0b100U) != 0 ? -1 : 1;

                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(boundary.center + v, boundary.halfSize.x / 10f);

                        oct++;
                    }
                    else if (type == NodeLeafType.Node)
                    {
                        // int childrenCount = OcTreeNode.capacity - node.CountEmptyChildren();

                        // Move to inner octant
                        node = nodes[value];
                        path.Push(oct);
                        boundary.RebuildToInner(oct);

                        // Debug
                        // parent_ids.Push(node_id);
                        // octs.Push(oct);
                        // node_id = value;

                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(boundary.center, boundary.halfSize * 2f);

                        oct = 0;
                    }
                    else
                    {
                        oct++;
                    }
                }
                else
                {
                    if (path.IsRoot) return;

                    var pi = node.parent_id;
                    node = nodes[pi];
                    oct = path.Pop();
                    boundary.RebuildToOuter(oct);

                    // int parent_id = parent_ids.Pop();
                    // if (pi != parent_id)
                    // {
                    //     Debug.LogError("Parent ID is wrong!");
                    // }
                    // uint real_oct = octs.Pop();
                    // if (oct != real_oct)
                    // {
                    //     Debug.LogError("Oct in path is wrong!");
                    // }
                    // node_id = pi;

                    oct++; // Move, because oct that we returned from path is already checked
                }
            }
        }
    }
}