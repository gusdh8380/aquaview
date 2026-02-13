# CLAUDE.md

## Project Overview
AquaView — 수처리 공정 모니터링 시스템 (포트폴리오 MVP)

## Architecture
```
[FastAPI 서버] → HTTP → [React 대시보드] → HTTP → [Unity WebGL 3D 뷰]
```
- **Backend**: FastAPI (Python) — 센서 시뮬레이션 + REST API
- **Frontend**: React (Vite) — 실시간 대시보드
- **3D View**: Unity WebGL — React 내 iframe 임베드 (예정)

## Project Structure
```
aquaview/
├── backend/
│   ├── app/
│   │   ├── main.py          # FastAPI 앱, CORS 설정
│   │   ├── models.py        # Pydantic 모델
│   │   ├── simulator.py     # 인메모리 센서 시뮬레이션
│   │   └── routers/
│   │       ├── sensors.py   # GET /api/sensors
│   │       ├── alerts.py    # GET /api/alerts
│   │       └── history.py   # GET /api/history
│   └── requirements.txt
└── frontend/
    └── src/
        ├── api/client.js         # API 호출 함수
        ├── hooks/usePolling.js   # 3초 폴링 훅
        ├── components/
        │   ├── SensorCard.jsx    # 센서 카드
        │   ├── HistoryChart.jsx  # 시계열 차트 (recharts)
        │   └── AlertPanel.jsx    # 경보 패널
        ├── App.jsx
        └── App.css
```

## API Endpoints
| Endpoint | Description |
|----------|-------------|
| `GET /api/sensors` | pH, 탁도, 유량, 수온 현재값 + 상태 |
| `GET /api/alerts` | 경고/위험 상태 경보 목록 |
| `GET /api/history?sensor={type}&limit={n}` | 센서별 최근 이력 |

## Sensor Thresholds
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
