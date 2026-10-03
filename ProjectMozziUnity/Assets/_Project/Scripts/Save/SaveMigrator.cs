using System;
using System.Collections.Generic;

namespace Mozzi.Save
{
    // 옛 판 세이브를 지금 판까지 한 판씩 올린다. 단계 등록은 CreateSteps 한 자리다.
    // 새 판을 만들 때 : SaveFile.CurrentVersion 을 올리고, SaveMigrationV(N)ToV(N+1) 을 만들어 여기에 더한다.
    public static class SaveMigrator
    {
        public static List<ISaveMigration> CreateSteps()
        {
            List<ISaveMigration> steps = new List<ISaveMigration>();
            steps.Add(new SaveMigrationV1ToV2());
            return steps;
        }

        // file 을 targetVersion 까지 올린다. 맞는 단계가 없거나 단계가 터지면 false 와 까닭을 준다 (file 은 고치다 만 채다).
        public static bool TryUpgrade(SaveFile file, List<ISaveMigration> steps, int targetVersion, out string error)
        {
            error = null;

            while (file.version < targetVersion)
            {
                ISaveMigration step = FindStep(steps, file.version);
                if (step == null)
                {
                    error = file.version + "판에서 올리는 단계가 없다";
                    return false;
                }

                try
                {
                    step.Apply(file);
                }
                catch (Exception e)
                {
                    error = file.version + "판 → " + (file.version + 1) + "판 단계 실패 : " + e.Message;
                    return false;
                }

                file.version++;
            }

            return true;
        }

        private static ISaveMigration FindStep(List<ISaveMigration> steps, int fromVersion)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].FromVersion == fromVersion)
                {
                    return steps[i];
                }
            }

            return null;
        }
    }
}
