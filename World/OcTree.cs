
using OcTreeData;
using Unity.Collections;
using UnityEngine.Jobs;

public class OcTree
{
    // Main
    private OcTreeRoot root;

    public OcTree(SOOcTree cfg, TransformAccessArray objects, Rectangle boundary)
    {
        root = new OcTreeRoot(cfg, objects, boundary);
    }

    public void Dispose()
    {
        root.Dispose();
    }

    public void Update()
    {
        root.Update();
    }

    public void Insert(int id)
    {
        root.Insert(id);
    }

    public void RemoveSwapBack(int data_id, int last_id)
    {
        root.RemoveSwapBack(data_id, last_id);
    }

    public void Query(Rectangle range, NativeList<int> result)
    {
        root.Query(range, result);
    }

    public OcTreeRoot GetRoot() => root;
}