import { useRef, useEffect } from "react";

export default function Unity3DView({ sensors }) {
  const iframeRef = useRef(null);

  useEffect(() => {
    if (!sensors || sensors.length === 0) return;
    const iframe = iframeRef.current;
    if (!iframe || !iframe.contentWindow) return;

    iframe.contentWindow.postMessage(
      { type: "SENSOR_UPDATE", sensors },
      "*"
    );
  }, [sensors]);

  return (
    <div className="unity-view-container">
      <h2 className="section-title">🏭 3D 공정 뷰</h2>
      <div className="unity-frame-wrapper">
        <iframe
          ref={iframeRef}
          src="/unity/AquaView3D/index.html"
          title="AquaView 3D"
          className="unity-iframe"
          allow="autoplay; fullscreen"
        />
        <div className="unity-overlay-hint">
          {!sensors || sensors.length === 0 ? "데이터 로딩 중..." : ""}
        </div>
      </div>
    </div>
  );
}
