using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Flat runway graphic placed beside each outpost building cluster at a random heading,
    /// with a type-3 runway tower on the strip.
    /// </summary>
    public static class OutpostRunwayVisual
    {
        public const string RunwayObjectName = "OutpostRunway";
        public const string RunwayTowerObjectName = "RunwayTower";
        public const string RunwayBunkerObjectName = "RunwayBunker";
        private const string TextureResourcePath = "Outpost/runway";
        private const float RunwaySpanTacs = 12f;
        private const float GroundLift = 0.03f;
        /// <summary>Center of the middle triangle marking on the runway line-art (UV space).</summary>
        private const float TriangleCenterU = 0.5f;
        private const float TowerTriangleCenterV = 0.55f;
        private const float BunkerTriangleCenterV = 0.42f;
        /// <summary>Interior width of that triangle as a fraction of full runway width.</summary>
        private const float TriangleInteriorWidthFraction = 0.36f;

        public static void EnsureAt(
            AntarcticaBase baseSite,
            Vector3 clusterCenter,
            float keepOutRadiusWorld,
            float ticSizeWorldUnits,
            System.Random rng)
        {
            if (baseSite == null || rng == null)
            {
                return;
            }

            var runway = baseSite.transform.Find(RunwayObjectName);
            if (runway == null)
            {
                runway = CreateRunway(baseSite, clusterCenter, keepOutRadiusWorld, ticSizeWorldUnits, rng)?.transform;
            }

            if (runway == null)
            {
                return;
            }

            EnsureRunwayTower(baseSite, runway, ticSizeWorldUnits);
            EnsureRunwayBunker(baseSite, runway, ticSizeWorldUnits);
        }

        public static void ApplySavedBunkerDestruction(AntarcticaBase baseSite, float ticSizeWorldUnits)
        {
            if (baseSite == null)
            {
                return;
            }

            if (!AntarcticaOutpostState.IsTargetDestroyedForBase(
                    baseSite,
                    OutpostPrimaryObjective.BunkerBuildingLabel))
            {
                return;
            }

            var bunker = baseSite.transform.Find(RunwayBunkerObjectName);
            if (bunker != null)
            {
                var building = bunker.GetComponent<OutpostBuilding>();
                if (building != null && !building.IsDestroyed)
                {
                    building.MarkDestroyed();
                }
                else if (bunker.gameObject.activeSelf)
                {
                    bunker.gameObject.SetActive(false);
                }
            }

            var runway = baseSite.transform.Find(RunwayObjectName);
            if (runway == null)
            {
                return;
            }

            var footprint = OutpostGroundRules.FootprintWorld(ticSizeWorldUnits);
            var groundPosition = GetRunwayBunkerWorldPosition(runway);
            OutpostSurfaceBunkerPad.EnsureAt(baseSite.transform, groundPosition, footprint);
        }

        public static void ApplySavedTowerDestruction(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return;
            }

            if (!AntarcticaOutpostState.IsTargetDestroyedForBase(
                    baseSite,
                    OutpostPrimaryObjective.RunwayTowerLabel))
            {
                return;
            }

            var tower = baseSite.transform.Find(RunwayTowerObjectName);
            if (tower == null)
            {
                return;
            }

            var building = tower.GetComponent<OutpostBuilding>();
            if (building != null && !building.IsDestroyed)
            {
                building.MarkDestroyed();
            }
            else if (tower.gameObject.activeSelf)
            {
                tower.gameObject.SetActive(false);
            }
        }

        public static void ApplyFriendlyBaseCleanup(AntarcticaBase baseSite)
        {
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
            {
                return;
            }

            AntarcticaOutpostState.MarkMissionStructuresClearedForBase(baseSite);

            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            ApplySavedTowerDestruction(baseSite);
            ApplySavedBunkerDestruction(baseSite, ticSize);
            RefreshBuildingAffiliations(baseSite);
            F89.Flight.CombatThreatRange.InvalidateCaches();
        }

        private static void RefreshBuildingAffiliations(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return;
            }

            var buildings = baseSite.GetComponentsInChildren<OutpostBuilding>(true);
            for (var i = 0; i < buildings.Length; i++)
            {
                var building = buildings[i];
                if (building == null || building.IsDestroyed)
                {
                    continue;
                }

                var lockable = building.GetComponent<LockableTarget>();
                if (lockable == null)
                {
                    continue;
                }

                lockable.Configure(
                    lockable.TargetLabel,
                    LockableTargetKind.Ground,
                    OutpostPrimaryObjective.AffiliationForBuilding(
                        building.BuildingType,
                        lockable.TargetLabel,
                        baseSite),
                    TargetUnitClass.Building);
            }
        }

        private static GameObject CreateRunway(
            AntarcticaBase baseSite,
            Vector3 clusterCenter,
            float keepOutRadiusWorld,
            float ticSizeWorldUnits,
            System.Random rng)
        {
            var texture = Resources.Load<Texture2D>(TextureResourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"F-89: Missing runway texture at Resources/{TextureResourcePath}.");
                return null;
            }

            var span = TacScale.TacsToWorld(RunwaySpanTacs, ticSizeWorldUnits);
            var aspect = texture.width / (float)texture.height;
            var lengthWorld = span;
            var widthWorld = span * aspect;
            var heading = (float)(rng.NextDouble() * 360.0);
            var offsetAngle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            var offsetDistance = keepOutRadiusWorld * 1.02f + span * 0.28f;
            var position = clusterCenter + new Vector3(
                Mathf.Cos(offsetAngle) * offsetDistance,
                GroundLift,
                Mathf.Sin(offsetAngle) * offsetDistance);

            var runway = GameObject.CreatePrimitive(PrimitiveType.Quad);
            runway.name = RunwayObjectName;
            runway.transform.SetParent(baseSite.transform, true);
            runway.transform.position = position;
            runway.transform.rotation = Quaternion.Euler(90f, heading, 0f);
            runway.transform.localScale = new Vector3(widthWorld, lengthWorld, 1f);

            var collider = runway.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = runway.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = F89RenderMaterials.CreateUnlitTransparentTextured(texture, Color.white);
                if (material != null)
                {
                    if (material.HasProperty("_Glossiness"))
                    {
                        material.SetFloat("_Glossiness", 0f);
                    }

                    renderer.sharedMaterial = material;
                }

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return runway;
        }

        private static void EnsureRunwayBunker(
            AntarcticaBase baseSite,
            Transform runway,
            float ticSizeWorldUnits)
        {
            if (baseSite == null
                || runway == null
                || baseSite.Control != BaseControl.Hostile)
            {
                return;
            }

            var existing = baseSite.transform.Find(RunwayBunkerObjectName);
            if (existing == null)
            {
                if (AntarcticaOutpostState.IsTargetDestroyedForBase(
                        baseSite,
                        OutpostPrimaryObjective.BunkerBuildingLabel))
                {
                    ApplySavedBunkerDestruction(baseSite, ticSizeWorldUnits);
                    return;
                }

                var bunker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bunker.name = RunwayBunkerObjectName;
                bunker.transform.SetParent(baseSite.transform, true);
                ApplyRunwayBunkerOrientation(bunker.transform, runway, baseSite.transform, ticSizeWorldUnits);

                var component = bunker.AddComponent<OutpostBuilding>();
                component.Configure(OutpostBuildingType.Bunker);

                var lockable = bunker.AddComponent<LockableTarget>();
                lockable.Configure(
                    OutpostPrimaryObjective.BunkerBuildingLabel,
                    LockableTargetKind.Ground,
                    OutpostPrimaryObjective.AffiliationForBuilding(
                        OutpostBuildingType.Bunker,
                        OutpostPrimaryObjective.BunkerBuildingLabel,
                        baseSite),
                    TargetUnitClass.Building);
                lockable.SetMaxGroundHitPoints(OutpostBuildingGhp.ForType(OutpostBuildingType.Bunker));
                lockable.SetHitRadiusWorld(
                    ResolveRunwayBunkerFootprint(runway, ticSizeWorldUnits) * 0.55f);

                var collider = bunker.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }

                var renderer = bunker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = F89RenderMaterials.CreateUnlit(new Color(0.22f, 0.22f, 0.24f));
                    if (material != null)
                    {
                        renderer.sharedMaterial = material;
                    }
                }

                return;
            }

            ApplyRunwayBunkerOrientation(existing, runway, baseSite.transform, ticSizeWorldUnits);
        }

        private static void ApplyRunwayBunkerOrientation(
            Transform bunker,
            Transform runway,
            Transform baseSiteRoot,
            float ticSizeWorldUnits)
        {
            if (bunker == null || runway == null)
            {
                return;
            }

            if (baseSiteRoot != null && bunker.parent != baseSiteRoot)
            {
                bunker.SetParent(baseSiteRoot, true);
            }

            var footprint = ResolveRunwayBunkerFootprint(runway, ticSizeWorldUnits);
            var height = TacScale.TacsToWorld(1f, ticSizeWorldUnits);
            var anchor = GetRunwayBunkerWorldPosition(runway);
            anchor.y = height * 0.5f + GroundLift;

            bunker.SetPositionAndRotation(anchor, Quaternion.identity);
            bunker.localScale = new Vector3(footprint, height, footprint);
        }

        private static float ResolveRunwayBunkerFootprint(Transform runway, float ticSizeWorldUnits)
        {
            var defaultFootprint = OutpostGroundRules.FootprintWorld(ticSizeWorldUnits);
            if (runway == null)
            {
                return defaultFootprint;
            }

            var runwayWidth = Mathf.Abs(runway.lossyScale.x);
            var triangleWidth = runwayWidth * TriangleInteriorWidthFraction;
            return Mathf.Min(defaultFootprint, triangleWidth * 0.88f);
        }

        public static Vector3 GetRunwayBunkerWorldPosition(Transform runway)
        {
            if (runway == null)
            {
                return Vector3.zero;
            }

            var local = GetRunwayTriangleLocalPosition(BunkerTriangleCenterV);
            var world = runway.TransformPoint(local);
            world.y = GroundLift;
            return world;
        }

        private static void EnsureRunwayTower(
            AntarcticaBase baseSite,
            Transform runway,
            float ticSizeWorldUnits)
        {
            if (baseSite == null || runway == null)
            {
                return;
            }

            if (baseSite.Control != BaseControl.Hostile
                || AntarcticaOutpostState.IsFriendlyOccupied(baseSite.BaseName)
                || (!string.IsNullOrWhiteSpace(baseSite.SiteCode)
                    && AntarcticaOutpostState.IsFriendlyOccupied(baseSite.SiteCode)))
            {
                ApplySavedTowerDestruction(baseSite);
                return;
            }

            var existing = runway.Find(RunwayTowerObjectName);
            if (existing == null)
            {
                existing = baseSite.transform.Find(RunwayTowerObjectName);
            }

            if (existing != null)
            {
                ApplyRunwayTowerOrientation(existing, runway, baseSite.transform, ticSizeWorldUnits);
                return;
            }

            if (AntarcticaOutpostState.IsTargetDestroyedForBase(
                    baseSite,
                    OutpostPrimaryObjective.RunwayTowerLabel))
            {
                return;
            }

            var tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = RunwayTowerObjectName;
            tower.transform.SetParent(baseSite.transform, true);
            ApplyRunwayTowerOrientation(tower.transform, runway, baseSite.transform, ticSizeWorldUnits);

            var component = tower.AddComponent<OutpostBuilding>();
            component.Configure(OutpostBuildingType.Type3);

            var lockable = tower.AddComponent<LockableTarget>();
            lockable.Configure(
                OutpostPrimaryObjective.RunwayTowerLabel,
                LockableTargetKind.Ground,
                OutpostPrimaryObjective.AffiliationForBuilding(
                    OutpostBuildingType.Type3,
                    OutpostPrimaryObjective.RunwayTowerLabel,
                    baseSite),
                TargetUnitClass.Building);
            lockable.SetMaxGroundHitPoints(OutpostBuildingGhp.ForType(OutpostBuildingType.Type3));
            lockable.SetHitRadiusWorld(
                ResolveRunwayTowerFootprint(runway, ticSizeWorldUnits) * 0.55f);

            var collider = tower.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = tower.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = F89RenderMaterials.CreateUnlit(new Color(0.38f, 0.40f, 0.44f));
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        private static void ApplyRunwayTowerOrientation(
            Transform tower,
            Transform runway,
            Transform baseSiteRoot,
            float ticSizeWorldUnits)
        {
            if (tower == null || runway == null)
            {
                return;
            }

            if (baseSiteRoot != null && tower.parent != baseSiteRoot)
            {
                tower.SetParent(baseSiteRoot, true);
            }

            var footprint = ResolveRunwayTowerFootprint(runway, ticSizeWorldUnits);
            var height = TacScale.TacsToWorld(2f, ticSizeWorldUnits);
            var anchor = GetRunwayTowerWorldPosition(runway);
            anchor.y = height * 0.5f + GroundLift;

            tower.SetPositionAndRotation(anchor, Quaternion.identity);
            tower.localScale = new Vector3(footprint, height, footprint);
        }

        /// <summary>
        /// Local position on the runway quad that sits inside the middle triangle marking.
        /// Quad mesh spans -0.5..0.5 on X/Y before scale; texture V maps to local Y.
        /// </summary>
        private static Vector3 GetRunwayTriangleLocalPosition(float triangleCenterV)
        {
            var localX = TriangleCenterU - 0.5f;
            var localY = triangleCenterV - 0.5f;
            return new Vector3(localX, localY, 0f);
        }

        public static Vector3 GetRunwayTowerWorldPosition(Transform runway)
        {
            if (runway == null)
            {
                return Vector3.zero;
            }

            var local = GetRunwayTriangleLocalPosition(TowerTriangleCenterV);
            var world = runway.TransformPoint(local);
            world.y = GroundLift;
            return world;
        }

        public static Vector3 GetRunwayTriangleWorldPosition(Transform runway) =>
            GetRunwayTowerWorldPosition(runway);

        private static float ResolveRunwayTowerFootprint(Transform runway, float ticSizeWorldUnits)
        {
            var defaultFootprint = OutpostGroundRules.FootprintWorld(ticSizeWorldUnits);
            if (runway == null)
            {
                return defaultFootprint;
            }

            var runwayWidth = Mathf.Abs(runway.lossyScale.x);
            var triangleWidth = runwayWidth * TriangleInteriorWidthFraction;
            return Mathf.Min(defaultFootprint, triangleWidth * 0.92f);
        }
    }
}
