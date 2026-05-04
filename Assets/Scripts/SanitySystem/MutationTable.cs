using System;
using UnityEngine;
using FarmingSystem;

namespace SanitySystem
{
    /// <summary>
    /// 变异映射表 - 数据驱动。
    /// 定义正常作物到变异作物的映射关系。
    /// 设计师在 Inspector 中配置，无需修改代码即可新增变异品种。
    /// </summary>
    [CreateAssetMenu(fileName = "NewMutationTable", menuName = "Sanity/Mutation Table")]
    public class MutationTable : ScriptableObject
    {
        [Serializable]
        public struct MutationEntry
        {
            [Tooltip("正常作物数据")]
            public CropData normalCrop;

            [Tooltip("变异版本的作物数据")]
            public CropData mutatedCrop;

            [Tooltip("该品种的变异概率倍率（1.0=使用默认曲线）")]
            [Range(0.1f, 3f)]
            public float probabilityMultiplier;
        }

        [Header("变异映射列表")]
        public MutationEntry[] entries;

        /// <summary>查找变异版本，无则返回null</summary>
        public CropData GetMutatedVariant(CropData normalCrop)
        {
            if (entries == null || normalCrop == null) return null;

            foreach (var entry in entries)
            {
                if (entry.normalCrop == normalCrop)
                {
                    return entry.mutatedCrop;
                }
            }
            return null;
        }

        /// <summary>获取品种概率倍率，无配置则返回1.0</summary>
        public float GetProbabilityMultiplier(CropData normalCrop)
        {
            if (entries == null || normalCrop == null) return 1f;

            foreach (var entry in entries)
            {
                if (entry.normalCrop == normalCrop)
                {
                    return entry.probabilityMultiplier;
                }
            }
            return 1f;
        }

        /// <summary>是否有变异版本</summary>
        public bool HasMutatedVariant(CropData normalCrop)
        {
            return GetMutatedVariant(normalCrop) != null;
        }
    }
}
