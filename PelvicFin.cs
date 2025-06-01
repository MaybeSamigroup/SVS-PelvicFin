using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using Character;
using CharacterCreation;
using ILLGames.Unity.Component;

namespace PelvicFin
{
    public static class Util
    {
        internal static readonly Il2CppSystem.Threading.CancellationTokenSource Canceler = new();
        static Action AwaitDestroy<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            () => SingletonInitializer<T>.Instance.gameObject
                    .GetComponentInChildren<ObservableDestroyTrigger>()
                    .AddDisposableOnDestroy(Disposable.Create(onDestroy + AwaitSetup<T>(onSetup, onDestroy)));
        static Action AwaitSetup<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            () => UniTask.NextFrame().ContinueWith((Action)(() => Hook<T>(onSetup, onDestroy)));
        public static void Hook<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            SingletonInitializer<T>.WaitUntilSetup(Canceler.Token)
                .ContinueWith(onSetup + AwaitDestroy<T>(onSetup, onDestroy));
    }
    public delegate void Either(Action a, Action b);
    public static class FunctionalExtension
    {
        public static Either Either(bool value) => value ? (left, right) => right() : (left, right) => left();
        public static void Either(this bool value, Action left, Action right) => Either(value)(left, right);
        public static void Maybe(this bool value, Action maybe) => value.Either(() => { }, maybe);
        public static T With<T>(this T input, Action<T> sideEffect) => input.With(() => sideEffect(input));
        public static T With<T>(this T input, Action sideEffect)
        {
            sideEffect();
            return input;
        }
    }
    internal static class UIRef
    {
        internal static Transform Window = SV.Config.ConfigWindow
            .Instance.transform.Find("Canvas").Find("Background").Find("MainWindow");
        internal static GameObject Text => Window
            .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
            .Find("CameraSetting").Find("Content").Find("SensitivityX").Find("Title").gameObject;
        internal static GameObject Slider => Window
            .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
            .Find("CameraSetting").Find("Content").Find("SensitivityX").Find("Slider").gameObject;
        internal static GameObject Button => Window
            .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
            .Find("CameraSetting").Find("Content").Find("SensitivityX").Find("btnReset").gameObject;
        internal static GameObject Check => Window
            .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
            .Find("GraphicSetting").Find("Content").Find("Effects").Find("tglSSAO").gameObject;
        internal static Transform HumanCustomRoot =>
            HumanCustom.Instance.transform.Find("UI").Find("Root");
        internal static Transform HSceneRoot =>
            SV.H.HScene.Instance.transform;
    }
    enum ToggleProp
    {
        Son, Sack, Condom, EyesHighlight
    }
    enum CycledProp
    {
        EyebrowsPattern, EyesPattern, MouthPattern, Tears
    }
    enum RangedProp
    {
        EyesOpen, MouthOpen, NipStand, CheekRed, AssRed, Sweat, Wet
    }
    static partial class ToggleExtensions
    {
        static IEnumerable<Renderer> ToRenderers(this Transform tf) =>
            Enumerable.Range(0, tf.childCount).Select(idx => tf.GetChild(idx).gameObject).SelectMany(ToRenderers);
        static IEnumerable<Renderer> ToRenderers(this GameObject go) =>
            go.GetComponents<Renderer>().Concat(ToRenderers(go.transform));
        internal static Tuple<Func<bool>, Action<bool>> Transform(this ToggleProp prop, Human human) =>
            prop switch
            {
                ToggleProp.Son => new(
                    () => human.data.Status.visibleSonAlways,
                    (value) => human.data.Status.visibleSonAlways = value),
                ToggleProp.Sack => new(
                    () => human.body.objBody.ToRenderers()
                        .Where(renderer => "o_dan_f".Equals(renderer.name))
                        .Select(renderer => renderer.enabled).FirstOrDefault(false),
                    (value) => human.body.objBody.ToRenderers()
                        .Where(renderer => "o_dan_f".Equals(renderer.name))
                        .Do(renderer => renderer.enabled = value)),
                ToggleProp.Condom => new(
                    () => human.data.Status.visibleGomu,
                    (value) => human.data.Status.visibleGomu = value),
                ToggleProp.EyesHighlight => new(
                    () => !human.data.Status.hideEyesHighlight,
                    (value) => human.face.HideEyeHighlight(!value)),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
        internal static Tuple<Func<int>, Func<int>, Action<int>> Transform(this CycledProp prop, Human human) =>
            prop switch
            {
                CycledProp.EyebrowsPattern => new(
                    () => human.data.Status.eyebrowPtn,
                    () => (human.data.Status.eyebrowPtn + 1) % human.face.eyebrowCtrl.GetMaxPtn(),
                    (value) => human.face.ChangeEyebrowPtn(value)),
                CycledProp.EyesPattern => new(
                    () => human.data.Status.eyesPtn,
                    () => (human.data.Status.eyesPtn + 1) % human.face.eyesCtrl.GetMaxPtn(),
                    (value) => human.face.ChangeEyesPtn(value)),
                CycledProp.MouthPattern => new(
                    () => human.data.Status.mouthPtn,
                    () => (human.data.Status.mouthPtn + 1) % human.face.mouthCtrl.GetMaxPtn(),
                    (value) => human.face.ChangeMouthPtn(value)),
                CycledProp.Tears => new(
                    () => human.data.Status.tearsLv,
                    () => (human.data.Status.tearsLv + 1) % 4,
                    (value) => human.data.Status.tearsLv = (byte)value),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
        internal static Tuple<Func<float>, Action<float>> Transform(this RangedProp prop, Human human) =>
            prop switch
            {
                RangedProp.EyesOpen => new(
                    () => human.data.Status.eyesOpenMax,
                    (value) => human.face.ChangeEyesOpenMax(value)),
                RangedProp.MouthOpen => new(
                    () => human.data.Status.mouthOpenMax,
                    (value) => human.face.ChangeMouthOpenMax(value)),
                RangedProp.NipStand => new(
                    () => human.data.Status.nipStandRate,
                    (value) => human.body.ChangeNipRate(value)),
                RangedProp.CheekRed => new(
                    () => human.data.Status.hohoAkaRate,
                    (value) => human.face.ChangeHohoAkaRate(new Il2CppSystem.Nullable<float>(value))),
                RangedProp.AssRed => new(
                    () => human.data.Status.siriAkaRate,
                    (value) => human.body.ChangeSiriAkaRate(new Il2CppSystem.Nullable<float>(value))),
                RangedProp.Sweat => new(
                    () => human.data.Status.sweatRate,
                    (value) => human.ChangeSweat(value)),
                RangedProp.Wet => new(
                    () => human.data.Status.wetRate,
                    (value) => human.ChangeWet(value)),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
    }
    internal static class UI
    {
        internal static void Wrap(this Transform tf, GameObject go) => go.transform.SetParent(tf);
        internal static Action<GameObject> Active = go => go.SetActive(true);
        internal static Action<GameObject> Inactive = go => go.SetActive(false);
        internal static Action<GameObject> Configure<T>(Action<T> action) where T : Component => go => go.GetComponent<T>().With(action);
        internal static void Rename(this string value, GameObject go) =>
            go.GetComponentInChildren<TextMeshProUGUI>().SetText(go.name = value);
        internal static void Check(this string value, GameObject go) =>
            UnityEngine.Object.Instantiate(UIRef.Check, go.transform).With(ui =>
            {
                ui.AddComponent<LayoutElement>().preferredWidth = 180;
                ui.GetComponentInChildren<Toggle>().isOn = false;
                ui.GetComponentInChildren<TextMeshProUGUI>().SetText(value);
            });
        internal static void Label(this string value, GameObject go) =>
             UnityEngine.Object.Instantiate(UIRef.Text, go.transform).With(ui =>
            {
                ui.AddComponent<LayoutElement>().preferredWidth = 180;
                ui.GetComponent<TextMeshProUGUI>().SetText(value);
            });
        internal static void Cycle(GameObject go) =>
            UnityEngine.Object.Instantiate(UIRef.Button, go.transform).AddComponent<LayoutElement>().With(ui =>
            {
                ui.preferredWidth = 30;
                ui.preferredWidth = 30;
            });
        internal static void Value(GameObject go) =>
            UnityEngine.Object.Instantiate(UIRef.Text, go.transform).With(ui =>
            {
                ui.AddComponent<LayoutElement>().preferredWidth = 50;
                ui.GetComponent<TextMeshProUGUI>().With(text =>
                {
                    text.SetText("0");
                    text.overflowMode = TextOverflowModes.Ellipsis;
                });
            });
        internal static void Slider(GameObject go) =>
            UnityEngine.Object.Instantiate(UIRef.Slider, go.transform)
                .With(Configure<Slider>(ui =>
                {
                    ui.value = 0;
                    ui.minValue = 0.0f;
                    ui.maxValue = 1.0f;
                }))
                .AddComponent<LayoutElement>().preferredWidth = 80;
        static GameObject Canvas(this Transform parent) =>
            new GameObject(Plugin.Name).With(parent.Wrap).With(go =>
            {
                go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>().With(ui =>
                {
                    ui.referenceResolution = new(1920, 1080);
                    ui.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    ui.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                });
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<ObservableUpdateTrigger>();
            });
        internal static ConfigEntry<float> AnchorX;
        internal static ConfigEntry<float> AnchorY;
        static Action<Unit> UpdateAnchorPosition(RectTransform ui) =>
            _ => (AnchorX.Value, AnchorY.Value) = (ui.anchoredPosition.x, ui.anchoredPosition.y);
        static void Window(this GameObject go)
        {
            go.GetComponentInParent<ObservableUpdateTrigger>().UpdateAsObservable().Subscribe(UpdateAnchorPosition(
            go.GetComponent<RectTransform>().With(ui =>
            {
                ui.anchorMin = new(0.0f, 1.0f);
                ui.anchorMax = new(0.0f, 1.0f);
                ui.pivot = new(0.0f, 1.0f);
                ui.sizeDelta = new(300, 524);
                ui.anchoredPosition = new(AnchorX.Value, AnchorY.Value);
            })));
            go.GetComponent<VerticalLayoutGroup>().With(ui =>
            {
                ui.childControlWidth = true;
                ui.childControlHeight = true;
            });
            go.transform.Find("Title").gameObject.With(title =>
            {
                title.GetComponent<LayoutElement>().With(ui =>
                {
                    ui.preferredWidth = 260;
                    ui.preferredHeight = 44;
                });
                UnityEngine.Object.Destroy(title.transform.Find("Image").gameObject);
                UnityEngine.Object.Destroy(title.transform.Find("btnClose").gameObject);
                UnityEngine.Object.Instantiate(UIRef.Button, title.transform).With(toggle =>
                {
                    toggle.transform.SetSiblingIndex(0);
                    toggle.GetComponent<RectTransform>().With(ui =>
                    {
                        ui.anchorMin = new(0.0f, 0.0f);
                        ui.anchorMax = new(0.0f, 0.0f);
                        ui.offsetMin = new(20.0f, 0.0f);
                        ui.offsetMax = new(50.0f, 30.0f);
                        ui.sizeDelta = new(30.0f, 30.0f);
                    });
                });
            });
            go.transform.Find("Settings").gameObject.With(settings =>
            {
                Enumerable.Range(0, settings.transform.childCount)
                    .Select(settings.transform.GetChild)
                    .Select(tf => tf.gameObject).Do(UnityEngine.Object.Destroy);
                settings.GetComponent<LayoutElement>().preferredHeight = 480;
                settings.AddComponent<VerticalLayoutGroup>().With(ui =>
                {
                    ui.childControlWidth = true;
                    ui.childControlHeight = true;
                    ui.padding = new(20, 20, 10, 10);
                });
            });
            go.AddComponent<UI_DragWindow>();
        }
        internal static GameObject Window(this Transform parent) =>
            UnityEngine.Object.Instantiate(UIRef.Window.gameObject, parent.Canvas().transform).With(Window);
        internal static void FitLayout<T>(this GameObject go) where T : HorizontalOrVerticalLayoutGroup
        {
            go.AddComponent<RectTransform>().localScale = new(1.0f, 1.0f);
            go.AddComponent<LayoutElement>();
            go.AddComponent<T>().With(ui =>
            {
                ui.childControlWidth = true;
                ui.childControlHeight = true;
            });
            go.AddComponent<ContentSizeFitter>().With(ui =>
            {
                ui.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                ui.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            });
        }
    }
    abstract class CommonEdit
    {
        protected static GameObject PrepareArcheType(string name, Transform parent) =>
            new GameObject(name).With(parent.Wrap).With(UI.Inactive)
                .With(UI.FitLayout<HorizontalLayoutGroup>)
                .With(HumanCustom.Instance == null ? name.Check : name.Label)
                .With(UI.Configure<LayoutElement>(ui =>
                {
                    ui.preferredWidth = 260;
                    ui.preferredHeight = 30;
                }));
        protected abstract GameObject Archetype { get; }
        protected GameObject Edit;
        protected CommonEdit(string name, Transform parent) =>
            Edit = UnityEngine.Object.Instantiate(Archetype, parent)
                .With(name.Rename).With(UI.Active);
        bool Check => HumanCustom.Instance == null && Edit.GetComponentInChildren<Toggle>().isOn;
        internal void Update() =>
            Check.Either(OnUpdateGet, OnUpdateSet);
        protected abstract void OnUpdateGet();
        protected abstract void OnUpdateSet();
    }
    class ToggleEdit : CommonEdit
    {
        static GameObject Base { get; set; }
        internal static void PrepareArchetype(GameObject parent) =>
            Base = PrepareArcheType("BoolEdit", parent.transform).With("Enable".Check);
        protected override GameObject Archetype => Base;
        Func<bool> Getter;
        Action<bool> Setter;
        Toggle Value => Edit.GetComponentsInChildren<Toggle>()[^1];
        ToggleEdit(string name, Transform parent, Tuple<Func<bool>, Action<bool>> actions) :
            base(name, parent) => (Getter, Setter) = actions;
        ToggleEdit(ToggleProp prop, Transform parent, Human target) :
            this(prop.ToString(), parent, prop.Transform(target)) => Value.onValueChanged.AddListener(Setter);
        protected override void OnUpdateGet() =>
            Value.SetIsOnWithoutNotify(Getter());
        protected override void OnUpdateSet() =>
            Setter(Value.isOn);
        internal static IEnumerable<CommonEdit> Of(Transform parent, Human target) =>
            Enum.GetValues<ToggleProp>().Select(item => new ToggleEdit(item, parent, target));
    }
    class CycledEdit : CommonEdit
    {
        static GameObject Base { get; set; }
        internal static void PrepareArchetype(GameObject parent) =>
            Base = PrepareArcheType("BoolEdit", parent.transform).With(UI.Cycle).With(UI.Value);
        protected override GameObject Archetype => Base;
        internal Func<int> Getter;
        internal Func<int> Cycler;
        internal Action<int> Setter;
        Button Cycle => Edit.GetComponentInChildren<Button>();
        TextMeshProUGUI Value => Edit.GetComponentsInChildren<TextMeshProUGUI>()[^1];
        void OnCycle() => Value.SetText(Cycler().With(Setter).ToString());
        CycledEdit(string name, Transform parent, Tuple<Func<int>, Func<int>, Action<int>> actions) :
            base(name, parent) => (Getter, Cycler, Setter) = actions;
        CycledEdit(CycledProp prop, Transform parent, Human target) :
            this(prop.ToString(), parent, prop.Transform(target)) => Cycle.onClick.AddListener((Action)OnCycle);
        protected override void OnUpdateGet() =>
            Value.SetText(Getter().ToString());
        protected override void OnUpdateSet() =>
            Setter(int.Parse(Value.text));
        internal static IEnumerable<CommonEdit> Of(Transform parent, Human target) =>
            Enum.GetValues<CycledProp>().Select(item => new CycledEdit(item, parent, target));
    }
    class RangedEdit : CommonEdit
    {
        static GameObject Base { get; set; }
        internal static void PrepareArchetype(GameObject parent) =>
            Base = PrepareArcheType("BoolEdit", parent.transform).With(UI.Slider);
        protected override GameObject Archetype => Base;
        Action<float> Setter;
        Func<float> Getter;
        Slider Slider => Edit.GetComponentInChildren<Slider>();
        RangedEdit(string name, Transform parent, Tuple<Func<float>, Action<float>> actions) :
            base(name, parent) => (Getter, Setter) = actions;
        RangedEdit(RangedProp prop, Transform parent, Human target) :
            this(prop.ToString(), parent, prop.Transform(target)) => Slider.onValueChanged.AddListener(Setter);
        protected override void OnUpdateGet() =>
            Slider.SetValueWithoutNotify(Getter());
        protected override void OnUpdateSet() =>
            Setter(Slider.value);
        internal static IEnumerable<CommonEdit> Of(Transform parent, Human target) =>
            Enum.GetValues<RangedProp>().Select(item => new RangedEdit(item, parent, target));
    }
    class HumanPanel
    {
        Action OnActive;
        GameObject Panel;
        List<CommonEdit> Edits;
        HumanPanel(Transform parent, Human target) =>
            Edits = ToggleEdit.Of(parent, target).Concat(CycledEdit.Of(parent, target)).Concat(RangedEdit.Of(parent, target)).ToList();
        HumanPanel(GameObject panel, Human target) : this(panel.transform, target) =>
            Panel = panel;
        internal HumanPanel(Transform title, Transform settings, Human target) :
            this(new GameObject(target.name).With(settings.Wrap).With(UI.FitLayout<VerticalLayoutGroup>), target) =>
            OnActive = () => title.GetComponentInChildren<TextMeshProUGUI>().SetText(target.fileParam.fullname);
        void Enable() =>
            Panel.With(OnActive).SetActive(true);
        void Disable() =>
            Panel.SetActive(false);
        internal void SetActive(bool value) =>
            value.Either(Disable, Enable);
        internal void Update() =>
            Edits.Do(item => item.Update());
    }
    internal class Window
    {
        static ConfigEntry<KeyboardShortcut> Toggle { get; set; }
        static ConfigEntry<bool> Status { get; set; }
        int CurrentIndex = -1;
        List<HumanPanel> Panels;
        void Cycle() =>
            CurrentIndex = (CurrentIndex + 1) % Panels.Count;
        void CyclePanel() =>
            Enumerable.Range(0, Panels.Count).With(Cycle).Do(index => Panels[index].SetActive(index == CurrentIndex));
        void Update() =>
            Panels.Do(item => item.Update());
        Action<Unit> ToggleActive(GameObject go) =>
            _ => Toggle.With(Update).Value.IsDown().Maybe(() => go.SetActive(Status.Value = !Status.Value));
        Window(Transform title, Transform settings, IEnumerable<Human> targets) =>
            Panels = targets.Select(target => new HumanPanel(title, settings, target)).ToList();
        Window(Transform tf, IEnumerable<Human> target) : this(tf.Find("Title"), tf.Find("Settings"), target) =>
            tf.With(CyclePanel).GetComponentInChildren<Button>().onClick.AddListener((Action)CyclePanel);
        Window(GameObject go, IEnumerable<Human> target) :
            this(go.With(ToggleEdit.PrepareArchetype).With(CycledEdit.PrepareArchetype).With(RangedEdit.PrepareArchetype).transform, target)
        {
            go.With(Status.Value ? UI.Active : UI.Inactive)
                .GetComponentInParent<ObservableUpdateTrigger>()
                .UpdateAsObservable().Subscribe(ToggleActive(go));
        }
        static void DoNothing() { }
        static void OnNextFrame(Action action) =>
            UniTask.NextFrame().ContinueWith(action);
        static Action CheckHuman() =>
            HumanCustom.Instance.Human == null ?
                () => OnNextFrame(CheckHuman()) :
                () => new Window(UIRef.HumanCustomRoot.Window(), [HumanCustom.Instance.Human]);
        internal static void Initialize()
        {
            UI.AnchorX = Plugin.Instance.Config.Bind("General", "Window AnchorX", 1000.0f);
            UI.AnchorY = Plugin.Instance.Config.Bind("General", "Window AnchorY", -400.0f);
            Status = Plugin.Instance.Config.Bind(new ConfigDefinition("General", "Show PelvicFin"), false);
            Toggle = Plugin.Instance.Config.Bind("General", "Toggle PelvicFin GUI", new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl));
            Util.Hook<HumanCustom>(() => OnNextFrame(CheckHuman()), DoNothing);
            Util.Hook<SV.H.HScene>(() => new Window(UIRef.HSceneRoot.Window(), SV.H.HScene.Instance.Actors.Select(actor => actor.Human)), DoNothing);
        }
    }
    [BepInProcess(Process)]
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BasePlugin
    {
        internal static Plugin Instance;
        public const string Process = "SamabakeScramble";
        public const string Name = "PelvicFin";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "1.0.1";
        public override void Load() =>
            (Instance = this).With(Window.Initialize);
    }
}