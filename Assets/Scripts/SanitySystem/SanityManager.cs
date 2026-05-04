using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FarmingSystem;

namespace SanitySystem
{
    /// <summary>
    /// SAN（理智值）管理器 - 单例。
    /// 管理SAN值、处理消耗来源、触发疯狂症状、评估作物变异、处理游戏结束。
    /// </summary>
    public class SanityManager : MonoBehaviour
    {
        #region Singleton
        public static SanityManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("配置")]
        [SerializeField] private SanityConfigSO config;

        [Header("变异")]
        [SerializeField] private MutationTable mutationTable;

        [Header("疯狂症状")]
        [Tooltip("实现 IMadnessSymptom 接口的 MonoBehaviour")]
        [SerializeField] private List<MonoBehaviour> symptomBehaviours;
        #endregion

        #region Private Fields
        private float currentSanity;
        private bool isGameOver;
        private List<ISanityDrainSource> drainSources = new List<ISanityDrainSource>();
        private List<IMadnessSymptom> symptoms = new List<IMadnessSymptom>();
        #endregion

        #region Public Properties
        public float CurrentSanity => currentSanity;
        public float MaxSanity => config.maxSanity;
        public float NormalizedSanity => currentSanity / config.maxSanity;
        public bool IsGameOver => isGameOver;
        public SanityConfigSO Config => config;
        #endregion

        #region Events
        /// <summary>SAN值变化时触发。参数: 新SAN值</summary>
        public event Action<float> OnSanityChanged;

        /// <summary>跨越疯狂阈值时触发。参数: 跨越的阈值(90/80/70...)</summary>
        public event Action<int> OnMadnessThresholdCrossed;

        /// <summary>作物变异发生时触发。参数: (原始CropData, 变异CropData)</summary>
        public event Action<CropData, CropData> OnCropMutated;

        /// <summary>游戏结束时触发（SAN归零）</summary>
        public event Action OnGameOver;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (config == null)
            {
                Debug.LogError("[SanityManager] 未设置 SanityConfigSO!");
                return;
            }

