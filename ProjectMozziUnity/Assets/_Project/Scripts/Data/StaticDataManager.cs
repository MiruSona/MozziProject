using System;
using System.Collections.Generic;
using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Data
{
    // Resources/Data/gamedata.bytes(datatool export 산출물)를 읽어 표 8개를 채운다. 키로 행 찾기만 하고 값 해석은 안 한다.
    // 원본은 ProjectMozziUnity/GameData/*.json 이다 (Docs/Guide/개발환경.md).
    public sealed class StaticDataManager : IManager, IStaticDataHub
    {
        // Resources.Load 는 확장자(.bytes)를 뺀 자리를 받는다.
        public const string BakedPath = "Data/gamedata";

        public DataTable<MozziSpeciesRow> Species { get; private set; }

        public DataTable<IngredientRow> Ingredients { get; private set; }

        public DataTable<RecipeRow> Recipes { get; private set; }

        public DataTable<SynthesisRuleRow> SynthesisRules { get; private set; }

        public DataTable<GrowthRow> Growths { get; private set; }

        public DataTable<StallRow> Stalls { get; private set; }

        public DataTable<FarmRow> Farms { get; private set; }

        public DataTable<FarmDecorRow> FarmDecors { get; private set; }

        public void Init()
        {
            GameDataTables tables = LoadBaked();
            Species = MakeTable("MozziSpecies", tables.MozziSpeciesRows, tables.MozziSpecies);
            Ingredients = MakeTable("Ingredient", tables.IngredientRows, tables.Ingredient);
            Recipes = MakeTable("Recipe", tables.RecipeRows, tables.Recipe);
            SynthesisRules = MakeTable("SynthesisRule", tables.SynthesisRuleRows, tables.SynthesisRule);
            Growths = MakeTable("Growth", tables.GrowthRows, tables.Growth);
            Stalls = MakeTable("Stall", tables.StallRows, tables.Stall);
            Farms = MakeTable("Farm", tables.FarmRows, tables.Farm);
            FarmDecors = MakeTable("FarmDecor", tables.FarmDecorRows, tables.FarmDecor);
        }

        public void PostInit()
        {
        }

        public void Dispose()
        {
        }

        // 안드로이드·WebGL 에서도 되게 StreamingAssets 가 아니라 Resources 의 TextAsset 바이트를 동기로 읽는다.
        // 스키마만 고치고 안 구웠으면 GameDataLoader 가 GameDataException 을 던진다.
        public static GameDataTables LoadBaked()
        {
            TextAsset asset = Resources.Load<TextAsset>(BakedPath);
            if (asset == null)
            {
                throw new InvalidOperationException("Resources 에 " + BakedPath + ".bytes 가 없다 — datatool export 를 돌려라");
            }

            return GameDataLoader.Load(asset.bytes);
        }

        // 행이 0개면 원본이 잘못 놓인 것이니 바로 던진다.
        private static DataTable<TRow> MakeTable<TRow>(string tableName, IReadOnlyList<TRow> rows,
            IReadOnlyDictionary<string, TRow> byId) where TRow : class
        {
            if (rows.Count == 0)
            {
                throw new InvalidOperationException(tableName + " 표에 행이 없다");
            }

            DataTable<TRow> table = new DataTable<TRow>(tableName, rows, byId);
            Debug.Log("[StaticData] " + tableName + " " + table.Count + "행");
            return table;
        }
    }
}
