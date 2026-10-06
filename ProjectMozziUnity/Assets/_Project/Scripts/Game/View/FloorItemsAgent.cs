using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Game.Farm;
using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 바닥 재료(낱개 · 꾸러미)를 맡는다. 모델은 IFarmHub 의 재료별 더미 수 하나뿐이고 바닥 자리는 화면만의 것이다.
    // 떨굼 이벤트가 오면 그 종 찹쌀이 발밑에서 낱개를 튀어 나오게 한다. 근처에 같은 재료 더미가 있으면 그 위로 뛰어 합쳐진다.
    // 매 프레임 화면 합계를 모델에 맞춘 뒤 30 을 넘으면 뭉친다.
    // 꾸러미 하나는 PileMax(5) 개까지다 — 맞추기도 뭉치기도 그 수를 안 넘긴다.
    // 셈은 FloorItemMath 가 하고 여기서는 오브젝트만 빌리고 돌린다.
    public sealed class FloorItemsAgent : AgentBase<FarmViewManager>
    {
        // 떨군 낱개가 발밑에서 퍼지는 폭 (px). 같은 마리가 여러 개 떨궈도 한 점에 안 쌓이게.
        private const float DropSpreadXPx = 14f;
        private const float DropSpreadYPx = 5f;

        // 발밑에서 이 거리 (px) 안에 같은 재료 더미(PileMax 미만)가 있으면 그 위로 떨어져 합쳐진다. 꾸러미 그림 두 개 폭이다.
        private const float DropMergeRadiusPx = 56f;

        private readonly IResourceHub _resource;
        private readonly IFarmHub _farm;
        private readonly IStaticDataHub _data;
        private readonly Transform _root;
        private readonly Rect _floor;

        // 셀 수 있는 것(줍기 전)과 사라지는 반응 중인 것을 나눠 든다. 사라지는 것은 모델에서 이미 빠졌다.
        private readonly List<FloorItemObject> _items = new List<FloorItemObject>();
        private readonly List<FloorItemObject> _vanishing = new List<FloorItemObject>();

        // 셈용 재사용 목록. 매 프레임 새로 만들지 않는다.
        private readonly List<FloorItemEntry> _entries = new List<FloorItemEntry>();
        private readonly List<int> _amounts = new List<int>();
        private readonly List<MozziObject> _droppers = new List<MozziObject>();

        // 더미 위로 떨어지는 중인 낱개와 그 더미. 내려앉으면 합친다.
        private readonly List<FloorItemObject> _landing = new List<FloorItemObject>();
        private readonly List<FloorItemObject> _landingTargets = new List<FloorItemObject>();

        // floor 는 바닥 재료 가운데가 놓일 수 있는 네모다. 그림이 울타리에 안 걸리게 Owner 가 줄여서 준다.
        public FloorItemsAgent(FarmViewManager owner, IResourceHub resource, IFarmHub farm, IStaticDataHub data, Transform root,
            Rect floor)
            : base(owner)
        {
            _resource = resource;
            _farm = farm;
            _data = data;
            _root = root;
            _floor = floor;
        }

        // 떨군 종의 찹쌀이 하나를 골라 그 발밑에서 낱개를 튀어 나오게 한다.
        // 고를 마리가 없으면(모두 들려 있음 등) 그냥 둔다 — 다음 맞추기가 모자란 만큼을 꾸러미로 깐다.
        public void OnDropped(string ingredientId, string speciesId, IReadOnlyList<MozziObject> mozzis)
        {
            _droppers.Clear();
            for (int i = 0; i < mozzis.Count; i++)
            {
                MozziObject mozzi = mozzis[i];
                if (mozzi.SpeciesId == speciesId && mozzi.IsDragging == false && mozzi.IsSliding == false)
                {
                    _droppers.Add(mozzi);
                }
            }

            if (_droppers.Count == 0)
            {
                return;
            }

            Vector2 foot = _droppers[Random.Range(0, _droppers.Count)].FootWorld;
            BuildEntries();
            int target = FloorItemMath.FindDropTarget(_entries, ingredientId, foot, LayoutConst.ToUnits(DropMergeRadiusPx), _farm.PileMax);
            if (target >= 0)
            {
                FloorItemObject onto = Spawn(ingredientId, _items[target].Position, 1);
                onto.PlayDrop(foot);
                _landing.Add(onto);
                _landingTargets.Add(_items[target]);
                return;
            }

            Vector2 land = foot + new Vector2(
                Random.Range(-1f, 1f) * LayoutConst.ToUnits(DropSpreadXPx),
                Random.Range(-1f, 1f) * LayoutConst.ToUnits(DropSpreadYPx));
            land.x = Mathf.Clamp(land.x, _floor.xMin, _floor.xMax);
            land.y = Mathf.Clamp(land.y, _floor.yMin, _floor.yMax);

            FloorItemObject item = Spawn(ingredientId, land, 1);
            item.PlayDrop(foot);
        }

        // 수집기가 그 재료를 count 개 걷었다 (모델에서는 이미 빠졌다). 맞추기(Reconcile)가 반응 없이 걷기 전에
        // 같은 규칙(새것부터)으로 깎되, 다 걷힌 꾸러미는 mouth(수집기 나팔)로 빨려 들고,
        // 일부만 걷힌 꾸러미는 걷힌 몫을 따로 떼어 날린다. 화면에 모자란 몫은 그대로 둔다 — 다음 맞추기가 맞춘다.
        public void OnCollected(string ingredientId, int count, Vector2 mouth)
        {
            BuildEntries();
            int shown = FloorItemMath.ShownOf(_entries, ingredientId);
            int need = Mathf.Min(count, shown);
            if (need <= 0)
            {
                return;
            }

            FloorItemMath.PlanRemove(_entries, ingredientId, need, _amounts);
            for (int i = _amounts.Count - 1; i >= 0; i--)
            {
                FloorItemObject item = _items[i];
                if (_amounts[i] == item.Amount)
                {
                    continue;
                }

                if (_amounts[i] <= 0)
                {
                    _items.RemoveAt(i);
                    _vanishing.Add(item);
                    item.PlaySucked(mouth);
                    continue;
                }

                FloorItemObject piece = Spawn(ingredientId, item.Position, item.Amount - _amounts[i]);
                _items.Remove(piece);
                _vanishing.Add(piece);
                piece.PlaySucked(mouth);
                item.SetAmount(_amounts[i]);
            }
        }

        public void Step(float deltaTime)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Step(deltaTime);
            }

            for (int i = _vanishing.Count - 1; i >= 0; i--)
            {
                FloorItemObject item = _vanishing[i];
                item.Step(deltaTime);
                if (item.IsGone)
                {
                    _vanishing.RemoveAt(i);
                    _resource.Return(item);
                }
            }

            MergeLanded();
            Reconcile();
            MergeOverflow();
        }

        // 하나를 줍는다. 다 들어가면 튀고 사라지고, 창고가 차서 덜 들어가면 남은 양으로 줄고 흔들린다.
        public void Pick(FloorItemObject item)
        {
            if (item == null || item.IsPickable == false)
            {
                return;
            }

            int moved = _farm.PickUp(item.IngredientId, item.Amount);
            if (moved >= item.Amount)
            {
                _items.Remove(item);
                _vanishing.Add(item);
                item.PlayPicked();
                return;
            }

            if (moved > 0)
            {
                item.SetAmount(item.Amount - moved);
            }

            item.PlayShake();
        }

        // 훑기. 선분 a-b 에서 손가락 반지름 + 그림 반쪽 안에 가운데가 드는 재료를 모두 줍는다.
        // Pick 은 i 번째 하나만 빼므로 뒤에서부터 돌면 안전하다.
        public void PickAlong(Vector2 a, Vector2 b)
        {
            float reach = LayoutConst.ToUnits(LayoutConst.SweepRadiusPx + LayoutConst.PilePx * 0.5f);
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                FloorItemObject item = _items[i];
                if (FloorItemMath.DistanceToSegment(a, b, item.Position) > reach)
                {
                    continue;
                }

                Pick(item);
            }
        }

        // 화면을 닫을 때. 플레이를 멈출 때는 Unity 가 먼저 부수므로 이미 없는 것은 반납하지 않는다.
        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                ReturnIfAlive(_items[i]);
            }

            for (int i = 0; i < _vanishing.Count; i++)
            {
                ReturnIfAlive(_vanishing[i]);
            }

            _items.Clear();
            _vanishing.Clear();
            _landing.Clear();
            _landingTargets.Clear();
        }

        // 더미 위로 떨어진 낱개가 내려앉으면 그 더미에 더한다. 그 사이 둘 중 하나를 주웠거나 더미가 찼으면 따로 둔다.
        private void MergeLanded()
        {
            for (int i = _landing.Count - 1; i >= 0; i--)
            {
                FloorItemObject item = _landing[i];
                if (item.IsDropping && _items.Contains(item))
                {
                    continue;
                }

                FloorItemObject target = _landingTargets[i];
                _landing.RemoveAt(i);
                _landingTargets.RemoveAt(i);

                // 풀에서 다시 빌려 다른 재료가 됐을 수도 있어 재료도 본다.
                bool bothHere = _items.Contains(item) && _items.Contains(target) && target.IngredientId == item.IngredientId;
                if (bothHere == false || target.Amount + item.Amount > _farm.PileMax)
                {
                    continue;
                }

                target.SetAmount(target.Amount + item.Amount);
                _items.Remove(item);
                _resource.Return(item);
            }
        }

        // 화면 합계를 모델에 맞춘다. 모델이 많으면(오프라인 · 디버그 Skip · 떨군 마리를 못 찾음) 차이를 PileMax 개씩 꾸러미로 깔고,
        // 적으면(다른 곳에서 주움) 새것부터 깎는다.
        private void Reconcile()
        {
            IReadOnlyList<IngredientRow> rows = _data.Ingredients.All;
            BuildEntries();

            for (int r = 0; r < rows.Count; r++)
            {
                string id = rows[r].Id;
                int model = _farm.PileOf(id);
                int shown = FloorItemMath.ShownOf(_entries, id);
                if (model == shown)
                {
                    continue;
                }

                if (model > shown)
                {
                    FloorItemMath.SplitIntoPiles(model - shown, _farm.PileMax, _amounts);
                    for (int i = 0; i < _amounts.Count; i++)
                    {
                        Spawn(id, RandomFloorSpot(), _amounts[i]);
                    }
                }
                else
                {
                    FloorItemMath.PlanRemove(_entries, id, shown - model, _amounts);
                    ApplyAmounts();
                }

                BuildEntries();
            }
        }

        // 보이는 수가 상한을 넘으면 합쳐도 꾸러미 한도(PileMax)를 안 넘는 가장 가까운 같은 재료 둘을 합친다.
        // 양은 더하고 자리는 큰 쪽이다. 합칠 짝이 없으면(모두 꽉 찬 꾸러미) 상한을 넘은 채로 둔다.
        private void MergeOverflow()
        {
            while (_items.Count > LayoutConst.FloorItemMax)
            {
                BuildEntries();
                int keep;
                int drop;
                if (FloorItemMath.FindMergePair(_entries, _farm.PileMax, out keep, out drop) == false)
                {
                    return;
                }

                FloorItemObject kept = _items[keep];
                FloorItemObject dropped = _items[drop];
                kept.SetAmount(kept.Amount + dropped.Amount);
                _items.RemoveAt(drop);
                _resource.Return(dropped);
            }
        }

        // PlanRemove 결과를 오브젝트에 옮긴다. 0 이 된 칸은 반응 없이 바로 걷는다 — 다른 곳에서 이미 주운 몫이다.
        private void ApplyAmounts()
        {
            for (int i = _amounts.Count - 1; i >= 0; i--)
            {
                FloorItemObject item = _items[i];
                if (_amounts[i] == item.Amount)
                {
                    continue;
                }

                if (_amounts[i] <= 0)
                {
                    _items.RemoveAt(i);
                    _resource.Return(item);
                    continue;
                }

                item.SetAmount(_amounts[i]);
            }
        }

        private void BuildEntries()
        {
            _entries.Clear();
            for (int i = 0; i < _items.Count; i++)
            {
                FloorItemObject item = _items[i];
                _entries.Add(new FloorItemEntry(item.IngredientId, item.Amount, item.Position));
            }
        }

        private FloorItemObject Spawn(string ingredientId, Vector2 position, int amount)
        {
            IngredientRow row = _data.Ingredients.Get(ingredientId);
            FloorItemObject item = _resource.Rent<FloorItemObject>(PrefabKeys.FloorItem, _root);
            item.Setup(row, _resource.GetSprite(row.SpriteKey), position, amount);
            _items.Add(item);
            return item;
        }

        private Vector2 RandomFloorSpot()
        {
            return new Vector2(Random.Range(_floor.xMin, _floor.xMax), Random.Range(_floor.yMin, _floor.yMax));
        }

        private void ReturnIfAlive(FloorItemObject item)
        {
            if (item == null)
            {
                return;
            }

            _resource.Return(item);
        }
    }
}
