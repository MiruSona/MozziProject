using System;
using System.Collections.Generic;

namespace Mozzi.Game.Codex
{
    // CodexManager 의 세이브 칸. 열린 종 · 열린 레시피 · 해 본 조합(문자열 키).
    [Serializable]
    public sealed class CodexState
    {
        public List<string> openSpecies = new List<string>();
        public List<string> openRecipes = new List<string>();
        public List<string> triedCombos = new List<string>();
    }
}
