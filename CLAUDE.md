# CLAUDE.md

## Project Overview
AquaView — 수처리 공정 모니터링 시스템 (포트폴리오 MVP)

## Architecture
```
[FastAPI 서버] → HTTP → [React 대시보드] → HTTP → [Unity WebGL 3D 뷰]
                                ↕
                    [Pipeline Simulation Engine]
                    (HRT 조절 → 연쇄 수질 계산)
```
- **Backend**: FastAPI (Python) — 센서 시뮬레이션 + REST API + 파이프라인 계산 엔진
- **Frontend**: React (Vite) — 실시간 대시보드 + SVG 공정도 + HRT 슬라이더
- **3D View**: Unity WebGL — React 내 iframe 임베드 (예정)

## Project Structure
```
aquaview/
├── backend/
│   ├── app/
│   │   ├── main.py          # FastAPI 앱, CORS 설정
│   │   ├── models.py        # Pydantic 모델 (센서 + 파이프라인)
│   │   ├── simulator.py     # 인메모리 센서 시뮬레이션
│   │   ├── pipeline.py      # 하수 처리 파이프라인 연쇄 계산 엔진
│   │   └── routers/
│   │       ├── sensors.py   # GET /api/sensors
│   │       ├── alerts.py    # GET /api/alerts
│   │       ├── history.py   # GET /api/history
│   │       └── pipeline.py  # GET /api/pipeline, POST /api/pipeline/params
│   └── requirements.txt
└── frontend/
    └── src/
        ├── api/client.js              # API 호출 함수
        ├── hooks/usePolling.js        # 3초 폴링 훅
        ├── components/
        │   ├── SensorCard.jsx         # 센서 카드
        │   ├── HistoryChart.jsx       # 시계열 차트 (recharts)
        │   ├── AlertPanel.jsx         # 경보 패널
        │   ├── Unity3DView.jsx        # Unity iframe 브릿지
        │   ├── ProcessFlowDiagram.jsx # SVG 공정 흐름도 (클릭 가능)
        │   ├── HRTControls.jsx        # 공정별 HRT 슬라이더 패널
        │   └── WaterQualityComparison.jsx  # 원수 vs 처리수 비교 카드
        ├── App.jsx
        └── App.css
```

## API Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/sensors` | GET | pH, 탁도, 유량, 수온 현재값 + 상태 |
| `GET /api/alerts` | GET | 경고/위험 상태 경보 목록 |
| `GET /api/history?sensor={type}&limit={n}` | GET | 센서별 최근 이력 |
| `GET /api/pipeline` | GET | 기본 HRT(100%)로 파이프라인 계산 결과 반환 |
| `POST /api/pipeline/params` | POST | HRT 비율 배열로 연쇄 계산 실행 |

## Pipeline Simulation
실제 도시 하수 처리 공정 데이터 기반 (EPA, WEF 기준)

### 처리 공정 순서
```
원수 (BOD 200, TSS 220 mg/L)
  ↓ [1차 침전] HRT 2h  — TSS/BOD 중력 침전
  ↓ [폭기조]   HRT 6h  — 미생물 유기물 분해 (BOD/COD 주요 제거)
  ↓ [2차 침전] HRT 1.5h — 활성슬러지 분리
  ↓ [질산화]   HRT 10h  — NH3→NO3 변환 (암모니아 제거)
  ↓ [소독]     HRT 0.5h — 염소 CT값 기반 병원균 제거
처리수 (BOD <10, TSS <10 mg/L 목표)
```

### HRT 조절 효과 (실제 수치 기반)
| 공정 | 설계 HRT | 주요 제거 항목 | HRT ↓ 시 영향 |
|------|---------|-------------|-------------|
| 1차 침전 | 2h | TSS 65%, BOD 45% | 부유물질 제거 저하 |
| 폭기조 | 6h | BOD 88%, COD 78% | BOD/COD 급등, 경보 |
| 2차 침전 | 1.5h | TSS 추가 제거 | 슬러지 월류 |
| 질산화 | 10h | NH3 90% | 균 washout → NH3 급등 |
| 소독 | 0.5h | 대장균 99.99% | 병원균 미처리 |

### Pipeline 측정 지표
BOD, TSS, COD, NH₃-N, 탁도(NTU), pH, DO(mg/L), 대장균(CFU/100mL)

## Sensor Thresholds (Real-time monitoring)
| Sensor | Normal | Warning | Danger |
|--------|--------|---------|--------|
| pH | 6.5~8.5 | 6.0~6.5 / 8.5~9.0 | <6.0 / >9.0 |
| Turbidity (NTU) | 0~5 | 5~10 | >10 |
| Flow (m³/h) | 50~150 | 30~50 / 150~180 | <30 / >180 |
| Temp (°C) | 15~25 | 10~15 / 25~30 | <10 / >30 |

## How to Run
```bash
# Backend (port 8000)
cd backend && python3 -m uvicorn app.main:app --reload

# Frontend (port 5173)
cd frontend && npm run dev
```

## Tech Stack
- Python 3.9+, FastAPI, Uvicorn
- React 19, Vite, Recharts
- Unity WebGL (예정)

## Notes
- DB 없이 인메모리 시뮬레이션 (랜덤 drift 기반)
- CORS: 개발 시 all origins 허용
- 프론트엔드 3초 폴링으로 실시간 갱신
- 파이프라인 계산: 시그모이드 함수 기반 HRT → 수질 제거율 매핑 (실제 공학 데이터 기반)
