using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.IO;
using UnityEngine.EventSystems;

public class SC_ItemCrafting : MonoBehaviour
{
    public RectTransform playerSlotsContainer;
    public RectTransform craftingSlotsContainer;
    public RectTransform resultSlotContainer;
    public Button craftButton;
    public SC_SlotTemplate slotTemplate;
    public SC_SlotTemplate resultSlotTemplate;

    public AudioClip craftSuccessClip;
    public AudioClip craftFailClip;
    private AudioSource audioSource;

    [System.Serializable] // スロット内のアイテム情報
    public class SlotContainer
    {
        public Sprite itemSprite; // アサインされたアイテムのスプライト
        public int itemCount; // アイテムの数

        [HideInInspector]
        public int tableID;
        [HideInInspector]
        public SC_SlotTemplate slot;
    }

    [System.Serializable]  // アイテムデータ
    public class Item
    {
        public Sprite itemSprite; // このアイテムはまとめて（スタックして）所持できるか？
        public bool stackable = false;
        public string craftRecipe; // このアイテムをクラフトするのに必要なアイテムキー（カンマ区切り、ヒント：プレイモードでクラフトボタンを押すとレシピがコンソールに表示されます）

    }

    public SlotContainer[] playerSlots;
    SlotContainer[] craftSlots = new SlotContainer[9];
    SlotContainer resultSlot = new SlotContainer();
    public Item[] items; // 利用可能なすべてのアイテムのリスト
    int resultTableID = -1; // アイテムを取り出せるが、配置できないテーブルのID

    ColorBlock defaultButtonColors;

    // 持ち上げ中のアイテム情報 ---
    Sprite heldSprite = null;
    int heldCount = 0;
    SlotContainer heldFromSlot = null;

    [System.Serializable] // インベントリ保存用データ構造
    public class InventorySaveData
    {
        public List<SlotData> playerSlots = new List<SlotData>();
    }

    [System.Serializable]
    public class SlotData
    {
        public string itemSpriteName;
        public int itemCount;
    }

    void Start()
    {
        if (playerSlots == null || playerSlots.Length == 0)
            playerSlots = new SlotContainer[20]; // 必要な数に合わせて
        if (craftSlots == null || craftSlots.Length == 0)
            craftSlots = new SlotContainer[9];
        // スロット要素テンプレートのセットアップ
        slotTemplate.container.rectTransform.pivot = new Vector2(0, 1);
        slotTemplate.container.rectTransform.anchorMax = slotTemplate.container.rectTransform.anchorMin = new Vector2(0, 1);
        slotTemplate.craftingController = this;
        slotTemplate.gameObject.SetActive(false);
        // 結果スロット要素テンプレートのセットアップ
        resultSlotTemplate.container.rectTransform.pivot = new Vector2(0, 1);
        resultSlotTemplate.container.rectTransform.anchorMax = resultSlotTemplate.container.rectTransform.anchorMin = new Vector2(0, 1);
        resultSlotTemplate.craftingController = this;
        resultSlotTemplate.gameObject.SetActive(false);

        // クラフトボタンにクリックイベントを追加
        craftButton.onClick.AddListener(PerformCrafting);
        // クラフトボタンのデフォルトカラーを保存
        defaultButtonColors = craftButton.colors;

        // クラフト用スロットの初期化
        InitializeSlotTable(craftingSlotsContainer, slotTemplate, craftSlots, 5, 0);
        UpdateItems(craftSlots);

        // プレイヤースロットの初期化
        InitializeSlotTable(playerSlotsContainer, slotTemplate, playerSlots, 5, 1);
        UpdateItems(playerSlots);

        // 結果スロットの初期化
        InitializeSlotTable(resultSlotContainer, resultSlotTemplate, new SlotContainer[] { resultSlot }, 0, 2);
        UpdateItems(new SlotContainer[] { resultSlot });
        resultTableID = 2;

        // スロットテンプレートの設定を元に戻す
        slotTemplate.container.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        slotTemplate.container.raycastTarget = slotTemplate.item.raycastTarget = slotTemplate.count.raycastTarget = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        LoadInventoryFromJson();
    }

