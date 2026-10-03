using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Farm;
using Mozzi.Game.Storage;
using Mozzi.Game.View;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 보관함 화면 (세로조각1판설계 3-5). 종 · 마릿수 줄마다 「농장에 놓기」 「팔기」 「일괄 팔기」.
    // 줄을 끌면 화면이 닫히고 그 종의 찹쌀이를 든 채 농장으로 나간다 (피드백 7). 버튼은 그대로다.
    public sealed class StorageScreen : OverlayScreen
    {
        private readonly List<StorageRowView> _rows = new List<StorageRowView>();
        private readonly List<UiDragSource> _dragSources = new List<UiDragSource>();

        private IStorageHub _storage;
        private IFarmHub _farm;
        private IWalletHub _wallet;
        private IStaticDataHub _data;
        private GameObject _emptyText;
        private TMP_Text _fenceText;
        private Button _fenceButton;

        protected override string PrefabName
        {
            get { return "StorageScreen"; }
        }

        protected override string Title
        {
            get { return "보관함"; }
        }

        protected override void BindChildren()
        {
            _storage = InGameHub.Get<IStorageHub>();
            _farm = InGameHub.Get<IFarmHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _data = InGameHub.Get<IStaticDataHub>();

            _emptyText = UiBindUtil.FindObject(transform, UiNames.StorageEmptyText, PrefabName);
            _fenceText = UiBindUtil.Find<TMP_Text>(transform, UiNames.StorageFenceText, PrefabName);
            _fenceButton = UiBindUtil.BindButton(transform, UiNames.StorageFenceButton, PrefabName, OnBuyFence);

            for (int i = 0; i < UiNames.StorageRowCount; i++)
            {
                int index = i;
                Transform rowRoot = UiBindUtil.FindObject(transform, UiNames.StorageRowPrefix + i, PrefabName).transform;
                _rows.Add(new StorageRowView(rowRoot, PrefabName,
                    delegate { OnPlace(index); },
                    delegate { OnSell(index); },
                    delegate { OnSellAll(index); }));

                UiDragSource source = rowRoot.GetComponent<UiDragSource>();
                if (source == null)
                {
                    source = rowRoot.gameObject.AddComponent<UiDragSource>();
                }

                source.OnBegin = delegate { OnRowDragBegin(index); };
                _dragSources.Add(source);
            }
        }

        protected override void UnbindChildren()
        {
            for (int i = 0; i < _dragSources.Count; i++)
            {
                _dragSources[i].OnBegin = null;
            }

            _dragSources.Clear();
            _rows.Clear();
            _fenceButton = null;
            _storage = null;
            _farm = null;
            _wallet = null;
            _data = null;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            UiBindUtil.SetText(_fenceText, "농장 " + _farm.MozziCount + "/" + _farm.FenceMax);
            RefreshFenceButton();

            IReadOnlyList<CountEntry> entries = _storage.Entries;
            UiBindUtil.SetActive(_emptyText, entries.Count == 0);
            bool farmFull = _farm.MozziCount >= _farm.FenceMax;

            for (int i = 0; i < _rows.Count; i++)
            {
                StorageRowView row = _rows[i];
                bool has = i < entries.Count;
                UiBindUtil.SetActive(row.Root, has);
                if (has == false)
                {
                    row.SpeciesId = null;
                    continue;
                }

                row.SpeciesId = entries[i].id;
                UiBindUtil.SetText(row.NameText, _data.Species.Get(row.SpeciesId).Name);
                UiBindUtil.SetText(row.CountText, entries[i].count + "마리");
                UiBindUtil.SetLabel(row.PlaceButton, "농장에 놓기");
                row.PlaceButton.interactable = farmFull == false;
                UiBindUtil.SetLabel(row.SellButton, "팔기 " + _storage.SellPrice);
                UiBindUtil.SetLabel(row.SellAllButton, "일괄 팔기");
            }
        }

        // 최대 단계면 「담장 최대」 로 잠그고, 골드가 모자라면 눌리지 않게 한다.
        private void RefreshFenceButton()
        {
            int price = _farm.NextFencePrice;
            if (price < 0)
            {
                UiBindUtil.SetLabel(_fenceButton, "담장 최대");
                _fenceButton.interactable = false;
                return;
            }

            UiBindUtil.SetLabel(_fenceButton, "담장 늘리기 " + price + "G");
            _fenceButton.interactable = _wallet.CanPay(price);
        }

        private void OnBuyFence()
        {
            _farm.BuyFence();
            Refresh();
        }

        // 줄을 끌기 시작. 담장이 찼으면 버튼처럼 안 된다. 화면을 닫고 월드 쪽에 넘긴다.
        private void OnRowDragBegin(int index)
        {
            string id = _rows[index].SpeciesId;
            if (id == null || _farm.MozziCount >= _farm.FenceMax)
            {
                return;
            }

            IFarmViewHub view;
            if (InGameHub.TryGet<IFarmViewHub>(out view) == false)
            {
                return;
            }

            InGameHub.Get<IUiHub>().CloseAll();
            view.BeginPlaceDrag(id);
        }

        private void OnPlace(int index)
        {
            string id = _rows[index].SpeciesId;
            if (id != null)
            {
                _storage.PlaceToFarm(id);
            }

            Refresh();
        }

        private void OnSell(int index)
        {
            string id = _rows[index].SpeciesId;
            if (id != null)
            {
                _storage.Sell(id);
            }

            Refresh();
        }

        private void OnSellAll(int index)
        {
            string id = _rows[index].SpeciesId;
            if (id != null)
            {
                _storage.SellAll(id);
            }

            Refresh();
        }
    }
}
