using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using Character;
using CharacterCreation;
using CoastalSmell;
using UniRx.Triggers;

namespace PelvicFin
{
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
                    value => human.data.Status.visibleSonAlways = value),
                ToggleProp.Sack => new(
                    () => human.body.objBody.ToRenderers()
                        .Where(renderer => "o_dan_f".Equals(renderer.name))
                        .Select(renderer => renderer.enabled).FirstOrDefault(false),
                    value => human.body.objBody.ToRenderers()
                        .Where(renderer => "o_dan_f".Equals(renderer.name))
                        .Do(renderer => renderer.enabled = value)),
                ToggleProp.Condom => new(
                    () => human.data.Status.visibleGomu,
                    value => human.data.Status.visibleGomu = value),
                ToggleProp.EyesHighlight => new(
                    () => !human.data.Status.hideEyesHighlight,
                    value => human.face.HideEyeHighlight(!value)),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
        internal static Tuple<Func<int>, Func<int>, Func<int>, Action<int>> Transform(this CycledProp prop, Human human) =>
            prop switch
            {
                CycledProp.EyebrowsPattern => new(
                    () => human.data.Status.eyebrowPtn,
                    () => (human.data.Status.eyebrowPtn - 1 + human.face.eyebrowCtrl.GetMaxPtn()) % human.face.eyebrowCtrl.GetMaxPtn(),
                    () => (human.data.Status.eyebrowPtn + 1) % human.face.eyebrowCtrl.GetMaxPtn(),
                    (value) => human.face.ChangeEyebrowPtn(value)),
                CycledProp.EyesPattern => new(
                    () => human.data.Status.eyesPtn,
                    () => (human.data.Status.eyesPtn - 1 + human.face.eyesCtrl.GetMaxPtn()) % human.face.eyesCtrl.GetMaxPtn(),
                    () => (human.data.Status.eyesPtn + 1) % human.face.eyesCtrl.GetMaxPtn(),
                    (value) => human.face.ChangeEyesPtn(value)),
                CycledProp.MouthPattern => new(
                    () => human.data.Status.mouthPtn,
                    () => (human.data.Status.mouthPtn - 1 + human.face.mouthCtrl.GetMaxPtn()) % human.face.mouthCtrl.GetMaxPtn(),
                    () => (human.data.Status.mouthPtn + 1) % human.face.mouthCtrl.GetMaxPtn(),
                    (value) => human.face.ChangeMouthPtn(value)),
                CycledProp.Tears => new(
                    () => human.data.Status.tearsLv,
                    () => (human.data.Status.tearsLv - 1 + 4) % 4,
                    () => (human.data.Status.tearsLv + 1) % 4,
                    (value) => human.data.Status.tearsLv = (byte)value),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
        internal static Tuple<Func<float>, Action<float>> Transform(this RangedProp prop, Human human) =>
            prop switch
            {
                RangedProp.EyesOpen => new(
                    () => human.data.Status.eyesOpenMax,
                    value => human.face.ChangeEyesOpenMax(value)),
                RangedProp.MouthOpen => new(
                    () => human.data.Status.mouthOpenMax,
                    value => human.face.ChangeMouthOpenMax(value)),
                RangedProp.NipStand => new(
                    () => human.data.Status.nipStandRate,
                    value => human.body.ChangeNipRate(value)),
                RangedProp.CheekRed => new(
                    () => human.data.Status.hohoAkaRate,
                    value => human.face.ChangeHohoAkaRate(new Il2CppSystem.Nullable<float>(value))),
                RangedProp.AssRed => new(
                    () => human.data.Status.siriAkaRate,
                    value => human.body.ChangeSiriAkaRate(new Il2CppSystem.Nullable<float>(value))),
                RangedProp.Sweat => new(
                    () => human.data.Status.sweatRate,
                    value => human.ChangeSweat(value)),
                RangedProp.Wet => new(
                    () => human.data.Status.wetRate,
                    value => human.ChangeWet(value)),
                _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
            };
    }
    abstract class CommonEdit
    {
        static Func<string, Transform, GameObject> PrepareArchetypeForCustom =
            (name, parent) => new GameObject(name).With(UGUI.Go(parent: parent, active: false))
                .With(UGUI.Cmp(UGUI.LayoutGroup<HorizontalLayoutGroup>(spacing: 5, padding: new(0, 0, 2, 2))))
                .With(UGUI.Cmp(UGUI.Layout(width: 240, height: 28)))
                .With(UGUI.Label.Apply(140).Apply(24).Apply("Name"));
        static Func<string, Transform, GameObject> PrepareArchetypeForHScene =
            (name, parent) => new GameObject(name).With(UGUI.Go(parent: parent, active: false))
                .With(UGUI.Cmp(UGUI.LayoutGroup<HorizontalLayoutGroup>(spacing: 2)))
                .With(UGUI.Cmp(UGUI.Layout(240)))
                .With(UGUI.Check.Apply(24).Apply(24).Apply("Check"))
                .With(UGUI.Label.Apply(106).Apply(24).Apply("Name"));
        protected static Func<string, Transform, GameObject> PrepareArchetype;
        static Func<string, Transform, GameObject, CommonEdit, GameObject> PrepareEditForCustom =
            (name, parent, archetype, instance) =>
                UnityEngine.Object.Instantiate(archetype, parent)
                    .With(UGUI.Go(name: name, active: true))
                    .With(UGUI.ModifyAt("Name")(UGUI.Cmp(UGUI.Text(text: name))));
        static Func<string, Transform, GameObject, CommonEdit, GameObject> PrepareEditForHScene =
            (name, parent, archetype, instance) =>
                UnityEngine.Object.Instantiate(archetype, parent)
                    .With(UGUI.Go(name: name, active: true))
                    .With(UGUI.ModifyAt("Name")(UGUI.Cmp(UGUI.Text(text: name))))
                    .With(UGUI.ModifyAt("Background.Check", "Check")
                        (UGUI.Cmp<Toggle>(ui => instance.Check = () => ui.isOn)));
        internal static void PrepareCustom() =>
            (PrepareArchetype, PrepareEdit) = (PrepareArchetypeForCustom, PrepareEditForCustom);
        internal static void PrepareHScene() =>
            (PrepareArchetype, PrepareEdit) = (PrepareArchetypeForHScene, PrepareEditForHScene);
        static Func<string, Transform, GameObject, CommonEdit, GameObject> PrepareEdit;
        protected GameObject Edit;
        Func<bool> Check = () => false;
        protected CommonEdit(string name, Transform parent, GameObject archetype) =>
            Edit = PrepareEdit(name, parent, archetype, this);
        internal void Update() =>
            Check().Either(OnUpdateGet, OnUpdateSet);
        protected abstract void OnUpdateGet();
        protected abstract void OnUpdateSet();
    }
    class ToggleEdit : CommonEdit
    {
        internal static void Prepare(GameObject parent) =>
            Archetype = PrepareArchetype("BoolEdit", parent.transform)
                .With(UGUI.Check.Apply(24).Apply(24).Apply("Value"))
                .With(UGUI.Label.Apply(56).Apply(24).Apply("Enable"));
        static GameObject Archetype { get; set; }
        Func<bool> Getter;
        Action<bool> Setter;
        Toggle Value;
        ToggleEdit(string name, Transform parent, Tuple<Func<bool>, Action<bool>> actions) :
            base(name, parent, Archetype) => (Getter, Setter) = actions;
        ToggleEdit(ToggleProp prop, Transform parent, Human target) :
            this(prop.ToString(), parent, prop.Transform(target)) => Edit
                .With(UGUI.ModifyAt("Background.Value", "Value")(UGUI.Cmp<Toggle>(ui =>
                    (Value = ui).With(OnUpdateGet).OnValueChangedAsObservable().Subscribe(Setter))));
        protected override void OnUpdateGet() =>
            Value.SetIsOnWithoutNotify(Getter());
        protected override void OnUpdateSet() =>
            Setter(Value.isOn);
        internal static IEnumerable<CommonEdit> Of(Transform parent, Human target) =>
            Enum.GetValues<ToggleProp>().Select(item => new ToggleEdit(item, parent, target));
    }
    class CycledEdit : CommonEdit
    {
        internal static void Prepare(GameObject parent) =>
            Archetype = PrepareArchetype("BoolEdit", parent.transform)
                .With(UGUI.Button.Apply(28).Apply(24).Apply("Prev"))
                .With(UGUI.Label.Apply(29).Apply(24).Apply("Value"))
                .With(UGUI.Button.Apply(28).Apply(24).Apply("Next"));
        static GameObject Archetype { get; set; }
        internal Func<int> Getter;
        internal Func<int> Prev;
        internal Func<int> Next;
        internal Action<int> Setter;
        TextMeshProUGUI Value;
        CycledEdit(string name, Transform parent, Tuple<Func<int>, Func<int>, Func<int>, Action<int>> actions) :
            base(name, parent, Archetype) => (Getter, Prev, Next, Setter) = actions;
        CycledEdit(CycledProp prop, Transform parent, Human target) :
            this(prop.ToString(), parent, prop.Transform(target)) => Edit
                .With(UGUI.ModifyAt("Prev")(UGUI.Cmp<Button>(ui =>
                    ui.OnClickAsObservable().Subscribe((F.Compose(Prev, Setter) + OnUpdateGet).Ignoring<Unit>()))))
                .With(UGUI.ModifyAt("Next")(UGUI.Cmp<Button>(ui =>
                    ui.OnClickAsObservable().Subscribe((F.Compose(Next, Setter) + OnUpdateGet).Ignoring<Unit>()))))
                .With(UGUI.ModifyAt("Prev", "Prev.Label")(UGUI.Cmp(UGUI.Text(text: "<)"))))
                .With(UGUI.ModifyAt("Next", "Next.Label")(UGUI.Cmp(UGUI.Text(text: "(>"))))
                .With(UGUI.ModifyAt("Value")(UGUI.Cmp<TextMeshProUGUI>(ui =>
                    (Value = ui).With(OnUpdateGet).horizontalAlignment = HorizontalAlignmentOptions.Right)));
        protected override void OnUpdateGet() =>
            Value.SetText(Getter().ToString());
        protected override void OnUpdateSet() =>
            Setter(int.Parse(Value.text));
        internal static IEnumerable<CommonEdit> Of(Transform parent, Human target) =>
            Enum.GetValues<CycledProp>().Select(item => new CycledEdit(item, parent, target));
    }
    class RangedEdit : CommonEdit
    {
        internal static void Prepare(GameObject parent) =>
            Archetype = PrepareArchetype("BoolEdit", parent.transform)
                .With(UGUI.Slider.Apply(95).Apply(24).Apply("Range"));
        static GameObject Archetype { get; set; }
        Action<float> Setter;
        Func<float> Getter;
        Slider Slider => Edit.GetComponentInChildren<Slider>();
        RangedEdit(string name, Transform parent, Tuple<Func<float>, Action<float>> actions) :
            base(name, parent, Archetype) => (Getter, Setter) = actions;
        RangedEdit(RangedProp prop, Transform parent, Human target) :
            this(prop.ToString(), parent, prop.Transform(target)) => Edit
                .With(UGUI.ModifyAt("Range")(UGUI.Cmp<Slider>(ui =>
                    ui.With(OnUpdateGet).OnValueChangedAsObservable().Subscribe(Setter))));
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
        internal HumanPanel(WindowHandle handle, GameObject parent, Human target) :
            this(UGUI.Panel(260, 500, target.name, parent)
                .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>(spacing: 5, padding: new(10, 10, 5, 5)))), target) =>
            OnActive = () => handle.Title.SetText(target.fileParam.fullname, false);
        void Enable() =>
            Panel.With(OnActive).SetActive(true);
        void Disable() =>
            Panel.SetActive(false);
        internal void SetActive(bool value) =>
            value.Either(Disable, Enable);
        internal void Update() =>
            Edits.ForEach(item => item.Update());
    }
    internal class Window
    {
        static WindowHandle Handle;
        List<HumanPanel> Panels;
        internal Action<bool> Toggle(int index) =>
            index < Panels.Count ? Panels[index].SetActive : F.DoNothing.Ignoring<bool>();
        Window(GameObject window, IEnumerable<Human> humans) =>
            (_, Panels) = (UGUI.Panel(260, 24, "Selection", window)
                .With(UGUI.Go(active: true))
                .With(UGUI.Cmp(UGUI.LayoutGroup<HorizontalLayoutGroup>(padding: new(10, 10, 0, 0))))
                .With(UGUI.Cmp(UGUI.ToggleGroup()))
                .With(UGUI.Toggle.Apply(80).Apply(24).Apply("1st"))
                .With(UGUI.Toggle.Apply(80).Apply(24).Apply("2nd"))
                .With(UGUI.Toggle.Apply(80).Apply(24).Apply("3rd")),
                humans.Select(target => new HumanPanel(Handle, window, target)).ToList());
        Window(IEnumerable<Human> humans, GameObject window) : this(window, humans) => window
            .With(UGUI.ModifyAt("Selection", "1st")(
                UGUI.Cmp<Toggle, ToggleGroup>((ui, group) => ui.group = group) +
                UGUI.Cmp<Toggle>(ui => ui.OnValueChangedAsObservable().Subscribe(Toggle(0))) +
                UGUI.Cmp(UGUI.Interactable<Toggle>(Panels.Count() > 0))))
            .With(UGUI.ModifyAt("Selection", "2nd")(
                UGUI.Cmp<Toggle>(ui => ui.OnValueChangedAsObservable().Subscribe(Toggle(1))) +
                UGUI.Cmp<Toggle, ToggleGroup>((ui, group) => ui.group = group) +
                UGUI.Cmp(UGUI.Interactable<Toggle>(Panels.Count() > 1))))
            .With(UGUI.ModifyAt("Selection", "3rd")(
                UGUI.Cmp<Toggle>(ui => ui.OnValueChangedAsObservable().Subscribe(Toggle(2))) +
                UGUI.Cmp<Toggle, ToggleGroup>((ui, group) => ui.group = group) +
                UGUI.Cmp(UGUI.Interactable<Toggle>(Panels.Count() > 2))))
            .GetComponentInParent<ObservableUpdateTrigger>()
                .UpdateAsObservable().Subscribe(F.Ignoring<Unit>(Update));
        void Update() => Panels.ForEach(Update);
        void Update(HumanPanel panel) => panel.Update();
        static GameObject Create =>
            UGUI.Window(260, 500, Plugin.Name, Handle)
                .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>(spacing: 6)));
        static void PrepareCustom() =>
            new Window([HumanCustom.Instance.Human],
                Create
                    .With(CommonEdit.PrepareCustom)
                    .With(ToggleEdit.Prepare)
                    .With(CycledEdit.Prepare)
                    .With(RangedEdit.Prepare));
        static void PrepareHScene() =>
            new Window(SV.H.HScene.Instance.Actors.Select(actor => actor.Human),
                Create
                    .With(CommonEdit.PrepareHScene)
                    .With(ToggleEdit.Prepare)
                    .With(CycledEdit.Prepare)
                    .With(RangedEdit.Prepare));
        internal static void Initialize()
        {
            Handle = new WindowHandle(Plugin.Instance, "PelvicFin", new(1000, -400), new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl));
            Util<HumanCustom>.Hook(Util.OnCustomHumanReady.Apply(PrepareCustom), F.DoNothing);
            Util<SV.H.HScene>.Hook(PrepareHScene, F.DoNothing);
        }
    }
    [BepInProcess(Process)]
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(CoastalSmell.Plugin.Guid)]
    public class Plugin : BasePlugin
    {
        internal static Plugin Instance;
        public const string Process = "SamabakeScramble";
        public const string Name = "PelvicFin";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "1.0.2";
        public override void Load() =>
            (Instance = this).With(Window.Initialize);
    }
}