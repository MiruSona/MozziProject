namespace Mozzi.Data
{
    // 화면·Manager 가 표를 꺼내 보는 창구. 표 8개를 그대로 내준다.
    public interface IStaticDataHub
    {
        DataTable<MozziSpeciesRow> Species { get; }

        DataTable<IngredientRow> Ingredients { get; }

        DataTable<RecipeRow> Recipes { get; }

        DataTable<SynthesisRuleRow> SynthesisRules { get; }

        DataTable<GrowthRow> Growths { get; }

        DataTable<StallRow> Stalls { get; }

        DataTable<FarmRow> Farms { get; }

        DataTable<FarmDecorRow> FarmDecors { get; }
    }
}
