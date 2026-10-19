using System.Collections.Generic;
using Mozzi.Save;

namespace Mozzi.Game.Storage
{
    // 보관함 창구. 종 id 별 마릿수이고 상한은 없다 (농장 문서 7절).
    public interface IStorageHub
    {
        // 마릿수가 1 이상인 종만. 처음 들어온 순서다.
        IReadOnlyList<CountEntry> Entries { get; }

        int Count(string speciesId);

        void Add(string speciesId, int amount);

        // 한 마리를 뺀다. 없으면 false.
        bool Take(string speciesId);

        // 담장이 차면 false 를 주고 보관함은 그대로.
        bool PlaceToFarm(string speciesId);

        // 농장의 한 마리를 보관함으로. 농장에 없으면 false.
        bool TakeFromFarm(int mozziId);

        int SellPrice { get; }

        bool Sell(string speciesId);

        // 그 종 전부를 판다. 판 마릿수를 준다.
        int SellAll(string speciesId);
    }
}
