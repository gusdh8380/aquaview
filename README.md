# 💧 AquaView

**수처리 공정 모니터링 시스템** — 실시간 센서 대시보드 + 하수 처리 파이프라인 시뮬레이션 + Unity 3D 시각화를 결합한 풀스택 포트폴리오 프로젝트

## 🎯 프로젝트 개요

실제 EPA/WEF 수처리 공학 데이터를 기반으로, 수처리 시설의 센서 모니터링과 5단계 하수 처리 공정 시뮬레이션을 인터랙티브하게 시각화한 대시보드입니다.

- **실시간 센서 모니터링**: pH, 탁도, 유량, 수온 — 3초 폴링, 상태별 경보
- **파이프라인 시뮬레이션**: HRT 슬라이더 조절 → BOD/TSS/COD/NH₃ 연쇄 계산
- **Unity 3D 뷰**: WebGL 임베드 — 탱크 수위·색상이 실시간 데이터로 변화
- **SVG 공정 흐름도**: 5단계 공정을 클릭하면 각 공정 상세 수질 데이터 확인

## 🏗️ 아키텍처

```
[FastAPI 백엔드]  ←──HTTP REST──→  [React 대시보드]  ──postMessage──→  [Unity WebGL 3D]
  센서 시뮬레이션                    3초 폴링                               탱크·파이프
  파이프라인 계산                    SVG 공정도                             수위·색상
  REST API                          HRT 슬라이더                           공정명 레이블
```

## 📸 주요 기능

### 1. 실시간 센서 카드
- 4개 센서(pH / 탁도 / 유량 / 수온) 현재값 + 정상/경고/위험 상태 표시
- 카드 클릭 → 해당 센서의 시계열 히스토리 차트 전환
- 헤더 우측에 전체 시스템 상태 배지 (펄스 애니메이션)

### 2. Unity 3D 공정 뷰
- 5개 탱크(1차침전 / 폭기조 / 2차침전 / 질산화 / 소독) 3D 시각화
- 수질 상태에 따라 탱크 물 색상 변화 (파랑=정상 / 노랑=경고 / 빨강=위험)
- 파이프 색상도 실시간 상태 반영
- 각 탱크 위 공정명 레이블 (NotoSansKR 폰트, 항상 카메라 방향)
- 탱크/사이드바 버튼 클릭 → 해당 공정으로 줌인, 세부 수질 데이터 패널 표시
- 전체보기 버튼으로 오버뷰 복귀

### 3. 하수 처리 공정 시뮬레이션
- **SVG 공정 흐름도**: 원수 → 5단계 → 처리수 흐름, 각 공정 상태 색상 표시
- **HRT 슬라이더**: 공정별 체류시간 조절 (25%~250%) → API 호출 → 연쇄 재계산
- **수질 비교 카드**: 원수 vs 처리수 BOD/TSS/COD/NH₃ 비교 + 제거율
- **공정 상세 패널**: 선택한 공정의 유입수/유출수/제거율 상세 표시

### 4. 히스토리 차트 + 경보 패널
- 최근 20개 이력 시계열 차트 (정상 범위 상·하한선 표시)
- 경보 발생 시 경고/위험 메시지 목록 실시간 갱신

## 🛠️ 기술 스택

| 영역 | 기술 |
|------|------|
| Backend | Python 3.9+, FastAPI, Uvicorn |
| Frontend | React 19, Vite, Recharts |
| 3D | Unity 6 (URP), WebGL, TextMeshPro |
| 통신 | REST API, postMessage 브릿지 |
| 배포 | Docker, Docker Compose, Nginx |

## 📁 프로젝트 구조

