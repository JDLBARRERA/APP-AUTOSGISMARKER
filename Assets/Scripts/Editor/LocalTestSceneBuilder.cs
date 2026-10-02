using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ARTrackBuilder.AR;
using ARTrackBuilder.Competition;
using ARTrackBuilder.Core;
using ARTrackBuilder.Data;
using ARTrackBuilder.Debugging;
using ARTrackBuilder.UI;

namespace ARTrackBuilder.Editor
{
    /// <summary>
    /// Arma la escena de prueba local con gestores, canvas y prefabs ya cableados.
    /// </summary>
    public static class LocalTestSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const string PREFAB_FOLDER = "Assets/Prefabs/AR";
        private const string MATERIAL_FOLDER = "Assets/Materials";

        [MenuItem("ARTrackBuilder/Preparar Escena de Prueba Local")]
        public static void BuildFromMenu()
        {
            if (File.Exists(SCENE_PATH)
                && !EditorUtility.DisplayDialog(
                    "Escena de prueba",
                    "SampleScene ya existe. Se reemplaza con la escena de prueba local.",
                    "Reemplazar",
                    "Cancelar"))
            {
                return;
            }

            BuildInternal();
        }

        internal static void BuildIfMissing()
        {
            if (File.Exists(SCENE_PATH))
            {
                return;
            }

            BuildInternal();
        }

