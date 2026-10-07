using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Study3D.MiniGolf
{
    public class GolfRoundFlow : MonoBehaviour
    {
        public enum MapMode { OneScene, SeparateScenes }
        [Serializable] public class HoleSetup
        {
            public GameObject mapRoot;
            public Transform spawnPoint;
        }
        public static GolfRoundFlow Instance { get; private set; }
        public MapMode mapMode = MapMode.OneScene;
        public GolfBall ball;
        public GolfPlayer player;
        public HoleSetup[] holes;
        [Tooltip("SeparateScenes 모드: 각 홀 씬의 경로 또는 고유한 이름을 순서대로 입력")]
        public string[] sceneNames;
        public Transform sceneSpawnPoint;
        [Min(0.1f)] public float clearSeconds = 1.5f;
        [Tooltip("카메라 위치 보정 등 기존 프로젝트 기능을 필요할 때 연결")]
        public UnityEvent onHoleStarted = new UnityEvent();
        private static int[] sceneScores;
        private int[] scores;
        private int index;
        [Header("Fall / Start")]
        [Min(0.5f)] public float fallDistanceBelowSpawn = 8f;
        public bool addPenaltyOnFall = false;
        public UnityEvent onRespawn = new UnityEvent();
        private Vector3 currentSpawn;
        private float respawnAllowedAt;
        private bool waitingForStart;
        [Header("Hole Testing")]
        public bool testMode = false;
        [Range(1, 3)] public int startHole = 1;
        private bool loadingScene;
        private bool busy;
        private bool valid;
        private Rigidbody body;
        private bool originalKinematic;
        private GolfRoundUI ui;
        private CursorLockMode oldLock;
        private bool oldVisible;
        public bool CanPlay => valid && !busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; sceneScores = null; }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ui = GetComponent<GolfRoundUI>();
            if (ui == null) ui = gameObject.AddComponent<GolfRoundUI>();
            oldLock = Cursor.lockState; oldVisible = Cursor.visible;
        }
        private void Start()
        {
            if (ball == null || player == null || (body = ball.GetComponent<Rigidbody>()) == null)
            { Fail("Ball / Player, Rigidbody를 연결하세요."); return; }
            originalKinematic = body.isKinematic;
            if (mapMode == MapMode.OneScene)
            {
                if (holes == null || holes.Length == 0) { Fail("Holes를 등록하세요."); return; }
                foreach (HoleSetup h in holes)
                    if (h == null || h.mapRoot == null || h.spawnPoint == null
                        || h.mapRoot == gameObject || transform.IsChildOf(h.mapRoot.transform)
                        || ball.transform.IsChildOf(h.mapRoot.transform)
                        || player.transform.IsChildOf(h.mapRoot.transform))
                    { Fail("각 홀의 Map Root/Spawn Point를 연결하고 공·플레이어·매니저는 맵 밖에 두세요."); return; }
                scores = new int[holes.Length];
                index = testMode ? Mathf.Clamp(startHole - 1, 0, holes.Length - 1) : 0;
            }
            else
            {
                if (sceneNames == null || sceneNames.Length == 0)
                { Fail("Scene Names를 등록하세요."); return; }
                Scene active = SceneManager.GetActiveScene();
                index = Array.FindIndex(sceneNames, n => n == active.name || n == active.path);
                if (index < 0) { Fail("현재 씬을 Scene Names에서 찾을 수 없습니다."); return; }
                foreach (string n in sceneNames)
                    if (string.IsNullOrWhiteSpace(n) || !Application.CanStreamedLevelBeLoaded(n))
                    { Fail("Scene Names의 씬들을 Build Profiles의 Scene List에 등록하세요."); return; }
                if (index == 0 || sceneScores == null || sceneScores.Length != sceneNames.Length)
                    sceneScores = new int[sceneNames.Length];
                scores = sceneScores;
            }
            valid = true;
            BeginHole();
        }
        private void Fail(string message)
        {
            Debug.LogError("GolfRoundFlow: " + message, this);
            if (player != null) player.enabled = false;
            ui.ShowResult(message, null);
        }
        private void BeginHole()
        {
            busy = true;
            player.enabled = false;
            if (player.aimIndicator != null) player.aimIndicator.Hide();
            ball.enabled = true;
            body.isKinematic = originalKinematic;
            if (mapMode == MapMode.OneScene)
            {
                for (int i = 0; i < holes.Length; i++) holes[i].mapRoot.SetActive(i == index);
                ball.PlaceAt(holes[index].spawnPoint.position);
            }
            else if (sceneSpawnPoint != null) ball.PlaceAt(sceneSpawnPoint.position);
            currentSpawn = mapMode == MapMode.OneScene
                ? holes[index].spawnPoint.position
                : (sceneSpawnPoint != null ? sceneSpawnPoint.position : ball.transform.position);
            ui.HideOverlay();
            ui.SetHUD(index + 1, scores.Length, scores[index]);
            onHoleStarted.Invoke();
            if (index == 0 && !testMode)
            {
                waitingForStart = true;
                ball.enabled = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                UnlockCursor();
                ui.ShowStart(StartPlaying);
            }
            else
            {
                waitingForStart = false;
                Cursor.lockState = oldLock; Cursor.visible = oldVisible;
                busy = false;
                respawnAllowedAt = Time.time + 0.5f;
                player.enabled = true;
            }
        }
        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        private void LateUpdate()
        {
            if (busy) UnlockCursor();
        }
        private void Update()
        {
            if (valid && testMode && !loadingScene)
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.f1Key.wasPressedThisFrame) { GoToHole(1); return; }
                    if (keyboard.f2Key.wasPressedThisFrame) { GoToHole(2); return; }
                    if (keyboard.f3Key.wasPressedThisFrame) { GoToHole(3); return; }
                }