            currentSanity = config.startingSanity;
            CollectSymptoms();
        }

        private void Update()
        {
            if (isGameOver || config == null) return;

            // 遍历所有持续消耗来源，累加消耗量
            float totalDrain = 0f;
            for (int i = 0; i < drainSources.Count; i++)
            {
                if (drainSources[i].IsActive)
                {
                    totalDrain += drainSources[i].GetDrainAmount(Time.deltaTime, currentSanity);
                }
            }

            if (totalDrain != 0f)
            {
                ModifySanity(-totalDrain);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Core Methods

        /// <summary>
        /// 修改SAN值 - 所有SAN变化的唯一入口。
        /// 正值=恢复，负值=消耗。
        /// </summary>
        public void ModifySanity(float delta)
        {
            if (isGameOver) return;
            if (delta == 0f) return;

            float previousSanity = currentSanity;
            currentSanity = Mathf.Clamp(currentSanity + delta, 0f, config.maxSanity);

            // 避免无变化时触发事件
            if (Mathf.Approximately(previousSanity, currentSanity)) return;

            OnSanityChanged?.Invoke(currentSanity);

            // 仅在SAN下降时检测阈值
            if (delta < 0f)
            {
                CheckMadnessThresholds(previousSanity, currentSanity);
            }

            // 检查游戏结束
            if (currentSanity <= SanityConstants.GameOverThreshold)
            {
                TriggerGameOver();
            }
        }

        /// <summary>便捷方法: 消耗SAN（传入正值）</summary>
        public void DrainSanity(float amount)
        {
            if (amount <= 0f) return;
            ModifySanity(-amount);
        }

        /// <summary>便捷方法: 恢复SAN（传入正值，当前阶段预留）</summary>
        public void RestoreSanity(float amount)
        {
            if (amount <= 0f) return;
            ModifySanity(amount);
        }

        /// <summary>
        /// 评估作物变异 - 种植时调用。
        /// 根据当前SAN值和变异表决定是否替换为变异作物。
        /// 返回实际使用的CropData（原始或变异版本）。
        /// </summary>
        public CropData EvaluateMutation(CropData originalCrop)
        {
            if (originalCrop == null || config == null || mutationTable == null)
                return originalCrop;

            // 已经是变异作物，不再二次变异
            if (originalCrop.isMutated)
                return originalCrop;

            // 检查是否有变异版本
            if (!mutationTable.HasMutatedVariant(originalCrop))
                return originalCrop;

            // 计算变异概率
            float baseProbability = config.GetMutationProbability(currentSanity);
            float multiplier = mutationTable.GetProbabilityMultiplier(originalCrop);
            float finalProbability = Mathf.Clamp01(baseProbability * multiplier);

            // 概率判定
            if (UnityEngine.Random.value < finalProbability)
            {
                CropData mutatedCrop = mutationTable.GetMutatedVariant(originalCrop);
                if (mutatedCrop != null)
                {
                    OnCropMutated?.Invoke(originalCrop, mutatedCrop);
                    Debug.Log($"[SanityManager] 作物变异! {originalCrop.cropName} → {mutatedCrop.cropName} (概率: {finalProbability:P0}, SAN: {currentSanity:F0})");
                    return mutatedCrop;
                }
            }

            return originalCrop;
        }

        #endregion

        #region Drain Source Management

        /// <summary>注册持续消耗来源</summary>
        public void RegisterDrainSource(ISanityDrainSource source)
        {
            if (source != null && !drainSources.Contains(source))
            {
                drainSources.Add(source);
                Debug.Log($"[SanityManager] 注册消耗来源: {source.SourceID}");
            }
        }

        /// <summary>注销持续消耗来源</summary>
        public void UnregisterDrainSource(ISanityDrainSource source)
        {
            if (source != null)
            {
                drainSources.Remove(source);
            }
        }

        #endregion

        #region Symptom Management

        /// <summary>注册疯狂症状</summary>
        public void RegisterSymptom(IMadnessSymptom symptom)
        {
            if (symptom != null && !symptoms.Contains(symptom))
            {
                symptoms.Add(symptom);
            }
        }

        /// <summary>注销疯狂症状</summary>
        public void UnregisterSymptom(IMadnessSymptom symptom)
        {
            if (symptom != null)
            {
                symptoms.Remove(symptom);
            }
        }

        /// <summary>
        /// 从 Inspector 列表中收集实现了 IMadnessSymptom 的组件
        /// </summary>
        private void CollectSymptoms()
        {
            symptoms.Clear();
            if (symptomBehaviours == null) return;

            foreach (var mb in symptomBehaviours)
            {
                if (mb is IMadnessSymptom symptom)
                {
                    symptoms.Add(symptom);
                }
                else if (mb != null)
                {
                    Debug.LogWarning($"[SanityManager] {mb.name} 未实现 IMadnessSymptom 接口，已忽略。");
                }
            }

            if (symptoms.Count > 0)
            {
                Debug.Log($"[SanityManager] 已注册 {symptoms.Count} 个疯狂症状。");
            }
        }

        #endregion

        #region Threshold Detection

        /// <summary>
        /// 检测跨越的疯狂阈值并触发对应症状。
        /// SAN从85降到65时，会依次触发80和70两个阈值。
        /// </summary>
        private void CheckMadnessThresholds(float previousSanity, float newSanity)
        {
            float interval = config.symptomInterval;
            int prevBucket = Mathf.FloorToInt(previousSanity / interval);
            int newBucket = Mathf.FloorToInt(newSanity / interval);

            // 从高到低遍历跨越的阈值
            for (int bucket = prevBucket; bucket > newBucket; bucket--)
            {
                int thresholdValue = bucket * (int)interval;
                if (thresholdValue <= 0) continue; // 跳过0阈值，由GameOver处理

                OnMadnessThresholdCrossed?.Invoke(thresholdValue);
                TriggerSymptoms(previousSanity, newSanity, thresholdValue);
                Debug.Log($"[SanityManager] 跨越疯狂阈值: {thresholdValue} (SAN: {previousSanity:F0} → {newSanity:F0})");
            }
        }

        /// <summary>
        /// 触发匹配的症状效果
        /// </summary>
        private void TriggerSymptoms(float previousSanity, float newSanity, int thresholdCrossed)
        {
            // 按优先级排序（高优先级先执行）
            symptoms.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var symptom in symptoms)
            {
                // 通配符(-1)或匹配特定阈值
                if (symptom.TriggerThreshold == -1 || symptom.TriggerThreshold == thresholdCrossed)
                {
                    symptom.OnTrigger(previousSanity, newSanity, thresholdCrossed);
                }
            }
        }

        #endregion

        #region Game Over

        /// <summary>
        /// 触发游戏结束
        /// </summary>
        private void TriggerGameOver()
        {
            if (isGameOver) return;

            isGameOver = true;
            Debug.Log("[SanityManager] 游戏结束 - 理智归零!");

            OnGameOver?.Invoke();

            // 冻结游戏时间（如果TimeManager可用）
            if (TimeSystem.TimeManager.Instance != null)
            {
                TimeSystem.TimeManager.Instance.Pause();
            }

            // 延迟后执行游戏结束序列
            StartCoroutine(GameOverSequence());
        }

        private IEnumerator GameOverSequence()
        {
            yield return new WaitForSeconds(config.gameOverDelay);

            // TODO: 存档系统完成后，改为回档到当天早晨
            Debug.Log("[SanityManager] 游戏结束序列完成（等待存档系统实现回档功能）");
        }

        #endregion
    }
}
