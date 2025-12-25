
using OcTreeJobs;
using System.Linq.Expressions;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using static Unity.Mathematics.math;

namespace OcTreeData
{

    [BurstCompile]
    public struct Rectangle
    {
        public float3 center;
        public float3 halfSize;

        public Rectangle(float3 center, float3 halfSize)
        {
            this.center = center;
            this.halfSize = halfSize;
        }
        public Rectangle(float3 pos, float dist)
        {
            center = pos;
            halfSize = new float3(dist);
        }

        public uint GetRelativeOctant(float3 pos)
        {
            bool3 b = pos < center;
            return (b.x ? 0U : 0b001U) |
                   (b.y ? 0U : 0b010U) |
                   (b.z ? 0U : 0b100U);
        }

        public void RebuildToInner(uint oct)
        {
            halfSize /= 2f;
            center.x += (oct & 0b001U) != 0 ? -halfSize.x : halfSize.x;
            center.y += (oct & 0b010U) != 0 ? -halfSize.y : halfSize.y;
            center.z += (oct & 0b100U) != 0 ? -halfSize.z : halfSize.z;
        }

        public void RebuildToOuter(uint oct)
        {
            center.x += (oct & 0b001U) != 0 ? halfSize.x : -halfSize.x;
            center.y += (oct & 0b010U) != 0 ? halfSize.y : -halfSize.y;
            center.z += (oct & 0b100U) != 0 ? halfSize.z : -halfSize.z;
            halfSize *= 2f;
        }

        public bool Contains(float3 pos)
        {
            // c.x - h.x > pos.x => pos is outside
            return !(any(center - halfSize > pos) || any(center + halfSize < pos));
        }

        public bool Intersects(Rectangle other)
        {
            return !any(abs(center - other.center) > (halfSize + other.halfSize));
        }
    }

    [BurstCompile]
    public struct Path
    {
        // Bad approach:
        // var path = new NativeList<int>(Allocator.Temp);
        // because it has a huge overhead
        //
        // The idea is that we encode our path in the 3d-space
        // using only 3 bits - relative position of octant

        // If we use a one long variable, we will have 64 bits = 21 * 3 + 1 bits
        // 21 level per one long variable
        // The question is: how many levels do we need?

        const int bits_per_level = 3; // we work with OcTree
        const int pow2perlevels = 1 << bits_per_level; // 0b0001 -> 0b1000
        const int ones = pow2perlevels - 1; // 0b1000 -> 0b0111
        public const int levels_max = 64 / bits_per_level; // 64 / 3 = 21

        // Private is used for protection from change, because bit shift is used
        private int cur_level;
        private long p;

        public bool IsEqual(Path other)
        {
            return p == other.p;
        }

        public void Reverse()
        {
            if (cur_level <= 1) return;

            long curPath = this.p;
            long np = 0; // We must use another long, because we need to save bits somewhere

            // level = 3    p = [101][010][111]
            // level_shift = 2 * 3 = 6  reverse_shift = 0
            // i = 0
            // lev = 111 << 6
            // lev &= p => lev = [101][000][000]
            // lev >> (6 - 0) = [000][000][101]
            int level_shift = (cur_level - 1) * bits_per_level;
            int reverse_shift = 0;
            for (int i = 0; i < cur_level; i++)
            {
                long oct = (ones << level_shift) & curPath;
                np |= (oct >> level_shift) << reverse_shift;

                level_shift -= bits_per_level;
                reverse_shift += bits_per_level;
            }
            this.p = np;
        }

        public void Push(uint oc_id)
        {
            if (oc_id > ones)
                Debug.LogError("Octant ID must fit in 3 bits (0–7).");
            if (cur_level >= levels_max)
                Debug.LogError("Path exceeds maximum depth.");

            // 010 -> 000[101][010]
            // level = 2
            // shift = 2 * 3 = 6
            // mask = 010 << 6 = 010[000][000]
            // p|= mask = [010][101][010]
            int shift = cur_level * bits_per_level;
            uint mask = oc_id << shift;
            p |= mask;
            cur_level++;
        }

        public uint Pop()
        {
            if (cur_level <= 0)
                Debug.LogError("Cannot pop from empty path.");

            // level = 2, p = ...000[010]010
            // shift = 1 * 3
            // mask = 111 << 1 = 111000
            // res = p & mask = ...000[010]000
            // we have to set 000 to the corresponding oct
            // return 010
            cur_level--;
            int shift = cur_level * bits_per_level;
            long mask = ones << shift;
            long res = p & mask;
            p &= ~mask;
            return (uint)(res >> shift);
        }

