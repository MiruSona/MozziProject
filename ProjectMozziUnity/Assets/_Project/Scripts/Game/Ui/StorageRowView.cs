using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 보관함 화면의 줄 하나가 쥔 위젯 묶음. 어느 종을 보여 주는지는 SpeciesId 로 기억한다.
    public sealed class StorageRowView
    {
        public readonly GameObject Root;
        public readonly TMP_Text NameText;
        public readonly TMP_Text CountText;
        public readonly Button PlaceButton;
        public readonly Button SellButton;
        public readonly Button SellAllButton;

        public string SpeciesId;

        public StorageRowView(Transform rowRoot, string prefabName, UnityAction onPlace, UnityAction onSell, UnityAction onSellAll)
        {
            Root = rowRoot.gameObject;
            NameText = UiBindUtil.Find<TMP_Text>(rowRoot, UiNames.StorageRowNameText, prefabName);
            CountText = UiBindUtil.Find<TMP_Text>(rowRoot, UiNames.StorageRowCountText, prefabName);
            PlaceButton = UiBindUtil.BindButton(rowRoot, UiNames.StoragePlaceButton, prefabName, onPlace);
            SellButton = UiBindUtil.BindButton(rowRoot, UiNames.StorageSellButton, prefabName, onSell);
            SellAllButton = UiBindUtil.BindButton(rowRoot, UiNames.StorageSellAllButton, prefabName, onSellAll);
        }
    }
}
