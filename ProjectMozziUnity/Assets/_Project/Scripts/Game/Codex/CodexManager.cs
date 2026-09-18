using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Events;
using Mozzi.Game.Storage;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Codex
{
    // 찹쌀이 도감 · 레시피 도감 · 해 본 조합 · 짝 구매. 시작 종(Farm 표 startSpeciesId)은 처음부터 열려 있다.
    // 짝 구매는 열린 1·2단계 일반 종만이고 값은 Growth 표 pair<단계> 다 (합성소 문서 6절).
    public sealed class CodexManager : IManager, ISavable, ICodexHub
    {
        private const string Key = "Codex";
        private const string FarmRowId = DataKeys.DefaultRowId;

        private readonly List<string> _openSpecies = new List<string>();
        private readonly HashSet<string> _openSpeciesSet = new HashSet<string>();
        private readonly HashSet<string> _openRecipes = new HashSet<string>();
        private readonly HashSet<string> _triedCombos = new HashSet<string>();

        private IStaticDataHub _data;
        private ISaveHub _save;
        private IWalletHub _wallet;
        private IStorageHub _storage;

        public string SaveKey
        {
            get { return Key; }
        }

        public IReadOnlyList<string> OpenSpecies
        {
            get { return _openSpecies; }
        }

        public void Init()
        {
        }

        public void PostInit()
        {
            _data = InGameHub.Get<IStaticDataHub>();
            _save = InGameHub.Get<ISaveHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _storage = InGameHub.Get<IStorageHub>();

            _save.Register(this);
            OpenQuietly(_data.Farms.Get(FarmRowId).startSpeciesId);
            Debug.Log("[Codex] 찹쌀이 " + _openSpecies.Count + "/" + _data.Species.Count
                + " · 레시피 " + _openRecipes.Count + "/" + _data.Recipes.Count);
        }

        public void Dispose()
        {
        }

        public bool IsOpen(string speciesId)
        {
            return _openSpeciesSet.Contains(speciesId);
        }

        public bool IsFamilyOpen(string family)
        {
            for (int i = 0; i < _openSpecies.Count; i++)
            {
                if (_data.Species.Get(_openSpecies[i]).family == family)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Discover(string speciesId)
        {
            if (OpenQuietly(speciesId) == false)
            {
                return false;
            }

            EventBus.Publish(new MozziDiscoveredEvent(speciesId, _openSpecies.Count));
            return true;
        }

        public bool IsRecipeOpen(string recipeId)
        {
            return _openRecipes.Contains(recipeId);
        }

        public bool DiscoverRecipe(string recipeId)
        {
            _data.Recipes.Get(recipeId);
            if (_openRecipes.Add(recipeId) == false)
            {
                return false;
            }

            EventBus.Publish(new RecipeDiscoveredEvent(recipeId, _openRecipes.Count));
            return true;
        }

        public bool HasTriedCombo(string comboKey)
        {
            return _triedCombos.Contains(comboKey);
        }

        public void MarkTriedCombo(string comboKey)
        {
            _triedCombos.Add(comboKey);
        }

        public int PairPrice(string speciesId)
        {
            if (IsOpen(speciesId) == false)
            {
                return -1;
            }

            MozziSpeciesRow species = _data.Species.Get(speciesId);
            if (species.isShiny || species.tier >= 3)
            {
                return -1;
            }

            GrowthRow growth = GrowthUtil.Find(_data.Growths, GrowthUtil.KindPair, species.tier);
            if (growth == null)
            {
                return -1;
            }

            return growth.price;
        }

        public bool BuyPair(string speciesId)
        {
            int price = PairPrice(speciesId);
            if (price < 0)
            {
                return false;
            }

            if (_wallet.Pay(price) == false)
            {
                return false;
            }

            _storage.Add(speciesId, 1);
            return true;
        }

        public string CaptureJson()
        {
            CodexState state = new CodexState();
            state.openSpecies.AddRange(_openSpecies);
            state.openRecipes.AddRange(_openRecipes);
            state.triedCombos.AddRange(_triedCombos);
            return JsonUtility.ToJson(state);
        }

        // 표에 없는 id 는 버린다 (표가 바뀐 옛 세이브).
        public void RestoreJson(string json)
        {
            CodexState state = JsonUtility.FromJson<CodexState>(json);
            if (state == null)
            {
                return;
            }

            for (int i = 0; i < state.openSpecies.Count; i++)
            {
                OpenQuietly(state.openSpecies[i]);
            }

            for (int i = 0; i < state.openRecipes.Count; i++)
            {
                RecipeRow row;
                if (_data.Recipes.TryGet(state.openRecipes[i], out row))
                {
                    _openRecipes.Add(row.id);
                }
            }

            for (int i = 0; i < state.triedCombos.Count; i++)
            {
                _triedCombos.Add(state.triedCombos[i]);
            }
        }

        // 이벤트 없이 연다. 복원·시작 상태용. 표에 없으면 false.
        private bool OpenQuietly(string speciesId)
        {
            MozziSpeciesRow row;
            if (_data.Species.TryGet(speciesId, out row) == false)
            {
                return false;
            }

            if (_openSpeciesSet.Add(speciesId) == false)
            {
                return false;
            }

            _openSpecies.Add(speciesId);
            return true;
        }
    }
}