        private static void BuildInternal()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PREFAB_FOLDER);
            EnsureFolder(MATERIAL_FOLDER);

            GameObject dotPrefab = CreateDotPrefab();
            GameObject exactPrefab = CreateMarkerPrefab("ExactZone", new Color(1f, 0.85f, 0.1f), new Vector3(0.08f, 0.005f, 0.08f));
            GameObject turboPrefab = CreateMarkerPrefab("TurboBoost", new Color(0.1f, 1f, 0.3f), new Vector3(0.08f, 0.005f, 0.12f));
            GameObject archPrefab = CreateMarkerPrefab("FinishArch", new Color(1f, 0.15f, 0.05f), new Vector3(0.08f, 0.06f, 0.02f));
            GameObject trackPrefab = CreateTrackPrefab();
            GameObject productCard = CreateProductCardPrefab();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera projectorCamera = CreateProjectorCamera();
            GameObject xrOrigin = CreateInactiveCameraRig("XR_Origin");
            ARSession session = CreateInactiveSession();
            GameObject trackRoot = new GameObject("TrackRoot");

            GameObject managers = new GameObject("AR_Managers");
            TrackDataManager trackData = managers.AddComponent<TrackDataManager>();
            TrackProjectorManager projector = managers.AddComponent<TrackProjectorManager>();
            managers.AddComponent<RaceTurnController>();
            managers.AddComponent<RaceHistoryManager>();
            VAROverlayController varOverlay = managers.AddComponent<VAROverlayController>();
            DisplayModeManager display = managers.AddComponent<DisplayModeManager>();
            TrackWaypointProjection waypoints = trackRoot.AddComponent<TrackWaypointProjection>();
            trackRoot.AddComponent<TrackChalkRibbon>();
            trackRoot.AddComponent<LandscapeGround>();
            managers.AddComponent<FloorReliefScanner>();
            managers.AddComponent<LandscapeDirector>();
            managers.AddComponent<SlotArchive>();
            managers.AddComponent<SlotSessionController>();
            managers.AddComponent<VoiceCommentator>();
            StoreDataManager storeData = managers.AddComponent<StoreDataManager>();
            StoreUIManager storeUi = managers.AddComponent<StoreUIManager>();
            RaceUIManager raceUi = managers.AddComponent<RaceUIManager>();
            RaceDashboardUIManager dashboard = managers.AddComponent<RaceDashboardUIManager>();
            RulebookUIManager rulebook = managers.AddComponent<RulebookUIManager>();
            TrackUIManager trackUi = managers.AddComponent<TrackUIManager>();
            KeystoneCalibrator keystone = managers.AddComponent<KeystoneCalibrator>();

            GameObject harness = new GameObject("Local_Debug_Harness");
            ARDebugMockController mock = harness.AddComponent<ARDebugMockController>();
            harness.AddComponent<LocalTestOverlay>();

            Font font = LoadFont();
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            GameObject canvas = CreateCanvas(font, uiSprite, out LocalUi ui);
            canvas.AddComponent<LandscapeUIManager>();
            canvas.AddComponent<SlotBroadcastUI>();

            Assign(mock, "_projectorManager", projector);
            Assign(mock, "_varController", varOverlay);
            Assign(projector, "_trackData", trackData);
            Assign(waypoints, "_waypointDotPrefab", dotPrefab);
            Assign(waypoints, "_exactZonePrefab", exactPrefab);
            Assign(waypoints, "_turboBoostPrefab", turboPrefab);
            Assign(waypoints, "_finishArchPrefab", archPrefab);
            Assign(display, "_xrOriginObject", xrOrigin);
            Assign(display, "_projectorCamera", projectorCamera);
            Assign(display, "_arSession", session);
            Assign(display, "_projector", projector);
            Assign(display, "_keystoneCalibrationPanel", ui.KeystonePanel);
            SetEnum(display, "_currentMode", (int)DisplayMode.TableProjector);
            Assign(varOverlay, "_varPanel", ui.VarPanel);
            Assign(varOverlay, "_varStatusText", ui.VarStatus);
            Assign(varOverlay, "_caliperBox", ui.Caliper);
            Assign(varOverlay, "_shotCountText", ui.ShotText);
            Assign(trackUi, "_projectorManager", projector);
            Assign(trackUi, "_opacitySlider", ui.OpacitySlider);
            Assign(trackUi, "_lockButton", ui.LockButton);
            Assign(trackUi, "_lockButtonText", ui.LockLabel);
            Assign(keystone, "_trackContainerTransform", trackRoot.transform);
            Assign(raceUi, "_historyManager", managers.GetComponent<RaceHistoryManager>());
            Assign(raceUi, "_projector", projector);
            Assign(raceUi, "_registrationPanel", ui.RegistrationPanel);
            Assign(raceUi, "_podiumPanel", ui.PodiumPanel);
            Assign(raceUi, "_playerNameInput", ui.NameInput);
            Assign(raceUi, "_carModelInput", ui.CarInput);
            Assign(raceUi, "_addPlayerButton", ui.AddPlayerButton);
            Assign(raceUi, "_playerListText", ui.PlayerList);
            Assign(raceUi, "_startRaceButton", ui.StartRaceButton);
            Assign(raceUi, "_winnerDropdown", ui.WinnerDropdown);
            Assign(raceUi, "_saveResultButton", ui.SaveRaceButton);
            Assign(raceUi, "_dashboard", dashboard);
            Assign(dashboard, "CurrentPlayerText", ui.CurrentPlayerText);
            Assign(dashboard, "NextPlayerText", ui.NextPlayerText);
            AssignArray(dashboard, "ShotCards", ui.ShotCardBackgrounds);
            AssignArray(dashboard, "ShotCardTexts", ui.ShotCardStatusTexts);
            Assign(dashboard, "ActionButton", ui.RegisterShotButton);
            Assign(dashboard, "PassTurnButton", ui.EndTurnButton);
            Assign(dashboard, "ActionButtonText", ui.ActionButtonText);
            Assign(dashboard, "HistoryText", ui.TurnHistoryText);
            Assign(dashboard, "TrackNameText", ui.TrackNameText);
            Assign(dashboard, "RuleButtonRoot", ui.RuleButtonRoot.transform);
            Assign(storeUi, "_storeData", storeData);
            Assign(storeUi, "_storePanel", ui.StorePanel);
            Assign(storeUi, "_productPrefab", productCard);
            Assign(storeUi, "_catalogGridParent", ui.StoreGrid.transform);
            Assign(storeUi, "_openStoreButton", ui.OpenStoreButton);
            Assign(storeUi, "_closeStoreButton", ui.CloseStoreButton);
            Assign(rulebook, "_rulebookPanel", ui.RulebookPanel);
            Assign(rulebook, "_rulebookText", ui.RulebookText);
            Assign(rulebook, "_openRulebookButton", ui.OpenRulebookButton);
            Assign(rulebook, "_closeRulebookButton", ui.CloseRulebookButton);

            WriteTrackCatalog(trackData, trackPrefab);
            WriteStoreCatalog(storeData);

            UnityEventTools.AddVoidPersistentListener(ui.DrawButton.onClick, waypoints.GenerateDefaultCircuit);
            UnityEventTools.AddVoidPersistentListener(ui.RaceButton.onClick, waypoints.BeginRace);
            UnityEventTools.AddVoidPersistentListener(ui.CityButton.onClick, waypoints.SelectCityRace);
            UnityEventTools.AddVoidPersistentListener(ui.MediumButton.onClick, waypoints.SelectMediumRace);
            UnityEventTools.AddVoidPersistentListener(ui.GrandPrixButton.onClick, waypoints.SelectGrandPrixRace);
            UnityEventTools.AddVoidPersistentListener(ui.NextRaceButton.onClick, waypoints.SiguienteCarrera);
            UnityEventTools.AddVoidPersistentListener(ui.ModeButton.onClick, display.ToggleMode);

            EditorSceneManager.SaveScene(projectorCamera.scene, SCENE_PATH);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[AR] Escena de prueba lista en " + SCENE_PATH + ". Pulsa Play.");
        }

        private static Camera CreateProjectorCamera()
        {
            GameObject cameraObject = new GameObject("ProjectorCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 0.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 1.6f, 0f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static GameObject CreateInactiveCameraRig(string name)
        {
            GameObject origin = new GameObject(name);
            GameObject cameraObject = new GameObject("ARCamera");
            cameraObject.transform.SetParent(origin.transform, false);
            cameraObject.AddComponent<Camera>();
            origin.SetActive(false);
            return origin;
        }

        private static ARSession CreateInactiveSession()
        {
            GameObject sessionObject = new GameObject("AR Session");
            ARSession session = sessionObject.AddComponent<ARSession>();
            sessionObject.SetActive(false);
            return session;
        }

        private static GameObject CreateCanvas(Font font, Sprite sprite, out LocalUi ui)
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<StandaloneInputModule>();

            ui = new LocalUi
            {
                ShotText = CreateText(canvasObject.transform, "ShotText", "TIRO: 1 / 3", font, 42, TextAnchor.UpperCenter, new Vector2(0f, -24f), new Vector2(700f, 70f)),
                OpacitySlider = CreateSlider(canvasObject.transform, sprite),
                LockButton = CreateButton(canvasObject.transform, "Button_Lock", "FIJAR PISTA", font, sprite, new Vector2(760f, -180f), new Vector2(280f, 64f), out uiLockLabel),
                DrawButton = CreateButton(canvasObject.transform, "Button_Draw", "UNIR PUNTOS", font, sprite, new Vector2(-620f, 40f), new Vector2(280f, 64f), out _),
                RaceButton = CreateButton(canvasObject.transform, "Button_BeginRace", "COMENZAR CARRERA", font, sprite, new Vector2(-300f, 40f), new Vector2(320f, 64f), out _),
                CityButton = CreateButton(canvasObject.transform, "Button_City", "CIRCUITO", font, sprite, new Vector2(-780f, 130f), new Vector2(220f, 56f), out _),
                MediumButton = CreateButton(canvasObject.transform, "Button_Medium", "GRAN CIRCUITO", font, sprite, new Vector2(-490f, 130f), new Vector2(320f, 56f), out _),
                GrandPrixButton = CreateButton(canvasObject.transform, "Button_GrandPrix", "GRAND PRIX", font, sprite, new Vector2(-180f, 130f), new Vector2(260f, 56f), out _),
                ModeButton = CreateButton(canvasObject.transform, "Button_Mode", "MODO PROYECTOR", font, sprite, new Vector2(40f, 40f), new Vector2(300f, 64f), out _),
                OpenStoreButton = CreateButton(canvasObject.transform, "Button_Store", "TIENDA", font, sprite, new Vector2(360f, 40f), new Vector2(200f, 64f), out _),
                OpenRulebookButton = CreateButton(canvasObject.transform, "Button_Rules", "REGLAMENTO", font, sprite, new Vector2(600f, 40f), new Vector2(240f, 64f), out _)
            };
            ui.LockLabel = uiLockLabel;
            RectTransform shotRect = ui.ShotText.rectTransform;
            shotRect.anchorMin = new Vector2(0.5f, 1f);
            shotRect.anchorMax = new Vector2(0.5f, 1f);
            shotRect.pivot = new Vector2(0.5f, 1f);
            shotRect.anchoredPosition = new Vector2(0f, -24f);

            ui.VarPanel = CreatePanel(canvasObject.transform, "Panel_VAR", sprite, new Color(0f, 0f, 0f, 0.75f), new Vector2(0f, 80f), new Vector2(640f, 420f));
            ui.Caliper = CreateCaliper(ui.VarPanel.transform, sprite);
            ui.VarStatus = CreateText(ui.VarPanel.transform, "VarStatus", "VAR", font, 28, TextAnchor.UpperCenter, new Vector2(0f, -16f), new Vector2(600f, 80f));
            ui.VarPanel.SetActive(false);

            ui.KeystonePanel = CreatePanel(canvasObject.transform, "Panel_Keystone", sprite, new Color(0.05f, 0.05f, 0.05f, 0.8f), new Vector2(0f, 140f), new Vector2(520f, 80f));
            CreateText(ui.KeystonePanel.transform, "KeystoneLabel", "CALIBRACION DE MESA", font, 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500f, 60f));

            ui.StorePanel = CreatePanel(canvasObject.transform, "Panel_Tienda", sprite, new Color(0.08f, 0.08f, 0.08f, 0.92f), Vector2.zero, new Vector2(900f, 640f));
            ui.StoreGrid = new GameObject("StoreGrid");
            ui.StoreGrid.transform.SetParent(ui.StorePanel.transform, false);
            RectTransform gridRect = ui.StoreGrid.AddComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(820f, 460f);
            ui.StoreGrid.AddComponent<GridLayoutGroup>().cellSize = new Vector2(240f, 140f);
            ui.CloseStoreButton = CreateButton(ui.StorePanel.transform, "Button_CloseStore", "CERRAR", font, sprite, new Vector2(0f, -260f), new Vector2(220f, 56f), out _);
            ui.StorePanel.SetActive(false);

            ui.RulebookPanel = CreatePanel(canvasObject.transform, "Panel_Reglamento", sprite, new Color(0.08f, 0.08f, 0.08f, 0.92f), Vector2.zero, new Vector2(980f, 720f));
            ui.RulebookText = CreateText(ui.RulebookPanel.transform, "RulebookText", string.Empty, font, 22, TextAnchor.UpperLeft, new Vector2(0f, -20f), new Vector2(900f, 560f));
            ui.CloseRulebookButton = CreateButton(ui.RulebookPanel.transform, "Button_CloseRules", "CERRAR", font, sprite, new Vector2(0f, -310f), new Vector2(220f, 56f), out _);
            ui.RulebookPanel.SetActive(false);

            ui.RegistrationPanel = CreatePanel(canvasObject.transform, "Panel_Registro", sprite, new Color(0f, 0f, 0f, 0.55f), new Vector2(700f, -120f), new Vector2(420f, 520f));
            ui.NameInput = CreateInput(ui.RegistrationPanel.transform, "Input_Name", "Nombre", font, sprite, new Vector2(0f, 160f));
            ui.CarInput = CreateInput(ui.RegistrationPanel.transform, "Input_Car", "Auto", font, sprite, new Vector2(0f, 80f));
            ui.AddPlayerButton = CreateButton(ui.RegistrationPanel.transform, "Button_AddPlayer", "AÑADIR", font, sprite, new Vector2(0f, 10f), new Vector2(240f, 52f), out _);
            ui.PlayerList = CreateText(ui.RegistrationPanel.transform, "PlayerList", "Participantes:", font, 20, TextAnchor.UpperLeft, new Vector2(0f, -80f), new Vector2(360f, 180f));
            ui.StartRaceButton = CreateButton(ui.RegistrationPanel.transform, "Button_StartRace", "INICIAR", font, sprite, new Vector2(0f, -210f), new Vector2(240f, 52f), out _);

            ui.PodiumPanel = CreatePanel(canvasObject.transform, "Panel_Podio", sprite, new Color(0f, 0f, 0f, 0.7f), new Vector2(700f, -120f), new Vector2(420f, 280f));
            ui.WinnerDropdown = CreateDropdown(ui.PodiumPanel.transform, font, sprite);
            ui.SaveRaceButton = CreateButton(ui.PodiumPanel.transform, "Button_SaveRace", "GUARDAR", font, sprite, new Vector2(0f, -80f), new Vector2(240f, 52f), out _);
            ui.PodiumPanel.SetActive(false);
            CreateDashboard(canvasObject.transform, font, sprite, ui);
            return canvasObject;
        }

        private static void CreateDashboard(Transform canvas, Font font, Sprite sprite, LocalUi ui)
        {
            ui.DashboardPanel = CreatePanel(canvas, "Panel_Dashboard", sprite, new Color(0.05f, 0.07f, 0.1f, 0.88f), new Vector2(-680f, 70f), new Vector2(500f, 700f));
            ui.CurrentPlayerText = CreateText(ui.DashboardPanel.transform, "Text_CurrentPlayer", "TURNO DE: JUGADOR 1", font, 26, TextAnchor.MiddleCenter, new Vector2(0f, 290f), new Vector2(460f, 48f));
            ui.NextPlayerText = CreateText(ui.DashboardPanel.transform, "Text_NextPlayer", "SIGUIENTE: Jugador 2", font, 20, TextAnchor.MiddleCenter, new Vector2(0f, 240f), new Vector2(460f, 36f));
            ui.NextPlayerText.color = new Color(0.75f, 0.85f, 0.95f, 1f);

            ui.ShotCardBackgrounds = new Image[3];
            ui.ShotCardStatusTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject card = CreatePanel(ui.DashboardPanel.transform, "Card_Tiro" + (i + 1), sprite, Color.white, new Vector2(-155f + (i * 155f), 130f), new Vector2(140f, 130f));
                Text title = CreateText(card.transform, "Title", "TIRO " + (i + 1), font, 18, TextAnchor.UpperCenter, new Vector2(0f, 40f), new Vector2(120f, 32f));
                title.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                Text status = CreateText(card.transform, "Status", i == 0 ? "EN CURSO" : "PENDIENTE", font, 16, TextAnchor.MiddleCenter, new Vector2(0f, -16f), new Vector2(120f, 36f));
                status.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                ui.ShotCardBackgrounds[i] = card.GetComponent<Image>();
                ui.ShotCardStatusTexts[i] = status;
            }

            ui.RegisterShotButton = CreateButton(ui.DashboardPanel.transform, "Button_YaTire", "¡YA TIRÉ! (TIRO 1 DE 3)", font, sprite, new Vector2(0f, 150f), new Vector2(440f, 72f), out ui.ActionButtonText);
            ui.RegisterShotButton.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.95f, 1f);
            ui.EndTurnButton = CreateButton(ui.DashboardPanel.transform, "Button_TerminarTurno", "TERMINAR TURNO (2 TIROS)", font, sprite, new Vector2(0f, 80f), new Vector2(440f, 52f), out _);
            ui.TurnHistoryText = CreateText(ui.DashboardPanel.transform, "Text_TurnHistory", "Historial", font, 18, TextAnchor.UpperLeft, new Vector2(0f, -30f), new Vector2(440f, 150f));
            ui.TrackNameText = CreateText(canvas, "Text_TrackName", "Sin pista", font, 22, TextAnchor.MiddleCenter, new Vector2(500f, 300f), new Vector2(340f, 48f));
            ui.NextRaceButton = CreateButton(canvas, "Button_SiguienteCarrera", "SIGUIENTE CARRERA", font, sprite, new Vector2(480f, 360f), new Vector2(340f, 64f), out _);
            ui.RuleButtonRoot = new GameObject("RuleButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            ui.RuleButtonRoot.transform.SetParent(canvas, false);
            RectTransform ruleRect = ui.RuleButtonRoot.GetComponent<RectTransform>();
            ruleRect.sizeDelta = new Vector2(300f, 520f);
            ruleRect.anchoredPosition = new Vector2(500f, -40f);
            VerticalLayoutGroup ruleLayout = ui.RuleButtonRoot.GetComponent<VerticalLayoutGroup>();
            ruleLayout.spacing = 8f;
            ruleLayout.childAlignment = TextAnchor.UpperCenter;
            ruleLayout.childControlWidth = true;
            ruleLayout.childControlHeight = true;
            ruleLayout.childForceExpandWidth = true;
            ruleLayout.childForceExpandHeight = false;
        }

        private static Text uiLockLabel;

        private static GameObject CreateDotPrefab()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "WaypointDot";
            root.transform.localScale = Vector3.one * 0.03f;
            ApplyEmission(root.GetComponent<Renderer>(), new Color(0.2f, 0.9f, 1f));
            Object.DestroyImmediate(root.GetComponent<Collider>());

            GameObject label = new GameObject("Number");
            label.transform.SetParent(root.transform, false);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            label.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = "1";
            text.characterSize = 0.4f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;
            text.font = LoadFont();
            return SavePrefab(root, PREFAB_FOLDER + "/WaypointDot.prefab");
        }

        private static GameObject CreateMarkerPrefab(string name, Color color, Vector3 scale)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = name;
            root.transform.localScale = scale;
            ApplyEmission(root.GetComponent<Renderer>(), color);
            Object.DestroyImmediate(root.GetComponent<Collider>());
            return SavePrefab(root, PREFAB_FOLDER + "/" + name + ".prefab");
        }

        private static GameObject CreateTrackPrefab()
        {
            GameObject root = new GameObject("Prefab_NeonTrack_01");
            root.AddComponent<MeshFilter>();
            root.AddComponent<MeshRenderer>();
            root.AddComponent<NeonTrackGenerator>();
            root.AddComponent<NeonPulseEffect>();
            return SavePrefab(root, PREFAB_FOLDER + "/Prefab_NeonTrack_01.prefab");
        }

        private static GameObject CreateProductCardPrefab()
        {
            Font font = LoadFont();
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            GameObject card = new GameObject("ProductCard");
            RectTransform rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 140f);
            Image image = card.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            CreateText(card.transform, "NameText", "Producto", font, 22, TextAnchor.UpperCenter, new Vector2(0f, -12f), new Vector2(220f, 40f));
            CreateText(card.transform, "PriceText", "$0.00", font, 20, TextAnchor.MiddleCenter, new Vector2(0f, -10f), new Vector2(220f, 36f));
            CreateButton(card.transform, "BuyButton", "COMPRAR", font, sprite, new Vector2(0f, -46f), new Vector2(180f, 40f), out _);
            return SavePrefab(card, PREFAB_FOLDER + "/ProductCard.prefab");
        }

        private static void WriteTrackCatalog(TrackDataManager trackData, GameObject trackPrefab)
        {
            SerializedObject serialized = new SerializedObject(trackData);
            SerializedProperty tracks = serialized.FindProperty("_availableTracks");
            tracks.arraySize = 1;
            SerializedProperty element = tracks.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("TrackID").stringValue = "neon01";
            element.FindPropertyRelative("DisplayName").stringValue = "Pista Neon";
            element.FindPropertyRelative("TrackPrefab").objectReferenceValue = trackPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteStoreCatalog(StoreDataManager storeData)
        {
            SerializedObject serialized = new SerializedObject(storeData);
            SerializedProperty catalog = serialized.FindProperty("_catalog");
            catalog.arraySize = 1;
            SerializedProperty element = catalog.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("ProductID").stringValue = "gis01";
            element.FindPropertyRelative("DisplayName").stringValue = "Gis blanco";
            element.FindPropertyRelative("Category").enumValueIndex = (int)ProductCategory.Gises;
            element.FindPropertyRelative("Description").stringValue = "Gis para unir los puntos de la pista.";
            element.FindPropertyRelative("Price").floatValue = 49f;
            element.FindPropertyRelative("PurchaseURL").stringValue = "https://example.com";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings()
        {
            System.Collections.Generic.List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(scene => scene.path == SCENE_PATH);
            scenes.Insert(0, new EditorBuildSettingsScene(SCENE_PATH, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int size, TextAnchor anchor, Vector2 position, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Sprite sprite, Vector2 position, Vector2 size, out Text labelText)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            Button button = buttonObject.AddComponent<Button>();
            labelText = CreateText(buttonObject.transform, "Text", label, font, 22, TextAnchor.MiddleCenter, Vector2.zero, size);
            return button;
        }

        private static Slider CreateSlider(Transform parent, Sprite sprite)
        {
            GameObject sliderObject = new GameObject("OpacitySlider");
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-40f, -40f);
            rect.sizeDelta = new Vector2(280f, 30f);
            Slider slider = sliderObject.AddComponent<Slider>();

            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObject.transform, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = sprite;
            backgroundImage.color = new Color(1f, 1f, 1f, 0.25f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(8f, 0f);
            fillAreaRect.offsetMax = new Vector2(-8f, 0f);
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.color = new Color(0.2f, 0.9f, 1f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.sprite = sprite;
            handleImage.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 30f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = 0.15f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        private static GameObject CreatePanel(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return panel;
        }

        private static RectTransform CreateCaliper(Transform parent, Sprite sprite)
        {
            GameObject caliper = new GameObject("Caliper");
            caliper.transform.SetParent(parent, false);
            RectTransform rect = caliper.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 180f);
            Image image = caliper.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(1f, 0.9f, 0.1f, 0.35f);
            return rect;
        }

        private static InputField CreateInput(Transform parent, string name, string placeholder, Font font, Sprite sprite, Vector2 position)
        {
            GameObject inputObject = new GameObject(name);
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(320f, 48f);
            Image image = inputObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            InputField input = inputObject.AddComponent<InputField>();
            Text text = CreateText(inputObject.transform, "Text", string.Empty, font, 20, TextAnchor.MiddleLeft, new Vector2(8f, 0f), new Vector2(300f, 40f));
            text.color = Color.black;
            Text hint = CreateText(inputObject.transform, "Placeholder", placeholder, font, 20, TextAnchor.MiddleLeft, new Vector2(8f, 0f), new Vector2(300f, 40f));
            hint.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            input.textComponent = text;
            input.placeholder = hint;
            return input;
        }

        private static Dropdown CreateDropdown(Transform parent, Font font, Sprite sprite)
        {
            GameObject dropdownObject = new GameObject("WinnerDropdown");
            dropdownObject.transform.SetParent(parent, false);
            RectTransform rect = dropdownObject.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(320f, 48f);
            Image image = dropdownObject.AddComponent<Image>();
            image.sprite = sprite;
            Dropdown dropdown = dropdownObject.AddComponent<Dropdown>();
            Text caption = CreateText(dropdownObject.transform, "Label", "Ganador", font, 20, TextAnchor.MiddleLeft, new Vector2(10f, 0f), new Vector2(280f, 40f));
            caption.color = Color.black;
            dropdown.captionText = caption;
            dropdown.targetGraphic = image;

            GameObject template = CreatePanel(dropdownObject.transform, "Template", sprite, Color.white, new Vector2(0f, -80f), new Vector2(320f, 120f));
            template.SetActive(false);
            template.AddComponent<ScrollRect>();
            dropdown.template = template.GetComponent<RectTransform>();
            Text itemLabel = CreateText(template.transform, "Item Label", "Opcion", font, 20, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(300f, 36f));
            itemLabel.color = Color.black;
            dropdown.itemText = itemLabel;
            return dropdown;
        }

        private static void ApplyEmission(Renderer renderer, Color color)
        {
            Shader shader = Shader.Find("Standard");
            Material material = shader != null ? new Material(shader) : renderer.material;
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2f);
            string path = MATERIAL_FOLDER + "/Mat_" + renderer.gameObject.name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(material, path);
            renderer.sharedMaterial = material;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignArray(Object target, string propertyName, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Font LoadFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private sealed class LocalUi
        {
            public Text ShotText;
            public Slider OpacitySlider;
            public Button LockButton;
            public Text LockLabel;
            public Button DrawButton;
            public Button RaceButton;
            public Button CityButton;
            public Button MediumButton;
            public Button GrandPrixButton;
            public Button ModeButton;
            public Button OpenStoreButton;
            public Button CloseStoreButton;
            public Button OpenRulebookButton;
            public Button CloseRulebookButton;
            public GameObject VarPanel;
            public Text VarStatus;
            public RectTransform Caliper;
            public GameObject KeystonePanel;
            public GameObject StorePanel;
            public GameObject StoreGrid;
            public GameObject RulebookPanel;
            public Text RulebookText;
            public GameObject RegistrationPanel;
            public GameObject PodiumPanel;
            public InputField NameInput;
            public InputField CarInput;
            public Button AddPlayerButton;
            public Text PlayerList;
            public Button StartRaceButton;
            public Dropdown WinnerDropdown;
            public Button SaveRaceButton;
            public GameObject DashboardPanel;
            public Text CurrentPlayerText;
            public Text NextPlayerText;
            public Image[] ShotCardBackgrounds;
            public Text[] ShotCardStatusTexts;
            public Button RegisterShotButton;
            public Button EndTurnButton;
            public Text ActionButtonText;
            public Text TurnHistoryText;
            public Text TrackNameText;
            public Button NextRaceButton;
            public GameObject RuleButtonRoot;
        }
    }

    [InitializeOnLoad]
    internal static class LocalTestSceneAutoBuilder
    {
        private const string SESSION_KEY = "ARTrackBuilder.LocalSceneAttempted";

        static LocalTestSceneAutoBuilder()
        {
            EditorApplication.delayCall += TryBuild;
        }

        private static void TryBuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryBuild;
                return;
            }

            if (SessionState.GetBool(SESSION_KEY, false))
            {
                return;
            }

            SessionState.SetBool(SESSION_KEY, true);
            LocalTestSceneBuilder.BuildIfMissing();
        }
    }
}
