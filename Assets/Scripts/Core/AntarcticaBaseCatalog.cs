using System;
using UnityEngine;

namespace F89.Core
{
    [CreateAssetMenu(fileName = "AntarcticaBaseCatalog", menuName = "F-89/Antarctica Base Catalog")]
    public class AntarcticaBaseCatalog : ScriptableObject
    {
        [Serializable]
        public struct BaseDefinition
        {
            public string baseName;
            [Tooltip("Miles east (+X) and north (+Z) from map center.")]
            public Vector2 positionMiles;
            public BaseControl control;
            public bool startsActive;
        }

        public BaseDefinition[] bases = Array.Empty<BaseDefinition>();
    }
}
