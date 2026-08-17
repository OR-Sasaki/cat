#nullable enable

namespace Root.AudioLogic
{
    /// SE 再生に使う AudioSource のインデックス選定ロジック。UnityEngine 非依存
    public static class SeSourcePicker
    {
        /// 空きがあれば最小インデックスの空きを、全て使用中なら startOrder が最小（最古）のインデックスを返す
        public static int Pick(bool[] isPlaying, int[] startOrder)
        {
            if (isPlaying.Length == 0)
                return -1;

            for (var i = 0; i < isPlaying.Length; i++)
            {
                if (!isPlaying[i])
                    return i;
            }

            var oldestIndex = 0;
            for (var i = 1; i < startOrder.Length; i++)
            {
                if (startOrder[i] < startOrder[oldestIndex])
                    oldestIndex = i;
            }

            return oldestIndex;
        }
    }
}