```
aquaview/
├── backend/
│   ├── app/
│   │   ├── main.py          # FastAPI 앱, CORS 설정
│   │   ├── models.py        # Pydantic 모델 (센서 + 파이프라인)
│   │   ├── simulator.py     # 인메모리 센서 시뮬레이션 (Gaussian drift)
│   │   ├── pipeline.py      # 5단계 연쇄 계산 엔진 (시그모이드 HRT 커브)
│   │   └── routers/
│   │       ├── sensors.py   # GET /api/sensors
│   │       ├── alerts.py    # GET /api/alerts
│   │       ├── history.py   # GET /api/history
│   │       └── pipeline.py  # GET /api/pipeline, POST /api/pipeline/params
│   ├── Dockerfile
│   └── requirements.txt
├── frontend/
│   ├── src/
│   │   ├── api/client.js
│   │   ├── hooks/usePolling.js
│   │   └── components/
│   │       ├── SensorCard.jsx
│   │       ├── HistoryChart.jsx
│   │       ├── AlertPanel.jsx
│   │       ├── Unity3DView.jsx          # postMessage 브릿지
│   │       ├── ProcessFlowDiagram.jsx   # SVG 공정 흐름도
│   │       ├── HRTControls.jsx          # HRT 슬라이더
│   │       └── WaterQualityComparison.jsx
│   ├── public/unity/                    # Unity WebGL 빌드
│   ├── Dockerfile
│   └── nginx.conf
├── unity/AquaView3D/
│   └── Assets/Scripts/
│       ├── SensorDataReceiver.cs  # postMessage 수신 싱글톤
│       ├── StageView.cs           # 탱크 수위·색상 제어
│       ├── StageLabelUI.cs        # 탱크 위 공정명 레이블 (World Space)
│       ├── DetailUI.cs            # 줌인 시 수질 데이터 패널
│       ├── SidebarUI.cs           # 공정 선택 사이드바
│       ├── CameraController.cs    # 오빗 카메라 (줌인/전체보기)
│       ├── PipeController.cs      # 파이프 색상 제어
│       └── PumpAnimator.cs        # 유량 기반 펌프 애니메이션
├── docker-compose.yml
└── CLAUDE.md
```

## 🚀 실행 방법

### 로컬 개발

```bash
# 1. 백엔드 실행 (port 8000)
cd backend
pip install -r requirements.txt
python3 -m uvicorn app.main:app --reload

# 2. 프론트엔드 실행 (port 5173)
cd frontend
npm install
npm run dev
```

브라우저에서 http://localhost:5173 접속

### Docker 배포

```bash
docker-compose up -d --build
```

## 📡 API 엔드포인트

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/sensors` | 4개 센서 현재값 + 상태 |
| GET | `/api/alerts` | 경고/위험 경보 목록 |
| GET | `/api/history?sensor={type}&limit={n}` | 센서 시계열 이력 |
| GET | `/api/pipeline` | 기본 HRT(100%)로 파이프라인 계산 |
| POST | `/api/pipeline/params` | HRT 비율 배열 → 5단계 연쇄 계산 |

## 🏭 파이프라인 시뮬레이션 원리

```
원수 (BOD 200, TSS 220 mg/L)
  ↓ [1차 침전]  HRT 2h    — TSS 65%, BOD 45% 중력 침전
  ↓ [폭기조]    HRT 6h    — BOD 88%, COD 78% 미생물 분해
  ↓ [2차 침전]  HRT 1.5h  — 활성슬러지 분리
  ↓ [질산화]    HRT 10h   — NH₃ 90% 제거 (균 washout에 민감)
  ↓ [소독]      HRT 0.5h  — 대장균 99.99% 염소 CT 제거
처리수 (BOD <10, TSS <10 mg/L 목표)
```

각 공정의 HRT → 수질 제거율 관계는 **시그모이드 함수**로 모델링:
- HRT가 짧아질수록 제거율 급감 (특히 질산화는 균 washout으로 매우 민감)
- EPA Secondary Treatment 기준값 기반 파라미터 설정

## 🎮 React ↔ Unity 통신 구조

```
React (3초/10초 폴링)
  ├─ SENSOR_UPDATE  → SensorDataReceiver → StageView (수위/색상)
  │                                      → PipeController (파이프 색상)
  │                                      → PumpAnimator (펌프 속도)
  └─ PIPELINE_UPDATE → SensorDataReceiver → StageView (5단계 상태)
                                          → DetailUI (수질 수치 패널)
                                          → SidebarUI (상태 점 색상)
```

Unity가 초기화되기까지 3~10초 소요되므로, iframe `onLoad` 후 3초/7초 재전송 + 10초 폴링으로 데이터 수신을 보장합니다.