        public readonly bool IsRoot => cur_level == 0;
    }

    public enum DataStatus
    {
        Nothing = 00,
        ForwardUpdate = 01,
        Queue = 10,
        BackwardUpdate = 11,
    }

    [BurstCompile]
    public struct UpdateData
    {
        public Path pathToNode; // struct that contains int int long = 4 + 4 + 8 = 16 bytes
        public Path newPath; // 16
        public Rectangle newBoundary; // struct that contains float3 12 bytes
        public DataStatus status; // enum - 4 bytes
        // Total: 48 bytes due to right alignment

        public UpdateData(Path pathToNode, Rectangle boundary)
        {
            this.pathToNode = pathToNode;
            newPath = pathToNode;
            newBoundary = boundary;
            status = DataStatus.BackwardUpdate;
        }

    }

    [BurstCompile]
    public struct DataWithStatus
    {
        public int data_shifted_id;
        public int node_id;

        public DataWithStatus(int data_id, int node_id)
        {
            this.data_shifted_id = data_id << 2;
            this.node_id = node_id;
        }

        public void GetAll(out DataStatus status, out int data_id, out int node_id)
        {
            int id = this.data_shifted_id;
            status = (DataStatus)(id | 0b11);
            data_id = id >> 2;
            node_id = this.node_id;
        }

        public int GetDataID() => data_shifted_id >> 2;

        public void SetStatus(DataStatus status)
        {
            data_shifted_id -= data_shifted_id | 0b11;
            data_shifted_id += (int)status;
        }

        public void SetDataID(int data_id)
        {
            data_shifted_id &= 0b11;
            data_shifted_id |= data_id << 2;
        }

        public DataStatus GetStatus()
        {
            return (DataStatus)(data_shifted_id | 0b11);
        }
    }

    [BurstCompile]
    public struct DataBuffer
    {
        public int data_id;
        public float3 pos;

        public DataBuffer(int data_id, float3 pos)
        {
            this.data_id = data_id;
            this.pos = pos;
        }
    }

    public enum NodeLeafType
    {
        Nothing,
        Data,
        Node,
    }

    [BurstCompile]
    public struct OcTreeNode
    {

        public int parent_id;

        // first bit is 0 - nothing or leaf if >0,
        // first bit is 1 - node
        private int c0, c1, c2, c3,
                   c4, c5, c6, c7; // private is needed, because bitshift is used internally

        public const int capacity = 8;

        public void GetLeaf(uint i, out NodeLeafType type, out int value)
        {
            if (i < 0 || i > 7)
                Debug.LogError("Octant ID must fit in 3 bits (0–7).");

            value = i switch
            {
                0 => c0,
                1 => c1,
                2 => c2,
                3 => c3,
                4 => c4,
                5 => c5,
                6 => c6,
                7 => c7,
                _ => -1
            };

            if ((value & 1) == 1)
            {
                type = NodeLeafType.Node;

                // Unpacking
                value >>= 1;
            }
            else if (value == 0)
            {
                type = NodeLeafType.Nothing;
            }
            else // Leaf
            {
                type = NodeLeafType.Data;

                // Unpacking
                value >>= 1;
                value -= 1;
            }
        }

        // returns -1 if not found, [0, capacity) otherwise
        public int CountNothing()
        {
            int counter = 0;
            for (int i = 0; i < capacity; i++)
            {
                int value = i switch
                {
                    0 => c0,
                    1 => c1,
                    2 => c2,
                    3 => c3,
                    4 => c4,
                    5 => c5,
                    6 => c6,
                    7 => c7,
                    _ => 0
                };
                if (value == 0)
                {
                    counter++;
                }
            }
            return counter;
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < capacity; i++)
            {
                int value = i switch
                {
                    0 => c0,
                    1 => c1,
                    2 => c2,
                    3 => c3,
                    4 => c4,
                    5 => c5,
                    6 => c6,
                    7 => c7,
                    _ => 0
                };
                if (value != 0)
                {
                    return false;
                }
            }
            return true;
        }

