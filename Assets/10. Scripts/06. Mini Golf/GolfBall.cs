using System.Collections.Generic;
using UnityEngine;
namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Rigidbody))]
    public class GolfBall : MonoBehaviour
    {
        [Header("Settings")]
        public float stopSpeed = 0.3f;
        public float stopDelay = 0.2f;
        public float maxAngularSpeed = 150f;
        private Rigidbody rBody;
        private Vector3 lastPosition;
        private Vector3 shotVelocity;
        private bool shotRequested;
        private float slowTime;
        private float externalUntil;
        private readonly HashSet<Collider> flatSupports = new HashSet<Collider>();
        public bool IsStopped => !shotRequested && flatSupports.Count > 0
            && Time.time >= externalUntil && slowTime >= Mathf.Max(0.02f, stopDelay)
            && rBody.linearVelocity.sqrMagnitude < stopSpeed * stopSpeed;
        private void Awake()
        {
            rBody = GetComponent<Rigidbody>();
            rBody.maxAngularVelocity = maxAngularSpeed;
            lastPosition = transform.position;
        }
        private void FixedUpdate()
        {
            if (shotRequested)
            {
                shotRequested = false;
                slowTime = 0f;
                rBody.WakeUp();
                rBody.AddForce(shotVelocity, ForceMode.VelocityChange);
                return;
            }
            flatSupports.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            if (flatSupports.Count > 0 && Time.time >= externalUntil
                && rBody.linearVelocity.magnitude < Mathf.Max(0.01f, stopSpeed))
            {
                slowTime += Time.fixedDeltaTime;
                if (slowTime >= Mathf.Max(0.02f, stopDelay))
                {
                    rBody.linearVelocity = Vector3.zero;
                    rBody.angularVelocity = Vector3.zero;
                    rBody.Sleep();
                }
            }
            else slowTime = 0f;
        }
        // 가속 구역/점프 등 외력이 작동할 때 정지 처리를 잠깐 보류합니다.
        public void NotifyExternalMotion()
        {
            externalUntil = Time.time + 0.1f;
            slowTime = 0f;
        }
        private void OnCollisionEnter(Collision collision) { UpdateSupport(collision); }
        private void OnCollisionStay(Collision collision) { UpdateSupport(collision); }
        private void OnCollisionExit(Collision collision) { flatSupports.Remove(collision.collider); }
        private void UpdateSupport(Collision collision)
        {
            // 움직이는 벽/발판을 지면으로 간주하지 않습니다. 정적 바닥만 대상.
            bool flat = false;
            if (collision.collider.attachedRigidbody == null)
                for (int i = 0; i < collision.contactCount; i++)
                    if (collision.GetContact(i).normal.y >= 0.98f) { flat = true; break; }
            if (flat) flatSupports.Add(collision.collider);
            else flatSupports.Remove(collision.collider);
        }
        public void Shoot(Vector3 direction, float power)
        {
            slowTime = 0f;
            shotRequested = true;
            shotVelocity = direction * power;
            lastPosition = transform.position;
        }
        private void Teleport(Vector3 position)
        {
            shotRequested = false;
            slowTime = 0f;
            externalUntil = Time.time + 0.1f;
            flatSupports.Clear();
            rBody.linearVelocity = Vector3.zero;
            rBody.angularVelocity = Vector3.zero;
            rBody.position = position;
            transform.position = position;
            rBody.WakeUp();
        }
        public void ReturnToLasShotPosition() { Teleport(lastPosition); }
        public void PlaceAt(Vector3 position) { lastPosition = position; Teleport(position); }
    }
}
