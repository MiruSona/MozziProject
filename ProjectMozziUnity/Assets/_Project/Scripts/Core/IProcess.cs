namespace Mozzi.Core
{
    // 매 프레임 도는 것만 단다. 안 쓰는 Manager 가 빈 함수를 달지 않게 셋으로 갈랐다.
    public interface IProcess
    {
        void Process(float deltaTime);
    }

    public interface IFixedProcess
    {
        void FixedProcess(float deltaTime);
    }

    public interface ILateProcess
    {
        void LateProcess(float deltaTime);
    }
}
