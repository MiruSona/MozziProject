using System.Collections.Generic;
using Mozzi.Data;

namespace Mozzi.Game.Synthesis
{
    // 합성 규칙의 무상태 계산 (합성소 문서 4절). 난수는 밖에서 0~1 로 받는다.
    // 교차 : 잔디+물 둘 다 2단계 이상이면 늪 1단계, 그 밖의 다른 계열은 첫 슬롯 계열의 다음 단계.
    public static class SynthesisRollUtil
    {
        public const string FamilyGrass = "grass";
        public const string FamilyWater = "water";
        public const string FamilySwamp = "swamp";

        // 비용·대기 단계는 재료 단계 중 높은 쪽이다.
        public static int MaterialTier(MozziSpeciesRow a, MozziSpeciesRow b)
        {
            if (a.Tier >= b.Tier)
            {
                return a.Tier;
            }

            return b.Tier;
        }

        public static bool IsSwampCross(MozziSpeciesRow a, MozziSpeciesRow b)
        {
            if (a.Tier < 2 || b.Tier < 2)
            {
                return false;
            }

            bool grassWater = a.Family == FamilyGrass && b.Family == FamilyWater;
            bool waterGrass = a.Family == FamilyWater && b.Family == FamilyGrass;
            return grassWater || waterGrass;
        }

        // ① 정해진 종. 첫 슬롯이 3단계면 다음 단계가 없어 null.
        public static MozziSpeciesRow FindFixed(DataTable<MozziSpeciesRow> species, MozziSpeciesRow a, MozziSpeciesRow b)
        {
            if (IsSwampCross(a, b))
            {
                return FindBase(species, FamilySwamp, 1);
            }

            return FindBase(species, a.Family, a.Tier + 1);
        }

        public static MozziSpeciesRow FindBase(DataTable<MozziSpeciesRow> species, string family, int tier)
        {
            return FindRow(species, family, tier, false);
        }

        public static MozziSpeciesRow FindShiny(DataTable<MozziSpeciesRow> species, string family, int tier)
        {
            return FindRow(species, family, tier, true);
        }

        // ② 후보 : 재료 둘과 다른 계열의 1단계 일반 종. 늪은 한 번 열린 뒤에만 든다.
        public static void CollectOtherFamily(DataTable<MozziSpeciesRow> species, MozziSpeciesRow a, MozziSpeciesRow b,
            bool swampOpen, List<MozziSpeciesRow> into)
        {
            into.Clear();
            IReadOnlyList<MozziSpeciesRow> rows = species.All;
            for (int i = 0; i < rows.Count; i++)
            {
                MozziSpeciesRow row = rows[i];
                if (row.Tier != 1 || row.IsShiny)
                {
                    continue;
                }

                if (row.Family == a.Family || row.Family == b.Family)
                {
                    continue;
                }

                if (row.Family == FamilySwamp && swampOpen == false)
                {
                    continue;
                }

                into.Add(row);
            }
        }

        // roll 은 0~1. ①이 없으면(3단계 2마리) Top 확률로 ②·③ 만 가른다.
        public static SynthesisBranch PickBranch(SynthesisRuleRow rule, bool hasFixed, float roll)
        {
            if (hasFixed == false)
            {
                if (roll < rule.POtherFamilyTop)
                {
                    return SynthesisBranch.OtherFamily;
                }

                return SynthesisBranch.Shiny;
            }

            if (roll < rule.PFixed)
            {
                return SynthesisBranch.Fixed;
            }

            if (roll < rule.PFixed + rule.POtherFamily)
            {
                return SynthesisBranch.OtherFamily;
            }

            return SynthesisBranch.Shiny;
        }

        private static MozziSpeciesRow FindRow(DataTable<MozziSpeciesRow> species, string family, int tier, bool shiny)
        {
            IReadOnlyList<MozziSpeciesRow> rows = species.All;
            for (int i = 0; i < rows.Count; i++)
            {
                MozziSpeciesRow row = rows[i];
                if (row.Family == family && row.Tier == tier && row.IsShiny == shiny)
                {
                    return row;
                }
            }

            return null;
        }
    }
}
