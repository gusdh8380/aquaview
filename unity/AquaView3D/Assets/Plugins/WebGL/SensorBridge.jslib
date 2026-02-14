mergeInto(LibraryManager.library, {
  // 이 파일은 Unity WebGL 빌드에 자동 포함됩니다.
  // React에서 보내는 postMessage를 수신하여 Unity C#으로 전달합니다.
});

// Unity가 로드된 후 postMessage 리스너를 등록합니다.
// index.html의 <script>에서 직접 등록하는 방식을 사용합니다.
