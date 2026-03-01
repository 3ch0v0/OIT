using System.Collections.Generic;

// 定义你测试的所有算法枚举
public enum OITAlgorithm
{
    WBOIT,
    DepthPeeling,
    Abuffer,
}

public static class OITRegistry
{
    // 全局静态字典：按算法分类存储当前激活的物体
    public static readonly Dictionary<OITAlgorithm, HashSet<OITObject>> Objects = new Dictionary<OITAlgorithm, HashSet<OITObject>>()
    {
        // 必须为每一个枚举初始化一个空的集合
        { OITAlgorithm.WBOIT, new HashSet<OITObject>() },
        { OITAlgorithm.DepthPeeling, new HashSet<OITObject>() },
        { OITAlgorithm.Abuffer, new HashSet<OITObject>() },
    };
}