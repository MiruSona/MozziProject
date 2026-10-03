namespace Mozzi.Save
{
    // 1판 → 2판 : 설정 구역(settings)이 생겼다. 1판 파일에는 없으니 기본값으로 채운다. 진행 칸은 그대로다.
    public sealed class SaveMigrationV1ToV2 : ISaveMigration
    {
        public int FromVersion
        {
            get { return 1; }
        }

        public void Apply(SaveFile file)
        {
            if (file.settings == null)
            {
                file.settings = new SaveSettings();
            }
        }
    }
}
