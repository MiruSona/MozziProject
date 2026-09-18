// WebGL 의 persistentDataPath(IDBFS) 는 메모리에 먼저 쓰이고, syncfs 를 불러야 IndexedDB 에 남는다.
// 안 부르면 탭을 닫을 때 세이브가 사라진다. Save/WebGlFsUtil.cs 가 부른다.
mergeInto(LibraryManager.library, {
    MozziSyncFs: function () {
        FS.syncfs(false, function (err) {
            if (err) {
                console.error("[Save] syncfs 실패 : " + err);
            }
        });
    }
});
