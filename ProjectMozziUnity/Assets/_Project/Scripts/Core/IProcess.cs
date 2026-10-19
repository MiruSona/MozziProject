namespace Mozzi.Core
{
    // 매 프레임 도는 것만 단다.
    public interface IProcess
    {
        void Process(float deltaTime);
    }
}
