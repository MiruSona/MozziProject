using LitMotion;
using Mozzi.Game.Stall;
using UnityEngine;

namespace Mozzi.Game.Ui
{
    // 손님 줄 칸 하나의 발끝 자리와 걷기 연출. 칸 뿌리(Customer_i)와 그 칸 말풍선을 같이 옮긴다.
    // 칸은 줄 번호에 묶여 있고 손님은 칸을 옮겨 다닌다 — 앞 손님이 빠지면 뒤 손님이 앞 칸 뿌리에 그려지고
    // 옛 칸 자리(지금 x)에서 새 칸 자리로 걸어간다. 그래서 「지금 이 칸에 누가 · 어디 서 있나」 를 여기서 쥔다.
    public sealed class CustomerWalkView
    {
        private readonly RectTransform _root;
        private readonly OrderBubbleView _bubble;
        private readonly float _homeX;

        private MotionHandle _walk;
        private float _fromX;
        private float _toX;

        // 지금 이 칸에 서 있는(또는 걸어오는) 손님. 비었으면 null.
        public StallCustomer Who { get; private set; }

        // 지금 발끝 x (걷는 중이면 걷는 자리).
        public float X { get; private set; }

        // 빈 MotionHandle(기본값)에 IsActive 를 묻지 않으려고 한 번이라도 걸었는지 따로 쥔다.
        private bool _hasWalk;

        public bool IsWalking
        {
            get { return _hasWalk && _walk.IsActive(); }
        }

        public CustomerWalkView(RectTransform customerRoot, OrderBubbleView bubble, float homeX)
        {
            _root = customerRoot;
            _bubble = bubble;
            _homeX = homeX;
            X = homeX;
        }

        // 걷지 않고 제 칸에 바로 세운다 (화면을 처음 열 때).
        public void Place(StallCustomer who)
        {
            Stop();
            Who = who;
            Apply(_homeX, 0f);
        }

        // fromX 에서 제 칸으로 걸어온다. 모션은 motions 에 같이 넣어 화면이 닫힐 때 한 번에 끊긴다.
        public void Walk(StallCustomer who, float fromX, IMotionScheduler scheduler, CompositeMotionHandle motions)
        {
            Stop();
            Who = who;
            _fromX = fromX;
            _toX = _homeX;
            Apply(fromX, 0f);

            _walk = LMotion.Create(0f, 1f, StallLayout.QueueWalkSec)
                .WithScheduler(scheduler)
                .Bind(this, ApplyWalk);
            _hasWalk = true;
            motions.Add(_walk);
        }

        // 칸을 비운다. 걷던 중이면 끊는다.
        public void Clear()
        {
            Stop();
            Who = null;
            Apply(_homeX, 0f);
        }

        // 말풍선을 새로 그린 뒤 지금 자리로 다시 붙인다 (말풍선은 늘 머리를 따라간다).
        public void SnapBubble()
        {
            Apply(X, 0f);
        }

        // 걷는 동안 x 는 고르게, y 는 QueueWalkHops 번 통통 뛴다. 자리는 도트가 안 깨지게 정수 px 로 반올림한다.
        private static void ApplyWalk(float t, CustomerWalkView self)
        {
            float x = Mathf.Lerp(self._fromX, self._toX, t);
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * StallLayout.QueueWalkHops)) * StallLayout.QueueHopHeight;
            self.Apply(x, hop);
        }

        private void Apply(float x, float hop)
        {
            X = x;
            float footX = Mathf.Round(x);
            float footY = StallLayout.QueueFootY - Mathf.Round(hop);
            _root.anchoredPosition = new Vector2(footX, -footY);
            _bubble.MoveTo(footX - StallLayout.HeadCenterOffset, footY - StallLayout.HeadHeight);
        }

        private void Stop()
        {
            if (IsWalking)
            {
                _walk.Cancel();
            }

            _hasWalk = false;
        }
    }
}
