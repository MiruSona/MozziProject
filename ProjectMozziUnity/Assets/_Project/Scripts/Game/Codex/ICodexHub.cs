using System.Collections.Generic;

namespace Mozzi.Game.Codex
{
    // 도감 창구. 찹쌀이 24칸 · 레시피 10칸 · 해 본 조합 · 짝 구매.
    public interface ICodexHub
    {
        IReadOnlyList<string> OpenSpecies { get; }

        bool IsOpen(string speciesId);

        // 그 계열의 종이 하나라도 열렸나. 늪처럼 숨은 계열이 ② 후보에 드는지 볼 때 쓴다.
        bool IsFamilyOpen(string family);

        // 새로 열리면 true 와 MozziDiscoveredEvent.
        bool Discover(string speciesId);

        bool IsRecipeOpen(string recipeId);

        bool DiscoverRecipe(string recipeId);

        bool HasTriedCombo(string comboKey);

        void MarkTriedCombo(string comboKey);

        // 살 수 없는 종이면 -1 (안 열림 · 변종 · 3단계).
        int PairPrice(string speciesId);

        // 골드를 빼고 보관함에 1마리. 못 사면 false 와 아무 변화 없음.
        bool BuyPair(string speciesId);
    }
}
