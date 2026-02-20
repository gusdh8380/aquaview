using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전체뷰(Overview) ↔ 시설 줌인(Detail) 카메라 전환을 담당합니다.
///
/// [전체뷰]
///   - 마우스 드래그로 씬 회전 가능
///   - 스크롤로 줌인/아웃
///
/// [줌인뷰]
///   - 선택한 StageView 오브젝트를 향해 부드럽게 카메라 이동
///   - 마우스 드래그 비활성화
///   - DetailUI 패널 표시
///   - 뒤로가기 버튼 → 전체뷰 복귀
/// </summary>
public class CameraController : MonoBehaviour
{
    // ── 싱글톤 ─────────────────────────────────────────────
    public static CameraController Instance { get; private set; }

    // ── Inspector 설정 ────────────────────────────────────
    [Header("전체뷰 카메라 위치/각도")]
    public Vector3 overviewPosition  = new Vector3(0f, 15f, -22f);
    public Vector3 overviewLookTarget = new Vector3(0f, 0f, 0f);

    [Header("줌인 카메라 오프셋 (시설 기준)")]
    [Tooltip("시설 위치로부터의 카메라 상대 위치")]
    public Vector3 zoomOffset = new Vector3(0f, 6f, -9f);

    [Header("전환 속도")]
    [Tooltip("줌인/줌아웃 이동 시간 (초)")]
    public float transitionDuration = 0.9f;

    [Header("전체뷰 오빗 설정")]
    public float rotateSpeed     = 160f;
    public float minVerticalAngle = 8f;
    public float maxVerticalAngle = 75f;
    public float zoomSpeed       = 5f;
    public float minDistance     = 6f;
    public float maxDistance     = 35f;

    // ── 내부 상태 ─────────────────────────────────────────
    public bool IsZoomedIn { get; private set; } = false;

    private bool      _isTransitioning = false;
    private StageView _focusedStage    = null;
    private DetailUI  _detailUI;

    // 오빗 카메라 내부 변수
    private float _yaw;
    private float _pitch;
    private float _distance;
    private float _targetYaw;
    private float _targetPitch;
    private float _targetDistance;
    private Vector3 _orbitPivot;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _detailUI = FindFirstObjectByType<DetailUI>();

        // 전체뷰 초기 오빗 상태 계산
        transform.position = overviewPosition;
        transform.LookAt(overviewLookTarget);
        _orbitPivot = overviewLookTarget;

        Vector3 offset = overviewPosition - overviewLookTarget;
        _distance = _targetDistance = offset.magnitude;
        _pitch    = _targetPitch    = Mathf.Asin(offset.normalized.y) * Mathf.Rad2Deg;
        _yaw      = _targetYaw      = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (!IsZoomedIn && !_isTransitioning)
            UpdateOrbit();
    }

    // ── 오빗 카메라 (전체뷰) ──────────────────────────────

    void UpdateOrbit()
    {
        // 마우스 드래그 회전 (New Input System)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            float dx =  delta.x * 0.1f;
            float dy =  delta.y * 0.1f;
            _targetYaw   += dx * rotateSpeed * Time.deltaTime;
            _targetPitch -= dy * rotateSpeed * Time.deltaTime;
            _targetPitch  = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
        }

        // 스크롤 줌 (New Input System)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y * 0.01f;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _targetDistance -= scroll * zoomSpeed;
                _targetDistance  = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
            }
        }

        // 부드럽게 적용
        _yaw      = Mathf.LerpAngle(_yaw,      _targetYaw,      Time.deltaTime * 8f);
        _pitch    = Mathf.LerpAngle(_pitch,    _targetPitch,    Time.deltaTime * 8f);
        _distance = Mathf.Lerp    (_distance, _targetDistance, Time.deltaTime * 8f);

        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _orbitPivot + rot * Vector3.back * _distance;
        transform.LookAt(_orbitPivot);
    }

    // ── 줌인/줌아웃 API ───────────────────────────────────

    /// <summary>
    /// SidebarUI 버튼에서 호출. 지정한 StageView로 줌인합니다.
    /// </summary>
    public void ZoomIn(StageView stage)
    {
        if (_isTransitioning || (IsZoomedIn && _focusedStage == stage)) return;

        _focusedStage = stage;
        Vector3 targetPos    = stage.GetFocusPosition() + zoomOffset;
        Vector3 targetLookAt = stage.GetFocusPosition() + Vector3.up * 1.5f;

        StartCoroutine(MoveCamera(targetPos, targetLookAt, onComplete: () =>
        {
            IsZoomedIn = true;
            _detailUI?.Show(stage);
        }));
    }

    /// <summary>
    /// 뒤로가기 버튼에서 호출. 전체뷰로 복귀합니다.
    /// </summary>
    public void ZoomOut()
    {
        if (_isTransitioning || !IsZoomedIn) return;

        _detailUI?.Hide();

        StartCoroutine(MoveCamera(overviewPosition, overviewLookTarget, onComplete: () =>
        {
            IsZoomedIn    = false;
            _focusedStage = null;

            // 오빗 상태 재동기화
            Vector3 offset = overviewPosition - overviewLookTarget;
            _distance = _targetDistance = offset.magnitude;
            _pitch    = _targetPitch    = Mathf.Asin(offset.normalized.y) * Mathf.Rad2Deg;
            _yaw      = _targetYaw      = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }));
    }

    // ── 카메라 이동 Coroutine ─────────────────────────────

    IEnumerator MoveCamera(Vector3 toPos, Vector3 toLookAt, System.Action onComplete = null)
    {
        _isTransitioning = true;

        Vector3 fromPos    = transform.position;
        Vector3 fromLookAt = transform.position + transform.forward * 10f;
        float   elapsed    = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            Vector3 pos    = Vector3.Lerp(fromPos, toPos, t);
            Vector3 lookAt = Vector3.Lerp(fromLookAt, toLookAt, t);

            transform.position = pos;
            transform.LookAt(lookAt);

            yield return null;
        }

        transform.position = toPos;
        transform.LookAt(toLookAt);

        _isTransitioning = false;
        onComplete?.Invoke();
    }
}
