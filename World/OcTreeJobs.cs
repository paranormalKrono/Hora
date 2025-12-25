
using OcTreeData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;

namespace OcTreeJobs
{

    // We should be careful here,
    // but it's possible to traverse and update octree in parallel
    // By using ParallelWriters for Queues and buffers

    [BurstCompile]
    public struct JobOcTree_Update_BackwardPath: IJobParallelForTransform // Transforms
    {
        public NativeQueue<int>.ParallelWriter updateQueue;

        public NativeArray<UpdateData> updateArray;

        // Task is to partially traverse the tree back and
        // 1. flag update data as dirty
        // 2. add it to the queue
        // 3. save the new path and new boundary
        public void Execute(int index, TransformAccess transform)
        {
            var data = updateArray[index];
            if (data.status == DataStatus.Nothing) return; // if data contains nothing -> return

            var newBoundary = data.newBoundary;
            var pos = transform.position;
            if (newBoundary.Contains(pos)) return; // if oct didn't change -> do nothing

            var newPath = data.newPath;
            while (!newBoundary.Contains(pos) && !newPath.IsRoot)
            {
                uint oct = newPath.Pop();
                newBoundary.RebuildToOuter(oct);
            }

            if (data.status != DataStatus.Queue)
            {
                data.status = DataStatus.Queue;
                updateQueue.Enqueue(index);
            }

            data.newPath = newPath;
            data.newBoundary = newBoundary;

            updateArray[index] = data;
        }
    }

    // Length = buffer's fullness of not empty data
    [BurstCompile]
    public struct JobOcTree_Update_ForwardPass: IJobParallelFor
    {
        [ReadOnly] public NativeArray<OcTreeNode> nodes;
        [ReadOnly] public NativeArray<DataBuffer> updateBuffer;

        [NativeDisableParallelForRestriction] // Careful
        public NativeArray<UpdateData> updateArray;

        // We need to traverse tree to find new path, new boundary and
        // new node_id to reinsert immediately
        public void Execute(int index)
        {
            var data = updateBuffer[index];
            var data_id = data.data_id;
            var updateData = updateArray[data_id];
            if (updateData.status != DataStatus.ForwardUpdate) return;

            var pos = data.pos;
            var newPath = updateData.newPath;
            var newBoundary = updateData.newBoundary;

            // Traverse through nodes with newPath
            var node = nodes[0];

            if (!newPath.IsRoot)
            {
                var followPath = newPath;
                followPath.Reverse();
                while (!followPath.IsRoot)
                {
                    uint foct = followPath.Pop();
                    node.GetLeaf(foct, out _, out int fval);
                    node = nodes[fval]; // Assumption: newPath was calculated precisely
                }
            }

            // Use position to move further
            uint oct = newBoundary.GetRelativeOctant(pos);
            node.GetLeaf(oct, out NodeLeafType type, out int val);
            while (type == NodeLeafType.Node)
            {
                // Move to inner node
                newPath.Push(oct);
                newBoundary.RebuildToInner(oct);
                node = nodes[val];
                oct = newBoundary.GetRelativeOctant(pos);
                node.GetLeaf(oct, out type, out val);
            }

            updateData.newPath = newPath;
            updateData.newBoundary = newBoundary;
            updateData.status = DataStatus.BackwardUpdate;
            updateArray[data_id] = updateData;
        }
    }

}