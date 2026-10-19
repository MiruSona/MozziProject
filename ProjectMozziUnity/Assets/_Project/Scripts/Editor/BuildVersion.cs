namespace Mozzi.Editor
{
    // 안드로이드·윈도우 빌드가 같이 쓰는 판 번호. 판을 낼 때는 이 파일만 고친다.
    //
    // Version : 사람이 읽는 판. Release 태그와 빌드 파일 이름에 붙는다.
    //   - 프로토타입 동안 작은 갱신은 끝자리를 올린다 (0.0.1 → 0.0.2).
    //   - 중간 갱신은 가운데 자리를 올리고 끝자리는 0 으로 (0.0.5 → 0.1.0).
    //   - 첫자리는 알파 → 베타 → 정식처럼 단계가 바뀔 때만 올린다 (→ 1.0.0).
    // VersionCode : 안드로이드 versionCode. 판을 낼 때마다 1 올린다.
    //   폰은 이 수가 더 커야 덮어 깔기를 받아 주므로 내리거나 되돌리지 않는다.
    public static class BuildVersion
    {
        public const string Version = "0.0.1";
        public const int VersionCode = 18;
    }
}
