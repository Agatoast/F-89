using SaveAntarctica.BunkerDefense.Combat;
using SaveAntarctica.BunkerDefense.UI;
using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Core
{
    public sealed class MatchController : MonoBehaviour
    {
        private enum Phase
        {
            Briefing,
            Fighting,
            Ended
        }

        public static MatchController Instance { get; private set; }

        public WaveDirector Waves { get; private set; }
        public FlyingSaucerSpawner Saucers { get; private set; }
        public MachineGun Gun { get; private set; }
        public HangarTarget Hangar { get; private set; }

        public bool IsFighting => _phase == Phase.Fighting;

        private Phase _phase = Phase.Briefing;

        public static MatchController Ensure()
        {
            var existing = FindAnyObjectByType<MatchController>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(nameof(MatchController));
            return go.AddComponent<MatchController>();
        }

        private void Awake()
        {
            Instance = this;
            EnemySoldier.ResetAliveCount();
            EnemyVehicle.ResetAliveCount();
            FlyingSaucer.ResetAliveCount();
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (FlightMissionData.Instance != null
                && string.IsNullOrWhiteSpace(FlightMissionData.Instance.ActiveDefenseMissionId))
            {
                FlightMissionData.Instance.BindPersistenceScope("Standalone");
                FlightMissionData.Instance.LoadPersistedConsumedMissions();
            }

            var mapNumber = FlightMissionData.Instance != null && FlightMissionData.Instance.StandaloneMapNumber > 0
                ? FlightMissionData.Instance.StandaloneMapNumber
                : 1;
            var t = Mathf.InverseLerp(1f, DefenseSiteCatalog.MissionCount, mapNumber);
            var hangarHp = Mathf.RoundToInt(Mathf.Lerp(120f, 90f, t));

            BattlefieldBuilder.Build(hangarHp, mapNumber);
            Hangar = HangarTarget.Instance;
            var bunker = FindAnyObjectByType<SandbagBunker>();
            Gun = MachineGun.Create(bunker);
            Gun.SetCanFire(false);
            Waves = WaveDirector.Create(mapNumber);
            Saucers = FlyingSaucerSpawner.Create(mapNumber, Waves);
            BoardClearGrenade.ResetForMatch(mapNumber);
            CombatHud.EnsureInScene();
            AimCrosshair.EnsureInScene();
            BriefingOverlay.Show(DefenseSiteCatalog.GetBriefingLabel(mapNumber));
        }

        public void BeginFighting()
        {
            StartMatch();
        }

        private void StartMatch()
        {
            if (_phase != Phase.Briefing)
            {
                return;
            }

            if (Gun == null || Waves == null)
            {
                Debug.LogError("F-89 Bunker Defense: cannot start — gun or waves failed to initialize.");
                return;
            }

            _phase = Phase.Fighting;
            FlightMissionData.Instance?.MarkMissionStartDefenseConsumed();
            Gun.SetCanFire(true);
            Waves.Begin();
            Saucers?.Begin();
            Debug.Log("F-89 Bunker Defense: OPEN FIRE — waves started.");
        }

        private void Update()
        {
            if (_phase != Phase.Fighting)
            {
                return;
            }

            if (Hangar != null && Hangar.IsDestroyed)
            {
                EndMatch(false);
                return;
            }

            if (Waves != null && Waves.WavesComplete && Waves.FieldIsClear())
            {
                EndMatch(true);
            }
        }

        private void EndMatch(bool victory)
        {
            _phase = Phase.Ended;
            Gun.SetCanFire(false);
            FlightMissionData.Instance?.WriteDefenseOutcome(victory);
            if (victory)
            {
                OutcomeOverlay.ShowVictory();
            }
            else
            {
                OutcomeOverlay.ShowDefeat();
            }
        }
    }
}
