namespace Mozzi.Resource
{
    // 프리팹 키는 코드 상수로만 둔다. 문자열을 흩뿌리지 않는다.
    // Resources 폴더 기준 경로다. 실제로 있는지는 메뉴 Tools/PrefabKeys 검사 로 본다.
    public static class PrefabKeys
    {
        public const string Mozzi = "Prefabs/Mozzi";
        // 바닥 재료(FloorItemObject). 프리팹 파일 이름은 옛 이름 그대로다.
        public const string FloorItem = "Prefabs/IngredientPile";
        public const string Building = "Prefabs/Building";
        public const string UiFarmScreen = "Prefabs/UI/FarmScreen";
        public const string UiSynthesisScreen = "Prefabs/UI/SynthesisScreen";
        public const string UiMozziCodexScreen = "Prefabs/UI/MozziCodexScreen";
        public const string UiStorageScreen = "Prefabs/UI/StorageScreen";
        public const string UiStallScreen = "Prefabs/UI/StallScreen";
        public const string UiKitchenScreen = "Prefabs/UI/KitchenScreen";
        public const string UiRecipeCodexScreen = "Prefabs/UI/RecipeCodexScreen";
        public const string UiOfflinePopup = "Prefabs/UI/OfflinePopup";
        public const string UiMozziPopup = "Prefabs/UI/MozziPopup";
        public const string UiDebugPanel = "Prefabs/UI/DebugPanel";
    }
}