        // returns -1 if not found, [0, capacity) otherwise
        public int FindNothing()
        {
            for (int i = 0; i < capacity; i++)
            {
                int value = i switch
                {
                    0 => c0,
                    1 => c1,
                    2 => c2,
                    3 => c3,
                    4 => c4,
                    5 => c5,
                    6 => c6,
                    7 => c7,
                    _ => 0
                };
                if (value == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        // returns -1 if count of children >1 or =0 otherwise returns oct of a single child
        public int FindSingleNotNothing()
        {
            int res = -1;
            for (int i = 0; i < capacity; i++)
            {
                int value = i switch
                {
                    0 => c0,
                    1 => c1,
                    2 => c2,
                    3 => c3,
                    4 => c4,
                    5 => c5,
                    6 => c6,
                    7 => c7,
                    _ => 0
                };
                if (value > 0)
                {
                    if (res > -1)
                    {
                        return -1;
                    }
                    res = i;
                }
            }
            return res;
        }

        // returns -1 if not found, [0, capacity) otherwise
        public int FindData(int value)
        {
            for (uint i = 0; i < capacity; ++i)
            {
                GetLeaf(i, out NodeLeafType type, out int val);
                if (type == NodeLeafType.Data && val == value)
                {
                    return (int)i;
                }
            }

            return -1;
        }

        // returns -1 if not found, [0, capacity) otherwise
        public int FindNode(int value)
        {
            for (uint i = 0; i < capacity; ++i)
            {
                GetLeaf(i, out NodeLeafType type, out int val);
                if (type == NodeLeafType.Node && val == value)
                {
                    return (int)i;
                }
            }

            return -1;
        }

        public void SetNothing(uint i)
        {
            switch (i)
            {
                case 0: c0 = 0; break;
                case 1: c1 = 0; break;
                case 2: c2 = 0; break;
                case 3: c3 = 0; break;
                case 4: c4 = 0; break;
                case 5: c5 = 0; break;
                case 6: c6 = 0; break;
                case 7: c7 = 0; break;
            }
        }

        public void SetData(uint i, int value)
        {
            // 0b001101 -> 0b011100
            value += 1; // 0 is nothing
            value <<= 1; // first bit is 0 when it's a Leaf

            switch (i)
            {
                case 0: c0 = value; break;
                case 1: c1 = value; break;
                case 2: c2 = value; break;
                case 3: c3 = value; break;
                case 4: c4 = value; break;
                case 5: c5 = value; break;
                case 6: c6 = value; break;
                case 7: c7 = value; break;
            }
        }

        public void SetNode(uint i, int value)
        {
            // first bit is 1 when it's Node
            value <<= 1;
            value |= 1;

            switch (i)
            {
                case 0: c0 = value; break;
                case 1: c1 = value; break;
                case 2: c2 = value; break;
                case 3: c3 = value; break;
                case 4: c4 = value; break;
                case 5: c5 = value; break;
                case 6: c6 = value; break;
                case 7: c7 = value; break;
            }
        }
    }

    [BurstCompile]
    public struct OcTreeRoot
    {
        public TransformAccessArray objectTransforms;
        public Rectangle rootBoundary;
        public NativeArray<OcTreeNode> nodes;
        public NativeArray<UpdateData> updateArray;
        public NativeReference<int> lastNodeIndex;
        public int batchCount;

        // Inner Data
        public NativeArray<DataBuffer> bufferUpdate;
        public NativeQueue<int> queueUpdate;

        private bool isCorrupted;

        public OcTreeRoot(SOOcTree cfg, TransformAccessArray objects, Rectangle boundary)
        {
            this.objectTransforms = objects;
            rootBoundary = boundary;
            nodes = new(new OcTreeNode[cfg.nodesPoolLength], Allocator.Persistent);
            updateArray = new(new UpdateData[cfg.updateArrayLength], Allocator.Persistent);
            lastNodeIndex = new NativeReference<int>(-1, Allocator.Persistent);
            batchCount = cfg.batchCount;

            bufferUpdate = new(new DataBuffer[cfg.updateBufferLength], Allocator.Persistent);
            queueUpdate = new(Allocator.Persistent);

            isCorrupted = false;

            AddNode(-1, out _);
        }

        public void Dispose()
        {
            nodes.Dispose();
            updateArray.Dispose();
            lastNodeIndex.Dispose();

            bufferUpdate.Dispose();
            queueUpdate.Dispose();
        }

        public void Update()
        {
            if (isCorrupted) return;
            // Be careful!!! You work in the struct with pointers and values

            var updateArray = this.updateArray;
            var queueUpdate = this.queueUpdate;
            var objectTransforms = this.objectTransforms;
            var bufferUpdate = this.bufferUpdate;

            // Retracts newPath and newBoundary in UpdateArray and adds data_id to the queue if it's changed
            var jobUpdateBackward = new JobOcTree_Update_BackwardPath()
            {
                updateArray = updateArray,
                updateQueue = queueUpdate.AsParallelWriter()
            }.Schedule(objectTransforms);

            jobUpdateBackward.Complete();

            int c = 0;
            while (c < bufferUpdate.Length &&
                queueUpdate.TryDequeue(out int data_id))
            {
                // Data may be outdated
                if (data_id > objectTransforms.length + 1) continue;

                // Check if it's delayed
                var updateData = updateArray[data_id];
                if (updateData.status != DataStatus.Queue) continue;
                // It should be ignored if there is a duplicate in the queue
                updateData.status = DataStatus.ForwardUpdate;
                updateArray[data_id] = updateData;

                Vector3 pos = objectTransforms[data_id].position;
                bufferUpdate[c] = new DataBuffer(data_id, pos);


                c++;
            }

            if (c < 1)
            {
                return;
            }

            // Calculates newPath and newBoundary
            // depending on retracted newPath and newBoundary
            var jobUpdateForward = new JobOcTree_Update_ForwardPass()
            {
                nodes = nodes,
                updateArray = updateArray,
                updateBuffer = this.bufferUpdate
            }.Schedule(c, batchCount);

            jobUpdateForward.Complete();

            // Reinserts data_id from curPath to newPath with newBoundary
            for (int i = 0; i < c; ++i)
            {
                var data = bufferUpdate[i];
                var data_id = data.data_id;
                var pos = data.pos;

                var updateData = updateArray[data_id];
                var pathToNode = updateData.pathToNode;
                var pathNew = updateData.newPath;
                var boundaryNew = updateData.newBoundary;

                if (pathToNode.IsEqual(pathNew)) continue; // Possible...

                // Remove
                TraverseThroughNodes(0, pathToNode, out int node_id);
                DFSSearch(node_id, data_id, out node_id, out uint oct);

                if (node_id == -1)
                {
                    Debug.LogError("OcTree Error: data_id wasn't found in with the path");
                    isCorrupted = true;
                    Debug.Break();
                }

                var node = nodes[node_id];
                node.SetNothing(oct);
                nodes[node_id] = node;

                // Insert
                TraverseThroughNodes(0, pathNew, out node_id);
                Insert(data_id, pos, node_id, pathNew, boundaryNew); // Insert using pos

                // After remove, structure of the tree could change
                // This is why we can't calculate node_id in the PassForward job

                updateData.status = DataStatus.BackwardUpdate; // Ability to start the process again
                updateArray[data_id] = updateData;
            }
        }

        public void Insert(int data_id)
        {
            if (isCorrupted) return;

            var path = new Path();
            var boundary = rootBoundary;
            float3 pos = objectTransforms[data_id].position;
            Insert(data_id, pos, 0, path, boundary);
        }

        public void RemoveSwapBack(int data_id, int last_id)
        {
            if (isCorrupted) return;

            Path path = updateArray[data_id].pathToNode;
            RemoveWithRestructure(data_id, path);
            RelocateLastIdIntoDataId(data_id, last_id);
        }

        // Get ids of elements in the given range
        public void Query(Rectangle range, NativeList<int> result)
        {
            if (isCorrupted) return;

            DFSQuery(0, rootBoundary, range, result);
        }

        public readonly void TraverseThroughNodes(int start_node_id, Path pathToTraverse, out int node_id)
        {
            node_id = start_node_id;
            if (pathToTraverse.IsRoot) return;

            var nodes = this.nodes;
            var reversedPath = pathToTraverse;
            reversedPath.Reverse();

            var node = nodes[node_id];
            uint oct = reversedPath.Pop();
            node.GetLeaf(oct, out NodeLeafType type, out int val);
            while (type == NodeLeafType.Node)
            {
                if (reversedPath.IsRoot)
                {
                    node_id = val;
                    return;
                }
                node = nodes[val];
                oct = reversedPath.Pop();
                node.GetLeaf(oct, out type, out val);
            }
        }
        
        // Searches in all nodes that intersect boundary, including all leafs in them
        private void DFSQuery(int node_id, Rectangle boundary,
            Rectangle queryRange, NativeList<int> result)
        {
            var nodes = this.nodes;
            var node = nodes[node_id];

            if (!boundary.Intersects(queryRange))
                return;

            Path path = new();
            uint oct = 0;

            int i = nodes.Length * 8 + 1;
            while (--i > 0)
            {
                if (oct < 8)
                {
                    node.GetLeaf(oct, out NodeLeafType type, out int value);
                    if (type == NodeLeafType.Data)
                    {
                        result.Add(value);

                        oct++;
                    }
                    else if (type == NodeLeafType.Node)
                    {
                        if (queryRange.Intersects(boundary))
                        {
                            // Move to inner octant
                            node_id = value;
                            node = nodes[node_id];
                            path.Push(oct);
                            boundary.RebuildToInner(oct);
                            oct = 0;
                        }
                        else
                        {
                            oct++;
                        }
                    }
                    else
                    {
                        oct++;
                    }
                }
                else
                {
                    if (path.IsRoot) return;

                    node = nodes[node.parent_id];
                    oct = path.Pop(); // Get previous node in path
                    boundary.RebuildToOuter(oct);

                    oct++; // Move, because oct that we returned from path is already checked
                }
            }

        }

        // Searches in all nodes that intersect boundary, including all leafs in them
        private void DFSSearch(int start_node_id, int data_id, out int node_id, out uint oct)
        {
            var nodes = this.nodes;
            node_id = start_node_id;
            var node = nodes[start_node_id];

            oct = 0;
            int i = nodes.Length * 8 + 1;
            while (--i > 0)
            {
                if (oct < 8)
                {
                    node.GetLeaf(oct, out NodeLeafType type, out int value);
                    if (type == NodeLeafType.Data)
                    {
                        if (data_id == value)
                        {
                            return;
                        }

                        oct++;
                    }
                    else if (type == NodeLeafType.Node)
                    {
                        // Move to inner octant
                        node_id = value;
                        node = nodes[node_id];
                        oct = 0;
                    }
                    else
                    {
                        oct++;
                    }
                }
                else
                {
                    if (node_id == start_node_id) return; // Relative root
                    node_id = node.parent_id;
                    node = nodes[node_id];
                    
                    oct++; // Move, because oct that we returned from path is already checked
                }
            }

        }

        private void RelocateLastIdIntoDataId(int data_id, int last_data_id)
        {
            Debug.Log("Relocate");
            var last_data = updateArray[last_data_id];
            last_data.status = DataStatus.BackwardUpdate;
            updateArray[data_id] = last_data;

            // Update tree
            TraverseThroughNodes(0, last_data.pathToNode, out int node_id);
            // DFSSearchLeaf(nodes, node_id, last_id, out node_id);
            var node = nodes[node_id];
            int oct = node.FindData(last_data_id);
            if (oct == -1)
            {
                isCorrupted = true;
                Debug.LogError("OcTree Error: Update data stores invalid path to data_id");
                Debug.DebugBreak();
            }
            node.SetData((uint)oct, data_id);
            nodes[node_id] = node;
        }

        private void RemoveWithRestructure(int data_id, Path calculatedPath)
        {
            Debug.Log("Remove");
            TraverseThroughNodes(0, calculatedPath, out int node_id);

            var node = nodes[node_id];

            int c = node.CountNothing();
            if (c < 7 || true)
            {
                // Change only leaf value to nothing
                int oct = node.FindData(data_id);
                if (oct == -1)
                {
                    Debug.LogError("OcTree Error: Remove didn't find the leaf with data_id");
                    isCorrupted = true;
                    Debug.Break();
                }
                node.SetNothing((uint)oct);
                nodes[node_id] = node;
            }
            else
            {
                SubdivisionRevert(node_id);
            }
        }

        private void SubdivisionRevert(int node_id)
        {
            Debug.Log("Subdivision revert");
            if (node_id == 0) return;
            var nodes = this.nodes;
            var node = nodes[node_id];
            int oct = node.FindSingleNotNothing();
            if (oct == -1) return; // More than 1 child
            node.GetLeaf((uint)oct, out NodeLeafType type, out int data_id);
            if (type != NodeLeafType.Data) return;

            // Keep track of path
            var data = updateArray[data_id];
            var path = data.pathToNode;

            // It's not a root, so we start to revert subdivision
            path.Pop();
            RemoveNodeSwapBack(node_id, out int parent_id);
            node = nodes[parent_id];
            node_id = parent_id;
            while (node_id != 0 && node.IsEmpty())
            {
                path.Pop();
                RemoveNodeSwapBack(node_id, out parent_id);
                node = nodes[parent_id];
                node_id = parent_id;
            }
            int i = node.FindNothing(); // It will be here, because we just removed node
            node.SetData((uint)i, data_id);
            nodes[node_id] = node;

            // Write path
            data.pathToNode = path; 
            updateArray[data_id] = data;
        }

        // Handles local restructuring during reinsertion
        private void Insert(int data_id, float3 pos, int node_id, Path pathToCurrentNode, Rectangle currentBoundary)
        {
            Debug.Log("Insert");
            var nodes = this.nodes;
            var node = nodes[node_id];

            int i = nodes.Length * 8 + 1;
            while (--i > 0)
            {
                // We may find an empty space
                int empty_id = node.FindNothing();
                if (empty_id != -1)
                {
                    // just insert as a leaf
                    node.SetData((uint)empty_id, data_id);
                    nodes[node_id] = node; // Write into memory
                    updateArray[data_id] = new UpdateData(pathToCurrentNode, currentBoundary);
                    return;
                }

                // It is a rare case when node is full
                var oct = currentBoundary.GetRelativeOctant(pos);
                node.GetLeaf(oct, out NodeLeafType type, out int data_or_node_id);
                if (type == NodeLeafType.Data)
                {
                    // Creating new node and inserting two data_ids
                    int new_node_id = this.lastNodeIndex.Value + 1;
                    this.lastNodeIndex.Value = new_node_id;
                    OcTreeNode new_node = new();
                    new_node.parent_id = node_id;
                    new_node.SetData(0U, data_id);
                    new_node.SetData(1U, data_or_node_id);
                    if (new_node_id >= nodes.Length)
                    {
                        Debug.LogError("OcTree Error: nodes, out of boundaries");
                        isCorrupted = true;
                        Debug.Break();
                    }
                    nodes[new_node_id] = new_node;

                    // Create update data
                    pathToCurrentNode.Push(oct);
                    currentBoundary.RebuildToInner(oct);
                    updateArray[data_id] = new UpdateData(pathToCurrentNode, currentBoundary);
                    updateArray[data_or_node_id] = new UpdateData(pathToCurrentNode, currentBoundary);

                    node.SetNode(oct, new_node_id);
                    nodes[node_id] = node;
                    return;
                }
                else // Node
                {
                    // Descent
                    node = nodes[data_or_node_id];
                    node_id = data_or_node_id;
                    pathToCurrentNode.Push(oct);
                    currentBoundary.RebuildToInner(oct);
                }
            }

            isCorrupted = true;
            Debug.LogError("OcTree Error: Insert boundary is reached, data_id wasn't inserted");
            Debug.Break();
        }

        private void AddNode(int parent_id, out int new_node_id)
        {
            new_node_id = this.lastNodeIndex.Value + 1;
            this.lastNodeIndex.Value = new_node_id;

            OcTreeNode new_node = new()
            {
                parent_id = parent_id
            };

            if (new_node_id >= nodes.Length)
            {
                isCorrupted = true;
                Debug.LogError("OcTree Error: nodes, out of boundaries");
                Debug.Break();
            }

            nodes[new_node_id] = new_node;
        }

        // parent_id = -1 if root else >= 0
        private void RemoveNodeSwapBack(int node_id, out int parent_id)
        {
            if (node_id == 0)
            {
                parent_id = -1; // Root doesn't have a parent
                return;
            }

            var node = nodes[node_id];

            // Parent change of node_id to Nothing to keep correctness
            parent_id = node.parent_id;
            var parent = nodes[parent_id];
            int parent_node_oct = parent.FindNode(node_id);
            if (parent_node_oct != -1)
            {
                parent.SetNothing((uint)parent_node_oct);
                nodes[parent_id] = parent;
            }
            else
            {
                Debug.LogError("OcTree Error: node_id wasn't found in it's parent node on RemoveSwapBack");
                isCorrupted = true; 
                Debug.Break();
            }

            // We need to place last node into node_id
            var last_id = this.lastNodeIndex.Value;
            var last = nodes[last_id];

            int last_parent_id = last.parent_id;
            // Parent change of last_id to node_id to keep correctness
            var last_parent = nodes[last_parent_id];
            int last_parent_last_oct = last_parent.FindNode(last_id);
            if (last_parent_last_oct != -1)
            {
                last_parent.SetNode((uint)last_parent_last_oct, node_id);
                nodes[last_parent_id] = last_parent;
            }
            else
            {
                Debug.LogError("OcTree Error: last_node_id wasn't found in it's parent node on RemoveSwapBack");
                isCorrupted = true; 
                Debug.Break();
            }

            // Placing last node into node_id
            nodes[node_id] = last;
            last_id -= 1;
            this.lastNodeIndex.Value = last_id;
        }
    }
}