namespace Mozzi.Resource
{
    // 전원 풀링이라 반납은 Destroy 가 아니다.
    // 정리 코드는 OnReturn 에 둔다.
    public interface IPooledObject
    {
        void OnRent();

        void OnReturn();
    }
}
