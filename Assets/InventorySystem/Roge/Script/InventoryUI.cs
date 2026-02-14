using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class ContainerView
    {
        public string containerId;
        public Transform slotRoot;   // GridLayoutGroup 等
    }

    [Header("Containers")]
    public List<ContainerView> containerViews;

    [Header("Slot UI")]
    public GameObject slotPrefab;   // ★ SlotUIが付いたPrefab

    Dictionary<string, List<SlotUI>> slotUIs
        = new Dictionary<string, List<SlotUI>>();

    void Start()
    {
        BuildAll();
        RefreshAll();
    }

    // =========================
    // UI構築
    // =========================
    void BuildAll()
    {
        foreach (var view in containerViews)
        {
            var container =
                InventoryManager.Instance.containers[view.containerId];

            var list = new List<SlotUI>();
            slotUIs[view.containerId] = list;

            for (int i = 0; i < container.slots.Count; i++)
            {
                GameObject go =
                    Instantiate(slotPrefab, view.slotRoot);

                // ★ 追加しない。取得する
                var slotUI = go.GetComponent<SlotUI>();
                if (slotUI == null)
                {
                    Debug.LogError("SlotPrefabにSlotUIが付いていません", go);
                    continue;
                }

                slotUI.Setup(view.containerId, i);
                list.Add(slotUI);
            }
        }
    }

    // =========================
    // UI更新
    // =========================
    public void RefreshAll()
    {
        foreach (var view in containerViews)
        {
            RefreshContainer(view.containerId);
        }
    }

    void RefreshContainer(string containerId)
    {
        var container =
            InventoryManager.Instance.containers[containerId];

        var uiList = slotUIs[containerId];

        for (int i = 0; i < uiList.Count; i++)
        {
            uiList[i].Refresh(container.slots[i]);
        }
    }

    // =========================
    // ボタン用（Inspector登録）
    // =========================
    public void SendTo(string targetContainerId)
    {
        InventoryManager.Instance
            .SendSelectedTo(targetContainerId);

        RefreshAll();
    }
}
