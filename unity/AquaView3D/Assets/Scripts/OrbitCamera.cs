using UnityEngine;

/// <summary>
/// 마우스 드래그로 씬을 회전하고 스크롤로 줌인/아웃하는 오빗 카메라.
/// 타겟 오브젝트를 중심으로 공전합니다.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [Header("타겟")]
    [Tooltip("카메라가 공전할 중심 오브젝트")]
    public Transform target;

    [Header("회전 설정")]
    public float rotateSpeed = 180f;      // deg/sec per pixel
    public float minVerticalAngle = 5f;
    public float maxVerticalAngle = 80f;

    [Header("줌 설정")]
    public float zoomSpeed = 5f;
    public float minDistance = 4f;
    public float maxDistance = 30f;

    [Header("초기값")]
    public float initialDistance = 18f;
    public float initialYaw = 2.6f;
    public float initialPitch = 19.2f;

    private float _yaw;
    private float _pitch;
    private float _distance;

    private float _targetYaw;
    private float _targetPitch;
    private float _targetDistance;

    void Start()
    {
        _yaw   = _targetYaw   = initialYaw;
        _pitch = _targetPitch = initialPitch;
        _distance = _targetDistance = initialDistance;

        // target이 지정 안 됐으면 씬 중앙(0,1.5,0)을 기본 타겟으로
        if (target == null)
        {
            var pivot = new GameObject("CameraPivot");
            pivot.transform.position = new Vector3(0f, 1.5f, 0f);
            target = pivot.transform;
        }

        ApplyTransform();
    }

    void Update()
    {
        HandleMouseDrag();
        HandleScroll();
        SmoothApply();
    }

    void HandleMouseDrag()
    {
        if (!Input.GetMouseButton(0)) return;

        float dx = Input.GetAxis("Mouse X");
        float dy = Input.GetAxis("Mouse Y");

        _targetYaw   += dx * rotateSpeed * Time.deltaTime;
        _targetPitch -= dy * rotateSpeed * Time.deltaTime;
        _targetPitch  = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
    }

    void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        _targetDistance -= scroll * zoomSpeed;
        _targetDistance  = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
    }

    void SmoothApply()
    {
        _yaw      = Mathf.LerpAngle(_yaw,      _targetYaw,      Time.deltaTime * 8f);
        _pitch    = Mathf.LerpAngle(_pitch,    _targetPitch,    Time.deltaTime * 8f);
        _distance = Mathf.Lerp    (_distance, _targetDistance, Time.deltaTime * 8f);
        ApplyTransform();
    }

    void ApplyTransform()
    {
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 direction   = rotation * Vector3.back;
        transform.position  = target.position + direction * _distance;
        transform.LookAt(target.position);
    }

    /// <summary>카메라를 초기 위치로 리셋합니다.</summary>
    public void ResetCamera()
    {
        _targetYaw      = initialYaw;
        _targetPitch    = initialPitch;
        _targetDistance = initialDistance;
    }
}
