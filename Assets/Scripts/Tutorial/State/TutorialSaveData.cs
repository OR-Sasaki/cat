using System;
using System.Collections.Generic;

namespace Tutorial.State
{
    /// 表示済みチュートリアルの ID 一覧。PlayerPrefs に JSON で永続化する
    [Serializable]
    public class TutorialSaveData
    {
        public List<string> shownIds = new();
    }
}
