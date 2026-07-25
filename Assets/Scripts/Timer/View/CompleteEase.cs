namespace Timer.View
{
    /// タイマー完了演出で共有するイージング関数。
    /// 演出ごとに AnimationCurve を持たせるとインスペクタ上で崩されやすいため、
    /// 「どう動いてほしいか」が名前で決まる関数として固定する。
    static class CompleteEase
    {
        /// 減速のみ。パネルがすっと定位置に収まる動きに使う
        public static float OutCubic(float t)
        {
            var inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        /// 行き過ぎてから戻る。「ニュッ」と飛び出す動きに使う
        public static float OutBack(float t, float overshoot)
        {
            var inverse = t - 1f;
            return inverse * inverse * ((overshoot + 1f) * inverse + overshoot) + 1f;
        }

        /// 逆方向へ助走してから加速する。画面外へ引っ込む動きに使う
        public static float InBack(float t, float overshoot)
        {
            return t * t * ((overshoot + 1f) * t - overshoot);
        }
    }
}
