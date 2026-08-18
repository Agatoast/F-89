using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Core
{
    public sealed class LevelBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            MatchController.Ensure();
        }
    }
}
