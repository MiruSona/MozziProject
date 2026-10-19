namespace Mozzi.Save
{
    // 1판 → 2판 : 고칠 것이 없다. 1판 파일도 2판으로 올라가야 하니 빈 단계로 남긴다.
    public sealed class SaveMigrationV1ToV2 : ISaveMigration
    {
        public int FromVersion
        {
            get { return 1; }
        }

        public void Apply(SaveFile file)
        {
        }
    }
}
