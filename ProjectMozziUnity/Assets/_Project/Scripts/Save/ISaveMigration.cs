namespace Mozzi.Save
{
    // 세이브 파일을 한 판 올리는 단계 하나. FromVersion 판 파일을 받아 FromVersion + 1 판 꼴로 고친다.
    // version 숫자는 SaveMigrator 가 올리니 단계는 내용만 고친다.
    public interface ISaveMigration
    {
        int FromVersion { get; }

        void Apply(SaveFile file);
    }
}
