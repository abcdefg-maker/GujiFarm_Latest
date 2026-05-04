using UnityEngine;

namespace ShopSystem
{
    /// <summary>
    /// 商店NPC触发器
    /// 挂载在NPC GameObject上（需Collider, isTrigger=true）
    /// 玩家进入范围后按E键打开商店
    /// </summary>
    public class ShopTrigger : MonoBehaviour
    {
        #region Serialized Fields
        [Header("商店配置")]
        [SerializeField] private ShopData shopData;

        [Header("交互提示")]
        [SerializeField] private GameObject interactHint;
        #endregion

        #region Private Fields
        private bool playerInRange;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (interactHint != null)
                interactHint.SetActive(false);
        }

        private void Update()
        {
            if (!playerInRange) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (ShopManager.Instance == null) return;

                if (ShopManager.Instance.IsOpen)
                {
                    ShopManager.Instance.CloseShop();
                }
                else
                {
                    ShopManager.Instance.OpenShop(shopData, transform);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = true;

            if (interactHint != null)
                interactHint.SetActive(true);

            Debug.Log("[ShopTrigger] 玩家进入商店范围");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = false;

            if (interactHint != null)
                interactHint.SetActive(false);

            // 玩家离开范围时关闭商店
            if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
            {
                ShopManager.Instance.CloseShop();
            }

            Debug.Log("[ShopTrigger] 玩家离开商店范围");
        }
        #endregion
    }
}