#else
                if (Input.GetKeyDown(KeyCode.F1)) { GoToHole(1); return; }
                if (Input.GetKeyDown(KeyCode.F2)) { GoToHole(2); return; }
                if (Input.GetKeyDown(KeyCode.F3)) { GoToHole(3); return; }
#endif
            }
            if (CanPlay && body.position.y < currentSpawn.y - fallDistanceBelowSpawn)
                RespawnBall(ball);
        }
        // 1부터 시작하는 홀 번호. 테스트 모드에서만 동작합니다.
        public void GoToHole(int holeNumber)
        {
            if (!valid || !testMode || loadingScene) return;
            int target = holeNumber - 1;
            if (target < 0 || target >= scores.Length)
            { Debug.LogWarning("등록된 홀 번호를 선택하세요.", this); return; }
            // 클리어 대기 중 점프하면 예약된 다음 홀 이동도 취소.
            StopAllCoroutines();
            busy = true;
            waitingForStart = false;
            player.enabled = false;
            if (player.aimIndicator != null) player.aimIndicator.Hide();
            Array.Clear(scores, 0, scores.Length);
            ui.HideOverlay();
            if (mapMode == MapMode.OneScene || target == index)
            {
                index = target;
                BeginHole();
            }
            else
            {
                ball.enabled = false;
                if (!body.isKinematic)
                { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = true;
                loadingScene = true;
                SceneManager.LoadSceneAsync(sceneNames[target]);
            }
        }
        [ContextMenu("Test/Go To Hole 1")] private void TestHole1() { GoToHole(1); }
        [ContextMenu("Test/Go To Hole 2")] private void TestHole2() { GoToHole(2); }
        [ContextMenu("Test/Go To Hole 3")] private void TestHole3() { GoToHole(3); }
        public void StartPlaying()
        {
            if (!valid || !waitingForStart) return;
            waitingForStart = false;
            ui.HideOverlay();
            body.isKinematic = originalKinematic;
            ball.enabled = true;
            ball.PlaceAt(currentSpawn);
            Cursor.lockState = oldLock;
            Cursor.visible = oldVisible;
            busy = false;
            respawnAllowedAt = Time.time + 0.5f;
            player.enabled = true;
        }
        public void RespawnBall(GolfBall fallingBall)
        {
            if (!CanPlay || fallingBall != ball || Time.time < respawnAllowedAt) return;
            respawnAllowedAt = Time.time + 0.5f;
            player.enabled = false;
            if (player.aimIndicator != null) player.aimIndicator.Hide();
            ball.PlaceAt(currentSpawn);
            if (addPenaltyOnFall) scores[index]++;
            ui.SetHUD(index + 1, scores.Length, scores[index]);
            onRespawn.Invoke();
            player.enabled = true;
        }
        // 실제 샷이 발생한 곳에서 한 번 호출합니다.
        public void RecordShot()
        {
            if (!CanPlay) return;
            scores[index]++;
            ui.SetHUD(index + 1, scores.Length, scores[index]);
        }
        public void CompleteHole(GolfBall enteringBall)
        {
            if (!CanPlay || enteringBall != ball) return;
            busy = true; // 여러 Collider가 들어와도 중복 클리어 금지
            player.enabled = false;
            if (player.aimIndicator != null) player.aimIndicator.Hide();
            ball.enabled = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            StartCoroutine(ClearRoutine());
        }
        private IEnumerator ClearRoutine()
        {
            ui.ShowClear(index + 1, scores[index]);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, clearSeconds));
            if (index + 1 < scores.Length)
            {
                if (mapMode == MapMode.OneScene) { index++; BeginHole(); }
                else { loadingScene = true; yield return SceneManager.LoadSceneAsync(sceneNames[index + 1]); }
            }
            else
            {
                var result = new StringBuilder();
                int total = 0;
                for (int i = 0; i < scores.Length; i++)
                { result.AppendLine($"HOLE {i + 1}   {scores[i]} shots"); total += scores[i]; }
                result.Append($"\nTOTAL   {total} shots");
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                ui.ShowResult(result.ToString(), RestartFromFirstHole);
            }
        }
        public void RestartFromFirstHole()
        {
            ui.DisableRestart();
            Cursor.lockState = oldLock; Cursor.visible = oldVisible;
            if (mapMode == MapMode.OneScene)
            { Array.Clear(scores, 0, scores.Length); index = 0; BeginHole(); }
            else { loadingScene = true; sceneScores = null; SceneManager.LoadSceneAsync(sceneNames[0]); }
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
