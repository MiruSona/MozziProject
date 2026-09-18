using System;
using Mozzi.Core;
using Mozzi.Data.Rows;
using UnityEngine;

namespace Mozzi.Data
{
    // Resources/Data/*.json 을 읽어 표 7개를 채운다. 키로 행 찾기만 하고 값 해석은 안 한다.
    public sealed class StaticDataManager : IManager, IStaticDataHub
    {
        private const string DataFolder = "Data/";

        public DataTable<MozziSpeciesRow> Species { get; private set; }

        public DataTable<IngredientRow> Ingredients { get; private set; }

        public DataTable<RecipeRow> Recipes { get; private set; }

        public DataTable<SynthesisRuleRow> SynthesisRules { get; private set; }

        public DataTable<GrowthRow> Growths { get; private set; }

        public DataTable<StallRow> Stalls { get; private set; }

        public DataTable<FarmRow> Farms { get; private set; }

        public void Init()
        {
            Species = LoadTable<MozziSpeciesRow>("MozziSpecies");
            Ingredients = LoadTable<IngredientRow>("Ingredient");
            Recipes = LoadTable<RecipeRow>("Recipe");
            SynthesisRules = LoadTable<SynthesisRuleRow>("SynthesisRule");
            Growths = LoadTable<GrowthRow>("Growth");
            Stalls = LoadTable<StallRow>("Stall");
            Farms = LoadTable<FarmRow>("Farm");
        }

        public void PostInit()
        {
        }

        public void Dispose()
        {
        }

        // 파일이 없거나 행이 0개면 JSON 이 잘못 놓인 것이니 바로 던진다.
        private static DataTable<TRow> LoadTable<TRow>(string tableName) where TRow : class, IDataRow
        {
            string path = DataFolder + tableName;
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Resources 에 " + path + ".json 이 없다");
            }

            DataTableJson<TRow> json = JsonUtility.FromJson<DataTableJson<TRow>>(asset.text);
            if (json == null || json.rows == null || json.rows.Count == 0)
            {
                throw new InvalidOperationException(path + ".json 에 rows 가 비었다");
            }

            DataTable<TRow> table = new DataTable<TRow>(tableName, json.rows);
            Debug.Log("[StaticData] " + tableName + " " + table.Count + "행");
            return table;
        }
    }
}