    void InitializeSlotTable(RectTransform container, SC_SlotTemplate slotTemplateTmp, SlotContainer[] slots, int margin, int tableIDTmp)
    {
        int resetIndex = 0;
        int rowTmp = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = new SlotContainer();
            }
            GameObject newSlot = Instantiate(slotTemplateTmp.gameObject, container.transform);
            slots[i].slot = newSlot.GetComponent<SC_SlotTemplate>();
            slots[i].slot.gameObject.SetActive(true);
            slots[i].tableID = tableIDTmp;

            float xTmp = (int)((margin + slots[i].slot.container.rectTransform.sizeDelta.x) * (i - resetIndex));
            if (xTmp + slots[i].slot.container.rectTransform.sizeDelta.x + margin > container.rect.width)
            {
                resetIndex = i;
                rowTmp++;
                xTmp = 0;
            }
            slots[i].slot.container.rectTransform.anchoredPosition = new Vector2(margin + xTmp, -margin - ((margin + slots[i].slot.container.rectTransform.sizeDelta.y) * rowTmp));
        }
    }

    // UIの更新
    void UpdateItems(SlotContainer[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Item slotItem = FindItem(slots[i].itemSprite);
            if (slotItem != null)
            {
                if (!slotItem.stackable)
                {
                    slots[i].itemCount = 1;
                }
                // アイテムの数を適用
                if (slots[i].itemCount > 1)
                {
                    slots[i].slot.count.enabled = true;
                    slots[i].slot.count.text = slots[i].itemCount.ToString();
                }
                else
                {
                    slots[i].slot.count.enabled = false;
                }
                // アイテムのスプライトを適用
                slots[i].slot.item.enabled = true;
                slots[i].slot.item.sprite = slotItem.itemSprite;
            }
            else
            {
                slots[i].slot.count.enabled = false;
                slots[i].slot.item.enabled = false;
            }
        }
    }

    // スプライトを参照してアイテムリストからアイテムを検索
    Item FindItem(Sprite sprite)
    {
        if (!sprite)
            return null;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemSprite == sprite)
            {
                return items[i];
            }
        }

        return null;
    }

    // レシピを参照してアイテムリストからアイテムを検索
    Item FindItem(string recipe)
    {
        if (recipe == "")
            return null;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].craftRecipe == recipe)
            {
                return items[i];
            }
        }

        return null;
    }

    // SC_SlotTemplate.csから呼び出される
    public void ClickEventRecheck(PointerEventData.InputButton button)
    {
        bool isLeftClick = button == PointerEventData.InputButton.Left;
        bool isRightClick = button == PointerEventData.InputButton.Right;

        if (heldSprite == null)
        {
            // 持ち上げは左クリックのみ
            if (isLeftClick)
            {
                var clicked = GetClickedSlot();
                if (clicked != null && clicked.itemSprite != null)
                {
                    heldSprite = clicked.itemSprite;
                    heldCount = clicked.itemCount;
                    heldFromSlot = clicked;
                    // 移動元からアイテムを消す
                    clicked.itemSprite = null;
                    clicked.itemCount = 0;
                    UpdateItems(playerSlots);
                    UpdateItems(craftSlots);
                    UpdateItems(new SlotContainer[] { resultSlot });
                }
            }
            // 右クリックでは何もしない
        }
        else
        {
            SlotContainer newClickedSlot = GetClickedSlot();
            if (newClickedSlot != null)
            {
                bool swapPositions = false;
                bool releaseClick = true;
                bool isResultSlot = newClickedSlot == resultSlot;

                // --- 移動元に戻す場合 ---
                if (newClickedSlot == heldFromSlot)
                {
                    if (isRightClick)
                    {
                        if (heldCount > 1)
                        {
                            heldFromSlot.itemSprite = heldSprite;
                            heldFromSlot.itemCount += 1;
                            heldCount -= 1;
                            UpdateItems(new SlotContainer[] { heldFromSlot });
                            releaseClick = false;
                        }
                        else if (heldCount == 1)
                        {
                            heldFromSlot.itemSprite = heldSprite;
                            heldFromSlot.itemCount += 1;
                            heldSprite = null;
                            heldCount = 0;
                            heldFromSlot = null;
                        }
                    }
                    else if (isLeftClick)
                    {
                        heldFromSlot.itemSprite = heldSprite;
                        heldFromSlot.itemCount += heldCount;
                        heldSprite = null;
                        heldCount = 0;
                        heldFromSlot = null;
                    }
                }
                // --- 他スロットに置く場合 ---
                else if ((newClickedSlot.itemSprite == null || newClickedSlot.itemSprite == heldSprite)
                    && resultTableID != newClickedSlot.tableID)
                {
                    Item slotItem = FindItem(heldSprite);
                    bool canStack = slotItem != null && slotItem.stackable;

                    // ★ここから追加: スタック不可アイテムの重ね置き阻止
                    if (!canStack && newClickedSlot.itemSprite == heldSprite)
                    {
                        // 何もせずreturn（消失を防ぐ）
                        return;
                    }
                    // ★ここまで追加

                    if (isRightClick)
                    {
                        if (heldCount > 1)
                        {
                            newClickedSlot.itemSprite = heldSprite;
                            newClickedSlot.itemCount += 1;
                            heldCount -= 1;
                            UpdateItems(new SlotContainer[] { newClickedSlot });
                            releaseClick = false;
                        }
                        else if (heldCount == 1)
                        {
                            newClickedSlot.itemSprite = heldSprite;
                            newClickedSlot.itemCount += 1;
                            heldSprite = null;
                            heldCount = 0;
                            heldFromSlot = null;
                        }
                    }
                    else if (isLeftClick)
                    {
                        newClickedSlot.itemSprite = heldSprite;
                        newClickedSlot.itemCount += heldCount;
                        heldSprite = null;
                        heldCount = 0;
                        heldFromSlot = null;
                    }
                }
                // 入れ替えは resultSlot 以外のみ許可
                else if (!isResultSlot)
                {
                    swapPositions = true;
                }

                if (swapPositions)
                {
                    // 入れ替え時はheldSprite/heldCountを新しいスロットの内容に
                    Sprite tempSprite = newClickedSlot.itemSprite;
                    int tempCount = newClickedSlot.itemCount;

                    newClickedSlot.itemSprite = heldSprite;
                    newClickedSlot.itemCount = heldCount;

                    heldSprite = tempSprite;
                    heldCount = tempCount;
                    heldFromSlot = newClickedSlot;
                }

                if (releaseClick && heldSprite != null && heldCount == 0)
                {
                    heldSprite = null;
                    heldFromSlot = null;
                }

                UpdateItems(playerSlots);
                UpdateItems(craftSlots);
                UpdateItems(new SlotContainer[] { resultSlot });
            }
        }
    }

    SlotContainer GetClickedSlot()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i].slot.hasClicked)
            {
                playerSlots[i].slot.hasClicked = false;
                return playerSlots[i];
            }
        }

        for (int i = 0; i < craftSlots.Length; i++)
        {
            if (craftSlots[i].slot.hasClicked)
            {
                craftSlots[i].slot.hasClicked = false;
                return craftSlots[i];
            }
        }

        if (resultSlot.slot.hasClicked)
        {
            resultSlot.slot.hasClicked = false;
            return resultSlot;
        }

        return null;
    }

    void PerformCrafting()
    {
        if (resultSlot.itemSprite != null && resultSlot.itemCount > 0)
        {
            if (craftFailClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(craftFailClip);
            }
            ColorBlock colors = craftButton.colors;
            colors.selectedColor = colors.pressedColor = new Color(0.8f, 0.55f, 0.55f, 1);
            craftButton.colors = colors;
            return;
        }

        craftButton.colors = defaultButtonColors;

        // レシピごとに成立するか判定
        Item craftedItem = null;
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.craftRecipe) && CanCraft(item))
            {
                craftedItem = item;
                break;
            }
        }

        if (craftedItem != null)
        {
            // 必要数だけ減算
            string[] recipeParts = craftedItem.craftRecipe.Split(',');
            for (int i = 0; i < craftSlots.Length; i++)
            {
                int needCount = 0;
                if (!string.IsNullOrEmpty(recipeParts[i]))
                {
                    string name = recipeParts[i];
                    int leftParen = name.IndexOf('(');
                    int rightParen = name.IndexOf(')');
                    if (leftParen > 0 && rightParen > leftParen)
                    {
                        string countStr = name.Substring(leftParen + 1, rightParen - leftParen - 1);
                        int.TryParse(countStr, out needCount);
                    }
                    else
                    {
                        needCount = 1;
                    }
                }
                if (craftSlots[i].itemSprite != null && craftSlots[i].itemCount > 0)
                {
                    craftSlots[i].itemCount -= needCount;
                    if (craftSlots[i].itemCount <= 0)
                    {
                        craftSlots[i].itemSprite = null;
                        craftSlots[i].itemCount = 0;
                    }
                }
            }

            resultSlot.itemSprite = craftedItem.itemSprite;
            resultSlot.itemCount = 1;

            UpdateItems(craftSlots);
            UpdateItems(new SlotContainer[] { resultSlot });

            if (craftSuccessClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(craftSuccessClip);
            }
        }
        else
        {
            ColorBlock colors = craftButton.colors;
            colors.selectedColor = colors.pressedColor = new Color(0.8f, 0.55f, 0.55f, 1);
            craftButton.colors = colors;

            if (craftFailClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(craftFailClip);
            }
        }
    }

    // インベントリをJSONで保存
    public void SaveInventoryToJson()
    {
        InventorySaveData saveData = new InventorySaveData();
        foreach (var slot in playerSlots)
        {
            SlotData data = new SlotData
            {
                itemSpriteName = slot.itemSprite != null ? slot.itemSprite.name : null,
                itemCount = slot.itemCount
            };
            saveData.playerSlots.Add(data);
        }

        string json = JsonUtility.ToJson(saveData, true);
        string path = Path.Combine(Application.dataPath, "inventory.json");
        File.WriteAllText(path, json);
        Debug.Log($"インベントリを保存しました: {path}");
    }

    // アプリケーション終了時に自動保存
    private void OnApplicationQuit()
    {
        MoveCraftAndResultToPlayerSlots();
        SaveInventoryToJson();
    }

    // インベントリをJSONから復元
    public void LoadInventoryFromJson()
    {
        string path = Path.Combine(Application.dataPath, "inventory.json");
        if (!File.Exists(path))
        {
            Debug.Log("インベントリ保存ファイルが見つかりません: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i < saveData.playerSlots.Count)
            {
                var data = saveData.playerSlots[i];
                playerSlots[i].itemSprite = null;
                // Sprite名からSpriteを復元
                if (!string.IsNullOrEmpty(data.itemSpriteName))
                {
                    foreach (var item in items)
                    {
                        if (item.itemSprite != null && item.itemSprite.name == data.itemSpriteName)
                        {
                            playerSlots[i].itemSprite = item.itemSprite;
                            break;
                        }
                    }
                }
                playerSlots[i].itemCount = data.itemCount;
            }
            else
            {
                playerSlots[i].itemSprite = null;
                playerSlots[i].itemCount = 0;
            }
        }
        UpdateItems(playerSlots);
        Debug.Log("インベントリを復元しました: " + path);
    }

    // Updateは毎フレーム呼び出される
    void Update()
    {
        // 持ち上げ中アイテムをカーソルに追従表示
        if (heldSprite != null && heldCount > 0)
        {
            if (!slotTemplate.gameObject.activeSelf)
            {
                slotTemplate.gameObject.SetActive(true);
                slotTemplate.container.enabled = false;
            }

            slotTemplate.item.enabled = true;
            slotTemplate.item.sprite = heldSprite;
            slotTemplate.item.color = Color.white;

            if (heldCount > 1)
            {
                slotTemplate.count.enabled = true;
                slotTemplate.count.text = heldCount.ToString();
            }
            else
            {
                slotTemplate.count.enabled = false;
            }

            // マウス位置に追従
            slotTemplate.container.rectTransform.position = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector3.zero;
        }
        else
        {
            if (slotTemplate.gameObject.activeSelf)
            {
                slotTemplate.gameObject.SetActive(false);
            }
        }
    }

    // クラフト前・後欄のアイテムをplayerSlotsに移動
    void MoveCraftAndResultToPlayerSlots()
    {
        // クラフトスロット
        for (int i = 0; i < craftSlots.Length; i++)
        {
            if (craftSlots[i].itemSprite != null && craftSlots[i].itemCount > 0)
            {
                AddItemToPlayerSlots(craftSlots[i].itemSprite, craftSlots[i].itemCount);
                craftSlots[i].itemSprite = null;
                craftSlots[i].itemCount = 0;
            }
        }
        // 結果スロット
        if (resultSlot.itemSprite != null && resultSlot.itemCount > 0)
        {
            AddItemToPlayerSlots(resultSlot.itemSprite, resultSlot.itemCount);
            resultSlot.itemSprite = null;
            resultSlot.itemCount = 0;
        }
        UpdateItems(playerSlots);
        UpdateItems(craftSlots);
        UpdateItems(new SlotContainer[] { resultSlot });
    }

    // アイテムをplayerSlotsにスタック加算または空き欄に配置
    void AddItemToPlayerSlots(Sprite itemSprite, int itemCount)
    {
        Item item = FindItem(itemSprite);
        if (item == null) return;

        if (item.stackable)
        {
            // 既存スロットに加算
            for (int i = 0; i < playerSlots.Length && itemCount > 0; i++)
            {
                if (playerSlots[i].itemSprite == itemSprite)
                {
                    playerSlots[i].itemCount += itemCount;
                    return;
                }
            }
            // 空きスロットにまとめて配置
            for (int i = 0; i < playerSlots.Length && itemCount > 0; i++)
            {
                if (playerSlots[i].itemSprite == null)
                {
                    playerSlots[i].itemSprite = itemSprite;
                    playerSlots[i].itemCount = itemCount;
                    return;
                }
            }
        }
        else
        {
            // スタック不可: 1スロットに1個ずつ
            for (int i = 0; i < playerSlots.Length && itemCount > 0; i++)
            {
                if (playerSlots[i].itemSprite == null)
                {
                    playerSlots[i].itemSprite = itemSprite;
                    playerSlots[i].itemCount = 1;
                    itemCount--;
                }
            }
        }

    }

    // レシピがクラフト欄の内容で成立するか判定
    bool CanCraft(Item recipeItem)
    {
        string[] recipeParts = recipeItem.craftRecipe.Split(',');
        for (int i = 0; i < craftSlots.Length; i++)
        {
            if (string.IsNullOrEmpty(recipeParts[i]))
            {
                if (craftSlots[i].itemSprite != null)
                    return false;
                continue;
            }
            string name = recipeParts[i];
            int needCount = 1;
            int leftParen = name.IndexOf('(');
            int rightParen = name.IndexOf(')');
            if (leftParen > 0 && rightParen > leftParen)
            {
                string countStr = name.Substring(leftParen + 1, rightParen - leftParen - 1);
                int.TryParse(countStr, out needCount);
                name = name.Substring(0, leftParen);
            }
            if (craftSlots[i].itemSprite == null || craftSlots[i].itemSprite.name != name || craftSlots[i].itemCount < needCount)
                return false;
        }
        return true;
    }
}