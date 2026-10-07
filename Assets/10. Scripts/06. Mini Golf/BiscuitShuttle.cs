using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Rigidbody))]
    public class BiscuitShuttle : MonoBehaviour
    {
        [Tooltip("시작점에서 끝점까지의 월드 좌표 이동량. X=좌우, Y=상하, Z=앞뒤")]
        public Vector3 offset = new Vector3(2f, 0f, 0f);
        [Min(0.1f)] public float roundTripSeconds = 3f;
        private Rigidbody body;
        private Vector3 startPosition;
        private float elapsed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            startPosition = body.position;
        }

        private void FixedUpdate()
        {
            elapsed += Time.fixedDeltaTime;
            float phase = elapsed / Mathf.Max(0.1f, roundTripSeconds) * Mathf.PI * 2f;
            float t = (1f - Mathf.Cos(phase)) * 0.5f;
            body.MovePosition(startPosition + offset * t);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawLine(start, start + offset);
            Gizmos.DrawWireSphere(start + offset, 0.15f);
        }
    }
}
