using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveAntarctica.BunkerDefense.Core
{
    /// <summary>
    /// DontDestroyOnLoad bridge to Save Antarctica / F-89.
    /// Hangar survives → +1 plane hit; hangar destroyed → -1. Next flight mission only.
    /// </summary>
    public sealed class FlightMissionData : MonoBehaviour
    {
        public static FlightMissionData Instance { get; private set; }

        public bool HasDefenseOutcome { get; private set; }
        public bool DefenseSucceeded { get; private set; }
        public bool HasBaseDefenseOutcome => HasDefenseOutcome;
        public bool BaseDefenseSucceeded => DefenseSucceeded;
        public int NextFlightHitModifier { get; private set; }
        public int PlaneHitPoints { get; private set; } = GameConstants.DefaultPlaneHitPoints;
        public string ReturnSceneName { get; private set; } = GameScenes.BomberTakeoffScene;
        public string LastLandedBaseId { get; private set; } = string.Empty;
        public bool HasPendingHostResume { get; private set; }
        public string PendingHostUiRequest { get; private set; } = string.Empty;
        public string ActiveDefenseMissionId { get; private set; } = string.Empty;
        public string SiteCode { get; private set; } = string.Empty;
        public int StandaloneMapNumber { get; private set; }
        public DefenseDifficulty SelectedDifficulty { get; private set; } = DefenseDifficulty.Normal;

        private readonly HashSet<string> _consumedDefenseMissionIds = new(StringComparer.OrdinalIgnoreCase);
        private string _persistenceScopeKey = string.Empty;

        public static Action OnConsumedMissionsChanged { get; set; }

        public void BindPersistenceScope(string scopeKey)
        {
            _persistenceScopeKey = string.IsNullOrWhiteSpace(scopeKey) ? string.Empty : scopeKey.Trim();
        }

        public void ImportConsumedMissionIds(IEnumerable<string> missionIds)
        {
            _consumedDefenseMissionIds.Clear();
            if (missionIds == null)
            {
                return;
            }

            foreach (var missionId in missionIds)
            {
                if (!string.IsNullOrWhiteSpace(missionId))
                {
                    _consumedDefenseMissionIds.Add(missionId.Trim());
                }
            }
        }

        public string[] ExportConsumedMissionIds()
        {
            if (_consumedDefenseMissionIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            var ids = new string[_consumedDefenseMissionIds.Count];
            _consumedDefenseMissionIds.CopyTo(ids);
            return ids;
        }

        private string GetConsumedPrefsKey() => $"BunkerDefense.Consumed.{_persistenceScopeKey}";

        public void LoadPersistedConsumedMissions()
        {
            if (string.IsNullOrEmpty(_persistenceScopeKey))
            {
                return;
            }

            _consumedDefenseMissionIds.Clear();
            var raw = PlayerPrefs.GetString(GetConsumedPrefsKey(), string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var parts = raw.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    _consumedDefenseMissionIds.Add(parts[i].Trim());
                }
            }
        }

        private void SavePersistedConsumedMissions()
        {
            if (string.IsNullOrEmpty(_persistenceScopeKey))
            {
                return;
            }

            if (_consumedDefenseMissionIds.Count == 0)
            {
                PlayerPrefs.DeleteKey(GetConsumedPrefsKey());
                return;
            }

            var ids = ExportConsumedMissionIds();
            PlayerPrefs.SetString(GetConsumedPrefsKey(), string.Join("|", ids));
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var go = new GameObject(nameof(FlightMissionData));
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<FlightMissionData>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(DispatchHostResumeNextFrame());
        }

        private IEnumerator DispatchHostResumeNextFrame()
        {
            yield return null;

            if (!TryConsumeHostResume(out var baseId, out var uiRequest))
            {
                yield break;
            }

            var handler = BunkerDefenseHostResumeRegistry.Handler;
            if (handler != null)
            {
                handler.ResumeToLastLanding(baseId, uiRequest);
                yield break;
            }

            Debug.LogWarning(
                $"Bunker defense resume: base='{baseId}', ui='{uiRequest}', scene='{SceneManager.GetActiveScene().name}'. " +
                "Register BunkerDefenseHostResumeRegistry.Handler from F-89 to restore landing and open mission character page.");
        }

        public void SetPlaneHitPoints(int hits)
        {
            PlaneHitPoints = Mathf.Max(GameConstants.MinPlaneHitPoints, hits);
        }

        public void SetReturnSceneName(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                ReturnSceneName = sceneName;
            }
        }

        public void SetSiteCode(string siteCode)
        {
            SiteCode = string.IsNullOrWhiteSpace(siteCode) ? string.Empty : siteCode.Trim();
        }

        public void SetDifficulty(DefenseDifficulty difficulty)
        {
            SelectedDifficulty = difficulty;
        }

        public void SetStandaloneMap(int mapNumber)
        {
            mapNumber = Mathf.Clamp(mapNumber, 1, DefenseSiteCatalog.MissionCount);
            StandaloneMapNumber = mapNumber;
            SetSiteCode(DefenseSiteCatalog.GetSiteCode(mapNumber));
        }

        public bool TryAuthorizeMissionStartDefense(
            string missionId,
            string returnSceneName,
            string lastLandedBaseId,
            string siteCode = null)
        {
            if (string.IsNullOrWhiteSpace(missionId) || !BunkerDefenseMissionTriggerCatalog.IsDefenseMission(missionId))
            {
                return false;
            }

            var normalizedMissionId = missionId.Trim();
            if (_consumedDefenseMissionIds.Contains(normalizedMissionId))
            {
                return false;
            }

            ConfigureHostLaunchContext(returnSceneName, lastLandedBaseId, siteCode);
            ActiveDefenseMissionId = normalizedMissionId;
            var mapNumber = DefenseSiteCatalog.GetMapNumber(normalizedMissionId);
            if (mapNumber <= 0 && !string.IsNullOrWhiteSpace(siteCode))
            {
                mapNumber = DefenseSiteCatalog.GetMapNumber(siteCode);
            }

            if (mapNumber > 0)
            {
                SetStandaloneMap(mapNumber);
            }

            HasDefenseOutcome = false;
            DefenseSucceeded = false;
            NextFlightHitModifier = 0;
            return true;
        }

        public void MarkMissionStartDefenseConsumed()
        {
            if (!string.IsNullOrWhiteSpace(ActiveDefenseMissionId))
            {
                _consumedDefenseMissionIds.Add(ActiveDefenseMissionId);
                SavePersistedConsumedMissions();
                OnConsumedMissionsChanged?.Invoke();
            }
        }

        public void ClearConsumedMissions()
        {
            _consumedDefenseMissionIds.Clear();
            SavePersistedConsumedMissions();
            OnConsumedMissionsChanged?.Invoke();
        }

        public void ConfigureHostLaunchContext(string returnSceneName, string lastLandedBaseId, string siteCode = null)
        {
            SetReturnSceneName(returnSceneName);
            LastLandedBaseId = string.IsNullOrWhiteSpace(lastLandedBaseId) ? string.Empty : lastLandedBaseId.Trim();
            if (!string.IsNullOrWhiteSpace(siteCode))
            {
                SetSiteCode(siteCode);
            }

            HasPendingHostResume = false;
            PendingHostUiRequest = string.Empty;
        }

        public void WriteBaseDefenseOutcome(bool success) => WriteDefenseOutcome(success);

        public void WriteDefenseOutcome(bool success)
        {
            if (HasDefenseOutcome)
            {
                return;
            }

            HasDefenseOutcome = true;
            DefenseSucceeded = success;
            NextFlightHitModifier = success
                ? GameConstants.DefenseSuccessHitDelta
                : GameConstants.DefenseFailureHitDelta;
            PlaneHitPoints = Mathf.Max(GameConstants.MinPlaneHitPoints, PlaneHitPoints + NextFlightHitModifier);
        }

        public void CommitOkReturnToHost()
        {
            HasPendingHostResume = true;
            PendingHostUiRequest = GameConstants.HostMissionCharacterPageUi;
        }

        public bool TryConsumeHostResume(out string lastLandedBaseId, out string hostUiRequest)
        {
            if (!HasPendingHostResume)
            {
                lastLandedBaseId = string.Empty;
                hostUiRequest = string.Empty;
                return false;
            }

            lastLandedBaseId = LastLandedBaseId;
            hostUiRequest = PendingHostUiRequest;
            HasPendingHostResume = false;
            PendingHostUiRequest = string.Empty;
            return true;
        }

        public int GetPlaneHitsForNextFlight() => PlaneHitPoints;

        public void ConsumeNextFlightHitModifier()
        {
            NextFlightHitModifier = 0;
        }

        public void ResetForNewDefenseEvent()
        {
            HasDefenseOutcome = false;
            DefenseSucceeded = false;
            NextFlightHitModifier = 0;
            LastLandedBaseId = string.Empty;
            ActiveDefenseMissionId = string.Empty;
            SiteCode = string.Empty;
            HasPendingHostResume = false;
            PendingHostUiRequest = string.Empty;
        }
    }
}
