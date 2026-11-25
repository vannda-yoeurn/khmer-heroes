// ផ្ទាំងអេក្រង់ទាំងអស់ (UI Toolkit) — គាំទ្រអក្សរខ្មែរ (Advanced Text Generator)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace KhmerHeroes
{
    public class UI : MonoBehaviour
    {
        public static UI I;
        public VisualElement root;
        StyleFontDefinition fReg, fBold, fTitle;

        public static readonly Color Gold = new Color(0.95f, 0.76f, 0.35f);
        public static readonly Color Ink = new Color(0.07f, 0.06f, 0.05f, 0.9f);
        public static readonly Color Red = new Color(0.62f, 0.12f, 0.12f);

        VisualElement hud, screen, dlg, modal, modalBox, toastBox, numLayer, fade, bossBox, bossFill, crosshair;
        Label bigL, raceL, mountL, helpL;
        VisualElement heroPanel;
        readonly Dictionary<object, Label> tags = new Dictionary<object, Label>();
        float bigUntil;
        Label heroName, heroHp, skillL, coinsL, zoneL, objL, promptL, bossName, dlgWho, dlgText, dlgHint;
        VisualElement hpFill;
        readonly Label[] partyL = new Label[4]; readonly VisualElement[] partyFill = new VisualElement[4]; readonly VisualElement[] partyRow = new VisualElement[4];
        Line[] dlgLines; int dlgIdx, dlgFrame; Action dlgDone;
        Action<int> quizKey;
        public bool DialogueOpen => dlg.style.display == DisplayStyle.Flex;
        public bool ModalOpen => modal.style.display == DisplayStyle.Flex;
        public bool ScreenOpen => screen.childCount > 0;
        public bool Busy => DialogueOpen || ModalOpen || ScreenOpen;

        struct Num { public Label l; public Vector3 p; public float t; public float life; }
        readonly List<Num> nums = new List<Num>();

        void Awake()
        {
            I = this;
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1600, 900);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            var theme = Resources.Load<ThemeStyleSheet>("UnityDefaultRuntimeTheme");
            if (theme != null) ps.themeStyleSheet = theme;
            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            root = doc.rootVisualElement;
            Fill(root); root.pickingMode = PickingMode.Ignore;
            fReg = LoadFont("Fonts/KantumruyPro-Regular");
            fBold = LoadFont("Fonts/KantumruyPro-Bold");
            fTitle = LoadFont("Fonts/Moul-Regular");
            root.style.unityFontDefinition = fReg;
            Advanced(root);
            root.style.color = Color.white;

            BuildHud();
            numLayer = Layer();
            toastBox = Layer(); toastBox.style.alignItems = Align.Center; toastBox.style.paddingTop = 130;
            promptL = L("", 22, Color.white, true); promptL.style.position = Position.Absolute; promptL.style.bottom = 120; promptL.style.left = 0; promptL.style.right = 0;
            promptL.style.unityTextAlign = TextAnchor.MiddleCenter; Shadow(promptL); root.Add(promptL);
            BuildDialogue();
            screen = Layer(); screen.pickingMode = PickingMode.Ignore;
            modal = Layer(); modal.style.backgroundColor = new Color(0, 0, 0, 0.55f); modal.style.alignItems = Align.Center; modal.style.justifyContent = Justify.Center; modal.pickingMode = PickingMode.Position;
            modal.style.display = DisplayStyle.None;
            fade = Layer(); fade.style.backgroundColor = Color.black; fade.style.opacity = 0;
            hud.style.display = DisplayStyle.None;
        }

        // ---------- អក្សរខ្មែរ ----------
        StyleFontDefinition LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f == null) { Debug.LogWarning("រកពុម្ពអក្សរមិនឃើញ៖ " + path); return new StyleFontDefinition(StyleKeyword.Null); }
            try
            {
                var fa = FontAsset.CreateFontAsset(f);
                if (fa != null) return new StyleFontDefinition(FontDefinition.FromSDFFont(fa));
            }
            catch (Exception e) { Debug.LogWarning("FontAsset: " + e.Message); }
            return new StyleFontDefinition(FontDefinition.FromFont(f));
        }

        /// <summary>បើក Advanced Text Generator ដើម្បីឲ្យជើង និងស្រៈខ្មែរ បង្ហាញត្រឹមត្រូវ</summary>
        public static void Advanced(VisualElement e)
        {
            try
            {
                var p = typeof(IStyle).GetProperty("unityTextGenerator");
                if (p == null) return;
                var enumType = p.PropertyType.GetGenericArguments()[0];
                var val = Enum.Parse(enumType, "Advanced");
                p.SetValue(e.style, Activator.CreateInstance(p.PropertyType, val));
            }
            catch (Exception ex) { Debug.LogWarning("Advanced text: " + ex.Message); }
        }

        // ---------- ជំនួយ ----------
        static void Fill(VisualElement v) { v.style.position = Position.Absolute; v.style.left = 0; v.style.right = 0; v.style.top = 0; v.style.bottom = 0; }
        static void Round(VisualElement v, float r) { v.style.borderTopLeftRadius = v.style.borderTopRightRadius = v.style.borderBottomLeftRadius = v.style.borderBottomRightRadius = r; }
        static void Pad(VisualElement v, float p) { v.style.paddingTop = v.style.paddingBottom = v.style.paddingLeft = v.style.paddingRight = p; }
        static void Border(VisualElement v, float w, Color c)
        {
            v.style.borderTopWidth = v.style.borderBottomWidth = v.style.borderLeftWidth = v.style.borderRightWidth = w;
            v.style.borderTopColor = v.style.borderBottomColor = v.style.borderLeftColor = v.style.borderRightColor = c;
        }
        static void Shadow(VisualElement v) { v.style.textShadow = new TextShadow { offset = new Vector2(1, 1), blurRadius = 3, color = Color.black }; }

        VisualElement Layer()
        {
            var v = new VisualElement(); Fill(v); v.pickingMode = PickingMode.Ignore; root.Add(v); return v;
        }

        public Label L(string text, float size = 20, Color? color = null, bool bold = false, bool title = false)
        {
            var l = new Label(text);
            l.style.fontSize = size;
            l.style.color = color ?? Color.white;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.unityFontDefinition = title ? fTitle : bold ? fBold : fReg;
            Advanced(l);
            l.style.marginBottom = 4;
            l.style.flexShrink = 0;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        public Button B(string text, Action onClick, float size = 22, Color? bg = null)
        {
            var b = new Button(() => { Sfx.I?.Play("pick", 0.4f); onClick?.Invoke(); }) { text = text };
            b.style.unityFontDefinition = fBold; Advanced(b);
            b.style.fontSize = size; b.style.whiteSpace = WhiteSpace.Normal; b.style.color = Color.white;
            var c = bg ?? Red;
            b.style.backgroundColor = c;
            Round(b, 12); Border(b, 2, new Color(Gold.r, Gold.g, Gold.b, 0.8f));
            b.style.paddingTop = b.style.paddingBottom = 10; b.style.paddingLeft = b.style.paddingRight = 22;
            b.style.marginTop = b.style.marginBottom = 6;
            b.RegisterCallback<MouseEnterEvent>(_ => b.style.backgroundColor = Color.Lerp(c, Gold, 0.35f));
            b.RegisterCallback<MouseLeaveEvent>(_ => b.style.backgroundColor = c);
            return b;
        }

        VisualElement Bar(Color c, out VisualElement fill, float w, float h)
        {
            var bg = new VisualElement(); bg.style.width = w; bg.style.height = h; bg.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            Round(bg, h / 2); bg.style.overflow = Overflow.Hidden; Border(bg, 1, new Color(1, 1, 1, 0.25f));
            fill = new VisualElement(); fill.style.height = Length.Percent(100); fill.style.width = Length.Percent(100); fill.style.backgroundColor = c;
            bg.Add(fill); return bg;
        }

        /// <summary>ស៊ុមក្បាច់ខ្មែរ (ឆ្នូតមាសលើ-ក្រោម)</summary>
        VisualElement Panel(float width)
        {
            var p = new VisualElement();
            p.style.width = width; p.style.backgroundColor = Ink; Round(p, 18); Border(p, 3, Gold); Pad(p, 26);
            p.style.alignItems = Align.Center;
            return p;
        }

        VisualElement Ornament(float width)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.Center; row.style.marginTop = 4; row.style.marginBottom = 10;
            int n = Mathf.Max(3, (int)(width / 34));
            for (int i = 0; i < n; i++)
            {
                var d = new VisualElement(); d.style.width = 12; d.style.height = 12; d.style.marginLeft = d.style.marginRight = 5;
                d.style.backgroundColor = i % 2 == 0 ? Gold : Red; d.style.rotate = new Rotate(new Angle(45, AngleUnit.Degree));
                row.Add(d);
            }
            return row;
        }

        // ---------- HUD ----------
        void BuildHud()
        {
            hud = Layer();
            var tl = new VisualElement(); tl.style.position = Position.Absolute; tl.style.left = 20; tl.style.top = 16;
            tl.style.backgroundColor = new Color(0, 0, 0, 0.4f); Pad(tl, 14); Round(tl, 14); Border(tl, 2, new Color(Gold.r, Gold.g, Gold.b, 0.5f));
            heroName = L("", 26, Gold, false, true); tl.Add(heroName);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.Add(Bar(new Color(0.85f, 0.2f, 0.22f), out hpFill, 300, 20)); heroHp = L("", 16); heroHp.style.marginLeft = 10; row.Add(heroHp);
            tl.Add(row);
            skillL = L("", 16, new Color(0.7f, 0.9f, 1f)); skillL.style.marginTop = 6; tl.Add(skillL);
            mountL = L("", 16, new Color(1f, 0.85f, 0.6f)); tl.Add(mountL);
            var sep = new VisualElement(); sep.style.height = 1; sep.style.backgroundColor = new Color(1, 1, 1, 0.2f); sep.style.marginTop = 6; sep.style.marginBottom = 6; tl.Add(sep);
            for (int i = 0; i < 4; i++)
            {
                var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginBottom = 3;
                partyL[i] = L("", 15); partyL[i].style.width = 170; partyL[i].style.marginBottom = 0; r.Add(partyL[i]);
                r.Add(Bar(new Color(0.4f, 0.85f, 0.5f), out partyFill[i], 120, 8));
                partyRow[i] = r; tl.Add(r);
            }
            hud.Add(tl); heroPanel = tl;

            var tr = new VisualElement(); tr.style.position = Position.Absolute; tr.style.right = 20; tr.style.top = 16; tr.style.width = 420;
            tr.style.backgroundColor = new Color(0, 0, 0, 0.4f); Pad(tr, 14); Round(tr, 14); Border(tr, 2, new Color(Gold.r, Gold.g, Gold.b, 0.5f));
            zoneL = L("", 20, Gold, false, true); tr.Add(zoneL);
            objL = L("", 17); tr.Add(objL);
            hud.Add(tr);

            raceL = L("", 22, Color.white, true); raceL.style.position = Position.Absolute; raceL.style.top = 50; raceL.style.left = 0; raceL.style.right = 0;
            raceL.style.unityTextAlign = TextAnchor.MiddleCenter; Shadow(raceL); hud.Add(raceL);
            bigL = L("", 110, Gold, false, true); bigL.style.position = Position.Absolute; bigL.style.top = Length.Percent(28); bigL.style.left = 0; bigL.style.right = 0;
            bigL.style.unityTextAlign = TextAnchor.MiddleCenter; Shadow(bigL); hud.Add(bigL);
            coinsL = L("", 20, Gold, true); coinsL.style.position = Position.Absolute; coinsL.style.top = 18; coinsL.style.left = 0; coinsL.style.right = 0;
            coinsL.style.unityTextAlign = TextAnchor.MiddleCenter; Shadow(coinsL); hud.Add(coinsL);

            bossBox = new VisualElement(); bossBox.style.position = Position.Absolute; bossBox.style.top = 60; bossBox.style.left = Length.Percent(28); bossBox.style.right = Length.Percent(28); bossBox.style.alignItems = Align.Center;
            bossName = L("", 24, new Color(1f, 0.6f, 0.55f), false, true); Shadow(bossName); bossBox.Add(bossName);
            bossBox.Add(Bar(new Color(0.75f, 0.1f, 0.15f), out bossFill, 640, 22));
            bossBox.style.display = DisplayStyle.None; hud.Add(bossBox);

            crosshair = new VisualElement(); crosshair.style.position = Position.Absolute; crosshair.style.left = Length.Percent(50); crosshair.style.top = Length.Percent(45);
            crosshair.style.width = 10; crosshair.style.height = 10; crosshair.style.marginLeft = -5; crosshair.style.marginTop = -5; Round(crosshair, 5);
            crosshair.style.backgroundColor = new Color(1, 1, 1, 0.85f); Border(crosshair, 2, new Color(0, 0, 0, 0.6f)); hud.Add(crosshair);

            var help = L(Data.HelpText, 14, new Color(1, 1, 1, 0.75f)); help.style.position = Position.Absolute; help.style.bottom = 6; help.style.left = 0; help.style.right = 0;
            help.style.unityTextAlign = TextAnchor.MiddleCenter; Shadow(help); hud.Add(help); helpL = help;
        }

        public void ShowHud(bool on) => hud.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        public void Objective(string zone, string obj) { zoneL.text = zone; objL.text = obj; }
        public void Boss(string name, float frac)
        {
            if (name == null) { bossBox.style.display = DisplayStyle.None; return; }
            bossBox.style.display = DisplayStyle.Flex; bossName.text = name; bossFill.style.width = Length.Percent(Mathf.Clamp01(frac) * 100);
        }
        public void Prompt(string t) { if (promptL.text != t) promptL.text = t ?? ""; }
        void LateUpdate()
        {
            promptL.style.display = Busy ? DisplayStyle.None : DisplayStyle.Flex;
            if (bigL != null && bigUntil > 0 && Time.unscaledTime > bigUntil) { bigL.style.display = DisplayStyle.None; bigUntil = 0; }
        }

        public void Toast(string text, float seconds = 2.6f)
        {
            var l = L(text, 22, Gold, true); l.style.backgroundColor = new Color(0, 0, 0, 0.6f); Pad(l, 10); Round(l, 10); l.style.marginBottom = 6;
            toastBox.Add(l);
            while (toastBox.childCount > 3) toastBox.RemoveAt(0);
            StartCoroutine(RemoveLater(l, seconds));
        }
        System.Collections.IEnumerator RemoveLater(VisualElement v, float s) { yield return new WaitForSecondsRealtime(s); v.RemoveFromHierarchy(); }

        public void DamageNumber(Vector3 at, int n)
        {
            var l = L(Kh.Num(n), 24, new Color(1f, 0.9f, 0.5f), true); l.style.position = Position.Absolute; Shadow(l);
            numLayer.Add(l);
            nums.Add(new Num { l = l, p = at + UnityEngine.Random.insideUnitSphere * 0.3f, t = 0, life = 0.9f });
        }

        /// <summary>អក្សរអណ្ដែតលើក្បាល (ឧ. «បុក!» «រអិល!»)</summary>
        public void FloatText(Vector3 at, string text, Color c)
        {
            var l = L(text, 30, c, false, true); l.style.position = Position.Absolute; Shadow(l);
            numLayer.Add(l);
            nums.Add(new Num { l = l, p = at, t = 0, life = 1.4f });
        }

        /// <summary>អក្សរធំកណ្ដាលអេក្រង់ (រាប់ថយក្រោយ)</summary>
        public void Big(string text, float seconds, Color? c = null)
        {
            bigL.text = text; bigL.style.color = c ?? Gold; bigUntil = Time.unscaledTime + seconds;
            bigL.style.display = DisplayStyle.Flex;
        }
        public void RaceHud(string text) { if (raceL.text != (text ?? "")) raceL.text = text ?? ""; }

        public void RaceResults(string[] rows, string note, Action close)
        {
            var b = OpenModal(700);
            b.Add(L("លទ្ធផលការប្រណាំង", 32, Gold, false, true)); b.Add(Ornament(360));
            for (int i = 0; i < rows.Length; i++)
            {
                var l = L(rows[i], 22, i == 0 ? Gold : Color.white, i == 0); l.style.alignSelf = Align.FlexStart; l.style.marginLeft = 60; b.Add(l);
            }
            var n = L(note, 20, new Color(0.8f, 1f, 0.8f)); n.style.marginTop = 12; n.style.unityTextAlign = TextAnchor.MiddleCenter; b.Add(n);
            b.Add(B("អស្ចារ្យ!", () => { CloseModal(); close?.Invoke(); }, 24));
        }

        void Update()
        {
            var cam = Camera.main;
            for (int i = nums.Count - 1; i >= 0; i--)
            {
                var n = nums[i]; n.t += Time.deltaTime; nums[i] = n;
                if (n.t > n.life || cam == null) { n.l.RemoveFromHierarchy(); nums.RemoveAt(i); continue; }
                var wp = n.p + Vector3.up * n.t * 1.2f;
                var vp = cam.WorldToViewportPoint(wp);
                if (vp.z < 0) { n.l.style.display = DisplayStyle.None; continue; }
                var pp = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, wp, cam);
                n.l.style.display = DisplayStyle.Flex;
                n.l.style.left = pp.x - 15; n.l.style.top = pp.y - 15; n.l.style.opacity = 1 - n.t / n.life;
            }

            var p = Player.I;
            if (p != null && hud.style.display == DisplayStyle.Flex)
            {
                var d = p.Def;
                heroName.text = d.name + "  ·  " + d.role;
                hpFill.style.width = Length.Percent(p.hp[p.active] / d.hp * 100);
                heroHp.text = Kh.Num(Mathf.CeilToInt(p.hp[p.active])) + "/" + Kh.Num((int)d.hp);
                float cd = p.SkillCooldownLeft;
                skillL.text = d.skillName + " (Q/កណ្ដុរស្ដាំ)៖ " + (cd > 0 ? Kh.Num(Mathf.CeilToInt(cd)) + " វិនាទី" : "រួចរាល់ ✦");
                for (int i = 0; i < 4; i++)
                {
                    var hd = p.defs[i];
                    bool down = p.hp[i] <= 0;
                    partyL[i].text = Kh.Num(i + 1) + "  " + hd.name + (down ? " (សន្លប់)" : "");
                    partyL[i].style.color = i == p.active ? Gold : down ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
                    partyFill[i].style.width = Length.Percent(p.hp[i] / hd.hp * 100);
                    partyRow[i].style.opacity = i == p.active ? 1f : 0.8f;
                }
                coinsL.text = "កាក់មាស  " + Kh.Num(p.coins);
                mountL.text = p.Riding ? "ជិះ៖ " + p.mount.def.name + "  ·  R ចុះ  ·  Shift ស្ទុះ" : "";
                mountL.style.display = p.Riding ? DisplayStyle.Flex : DisplayStyle.None;
                crosshair.style.display = d.weapon == Weapon.Bow ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // ការសន្ទនា៖ បន្ទាត់បន្ទាប់
            if (DialogueOpen && !ModalOpen && Time.frameCount > dlgFrame + 1 && InputX.Next) NextLine();
            // សំណួរ៖ ចុចលេខ ១-៤
            if (quizKey != null && ModalOpen)
            {
                for (int k = 0; k < 4; k++) if (Input.GetKeyDown(KeyCode.Alpha1 + k) || Input.GetKeyDown(KeyCode.Keypad1 + k)) { var q = quizKey; quizKey = null; q(k); break; }
            }
        }

        // ---------- ការសន្ទនា ----------
        void BuildDialogue()
        {
            dlg = new VisualElement(); dlg.style.position = Position.Absolute; dlg.style.left = Length.Percent(14); dlg.style.right = Length.Percent(14); dlg.style.bottom = 40;
            dlg.style.backgroundColor = Ink; Round(dlg, 16); Border(dlg, 3, Gold); Pad(dlg, 22); dlg.style.minHeight = 170;
            dlg.pickingMode = PickingMode.Position;
            dlgWho = L("", 24, Gold, false, true); dlg.Add(dlgWho);
            dlgText = L("", 23); dlgText.style.marginTop = 4; dlg.Add(dlgText);
            dlgHint = L("ចុច Space / F / ចុចកណ្ដុរ ដើម្បីបន្ត ▸", 14, new Color(1, 1, 1, 0.6f)); dlgHint.style.position = Position.Absolute; dlgHint.style.right = 20; dlgHint.style.bottom = 10; dlg.Add(dlgHint);
            dlg.style.display = DisplayStyle.None;
            root.Add(dlg);
        }

        static Color WhoColor(string who)
        {
            switch (who)
            {
                case "អ្នកនិទាន": return new Color(0.85f, 0.85f, 0.8f);
                case "មេចោរ": return new Color(1f, 0.45f, 0.4f);
                default: return Gold;
            }
        }

        public void Dialogue(Line[] lines, Action done)
        {
            dlgLines = lines; dlgIdx = -1; dlgDone = done; dlgFrame = Time.frameCount;
            dlg.style.display = DisplayStyle.Flex;
            NextLine();
        }

        void NextLine()
        {
            dlgIdx++;
            if (dlgLines == null || dlgIdx >= dlgLines.Length)
            {
                dlg.style.display = DisplayStyle.None;
                var d = dlgDone; dlgDone = null; dlgLines = null;
                d?.Invoke();
                return;
            }
            var ln = dlgLines[dlgIdx];
            string hn = Player.I != null ? Player.I.Def.name : "";
            string who = ln.who.Replace("{hero}", hn);
            dlgWho.text = who; dlgWho.style.color = WhoColor(ln.who);
            dlgText.text = ln.text.Replace("{hero}", hn);
            dlgFrame = Time.frameCount;
        }

        // ---------- ផ្ទាំងកណ្ដាល ----------
        VisualElement OpenModal(float width)
        {
            modal.Clear();
            modalBox = Panel(width);
            modal.Add(modalBox);
            modal.style.display = DisplayStyle.Flex;
            quizKey = null;
            return modalBox;
        }
        public void CloseModal() { modal.Clear(); modal.style.display = DisplayStyle.None; quizKey = null; }

        public void Message(string title, string body, string button, Action after)
        {
            var b = OpenModal(760);
            b.Add(L(title, 30, Gold, false, true)); b.Add(Ornament(400));
            var t = L(body, 21); t.style.unityTextAlign = TextAnchor.MiddleCenter; b.Add(t);
            b.Add(B(button, () => { CloseModal(); after?.Invoke(); }));
        }

        public void Help(Action back)
        {
            var b = OpenModal(860);
            b.Add(L("របៀបលេង", 30, Gold, false, true)); b.Add(Ornament(400));
            string[] rows =
            {
                "WASD ឬ ព្រួញ — ដើរ  ·  កណ្ដុរ — បង្វិលកាមេរ៉ា  ·  កង់កណ្ដុរ — ពង្រីក/បង្រួម",
                "កណ្ដុរឆ្វេង ឬ J — វាយ (ចុចជាប់ៗ ដើម្បីវាយជាប់គ្នា)",
                "កណ្ដុរស្ដាំ ឬ Q — ក្បាច់ពិសេសរបស់វីរជនម្នាក់ៗ",
                "Shift — ក្រឡាប់គេច (មិនរងរបួសពេលកំពុងគេច)  ·  Space — លោត",
                "F — និយាយ ប្រើ រុញថ្ម អុជចន្លុះ ទាញដងទាញ ឬជិះសត្វ",
                "ពេលជិះសត្វ៖ Shift — ស្ទុះលឿន · Space — លោត · R — ចុះ · រត់បុកសត្រូវឲ្យហោះ!",
                "១ ២ ៣ ៤ — ប្ដូរវីរជន។ វីរជនដែលសម្រាក នឹងបានឈាមមកវិញបន្តិចម្ដងៗ",
                "ទឹកដូង — បន្ថែមឈាមឲ្យគ្រប់គ្នា និងជួយអ្នកដែលសន្លប់ឲ្យភ្ញាក់វិញ",
                "ពេលសត្រូវភ្លឺពណ៌លឿង វាហៀបនឹងវាយ — ត្រូវគេច!",
            };
            foreach (var r in rows) { var l = L("•  " + r, 19); l.style.alignSelf = Align.FlexStart; b.Add(l); }
            b.Add(B("យល់ហើយ", () => { CloseModal(); back?.Invoke(); }));
        }

        // ---------- សំណួរ ----------
        public void QuizSession(Quiz[] qs, int need, Action<int> done)
        {
            int idx = 0, ok = 0;
            Action show = null;
            show = () =>
            {
                if (idx >= qs.Length) { CloseModal(); done?.Invoke(ok); return; }
                var q = qs[idx];
                var b = OpenModal(900);
                b.Add(L("សំណួរទី " + Kh.Num(idx + 1) + " / " + Kh.Num(qs.Length) + (need > 0 ? "   (ត្រូវឆ្លើយត្រូវ " + Kh.Num(need) + ")" : ""), 18, new Color(1, 1, 1, 0.7f)));
                b.Add(Ornament(300));
                var ql = L(q.q, 26, Color.white, true); ql.style.unityTextAlign = TextAnchor.MiddleCenter; ql.style.marginBottom = 14; b.Add(ql);
                var grid = new VisualElement(); grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.justifyContent = Justify.Center; grid.style.width = Length.Percent(100);
                Action<int> answer = null;
                var buttons = new List<Button>();
                answer = k =>
                {
                    quizKey = null;
                    bool right = k == q.ok;
                    if (right) ok++;
                    Sfx.I?.Play(right ? "good" : "bad");
                    for (int j = 0; j < buttons.Count; j++)
                    {
                        buttons[j].SetEnabled(false);
                        buttons[j].style.backgroundColor = j == q.ok ? new Color(0.15f, 0.55f, 0.25f) : j == k ? new Color(0.6f, 0.15f, 0.15f) : new Color(0.2f, 0.2f, 0.22f);
                        buttons[j].style.opacity = 1;
                    }
                    var res = L((right ? "✔ ត្រឹមត្រូវ! " : "✘ មិនទាន់ត្រូវទេ។ ") + q.why, 20, right ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.7f, 0.6f));
                    res.style.marginTop = 12; res.style.unityTextAlign = TextAnchor.MiddleCenter; b.Add(res);
                    idx++;
                    b.Add(B(idx >= qs.Length ? "បញ្ចប់" : "សំណួរបន្ទាប់ ▸", () => show()));
                };
                for (int k = 0; k < q.a.Length; k++)
                {
                    int kk = k;
                    var btn = B(Kh.Num(k + 1) + ".  " + q.a[k], () => answer(kk), 21, new Color(0.16f, 0.22f, 0.38f));
                    btn.style.width = Length.Percent(46); btn.style.marginLeft = btn.style.marginRight = 8;
                    buttons.Add(btn); grid.Add(btn);
                }
                b.Add(grid);
                quizKey = k => answer(k);
            };
            show();
        }

        // ---------- អេក្រង់ចាប់ផ្ដើម ----------
        public void Title(Action start, Action quit)
        {
            screen.Clear(); ShowHud(false);
            var s = new VisualElement(); Fill(s); s.style.alignItems = Align.Center; s.style.justifyContent = Justify.Center;
            s.style.backgroundColor = new Color(0, 0, 0, 0.35f); s.pickingMode = PickingMode.Position;
            var t = L(Data.Title, 86, Gold, false, true); t.style.unityTextAlign = TextAnchor.MiddleCenter; Shadow(t); s.Add(t);
            s.Add(Ornament(520));
            var st = L(Data.Subtitle, 34, Color.white, true); Shadow(st); s.Add(st);
            var sub = L("ហ្គេមផ្សងព្រេង · ប្រយុទ្ធ · ដោះប្រស្នា · ចំណេះដឹងទូទៅ · ប្រណាំងសត្វ", 18, new Color(1, 1, 1, 0.8f)); sub.style.marginBottom = 30; Shadow(sub); s.Add(sub);
            var col = new VisualElement(); col.style.width = 360;
            col.Add(B("ចាប់ផ្ដើមដំណើរ", start, 26));
            col.Add(B("កោះឧបសគ្គ · លេងច្រើននាក់", () => Game.I.OpenCourseLobby(), 22, new Color(0.15f, 0.45f, 0.25f)));
            col.Add(B("របៀបលេង", () => Help(null), 22, new Color(0.16f, 0.22f, 0.38f)));
            col.Add(B("ចាកចេញ", quit, 22, new Color(0.25f, 0.25f, 0.28f)));
            s.Add(col);
            var credit = L("វិទ្យាស្ថានទេវសាស្ត្រថេអូខៃរ៉ុស", 15, new Color(1, 1, 1, 0.6f)); credit.style.position = Position.Absolute; credit.style.bottom = 12; s.Add(credit);
            screen.Add(s);
        }

        public void Select(int current, Action<int> pick, Action go)
        {
            screen.Clear(); ShowHud(false);
            var s = new VisualElement(); Fill(s); s.style.alignItems = Align.Center; s.style.justifyContent = Justify.FlexEnd; s.style.paddingBottom = 26;
            s.pickingMode = PickingMode.Ignore;
            var head = L("ជ្រើសរើសវីរជនដំបូង", 34, Gold, false, true); Shadow(head); head.style.position = Position.Absolute; head.style.top = 24; s.Add(head);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.Center;
            for (int i = 0; i < Data.Heroes.Length; i++)
            {
                var h = Data.Heroes[i]; int idx = i;
                var c = new VisualElement(); c.style.width = 330; c.style.marginLeft = c.style.marginRight = 8;
                c.style.backgroundColor = i == current ? new Color(0.35f, 0.08f, 0.08f, 0.92f) : Ink; Round(c, 16);
                Border(c, i == current ? 4 : 2, i == current ? Gold : new Color(1, 1, 1, 0.25f)); Pad(c, 16);
                c.pickingMode = PickingMode.Position;
                c.RegisterCallback<ClickEvent>(_ => { Sfx.I?.Play("pick", 0.4f); pick(idx); });
                c.Add(L(Kh.Num(i + 1) + ". " + h.name, 26, Gold, false, true));
                c.Add(L(h.role + " · អាវុធ៖ " + h.weaponName, 17, new Color(0.8f, 0.9f, 1f), true));
                c.Add(L(h.desc, 15));
                Stat(c, "កម្លាំង", h.statPower, new Color(0.95f, 0.45f, 0.3f));
                Stat(c, "ល្បឿន", h.statSpeed, new Color(0.4f, 0.8f, 1f));
                Stat(c, "ឈាម", h.statLife, new Color(0.4f, 0.9f, 0.5f));
                var sk = L("✦ " + h.skillName + "៖ " + h.skillDesc, 15, Gold); sk.style.marginTop = 6; c.Add(sk);
                row.Add(c);
            }
            s.Add(row);
            var note = L("អ្នកអាចប្ដូរវីរជនបានគ្រប់ពេលក្នុងដំណើរ ដោយចុចលេខ ១ ២ ៣ ៤", 17, new Color(1, 1, 1, 0.85f)); note.style.marginTop = 10; Shadow(note); s.Add(note);
            var b = B("ចេញដំណើរ ▸", go, 26); b.style.width = 320; s.Add(b);
            screen.Add(s);
        }

        void Stat(VisualElement parent, string name, float v, Color c)
        {
            var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 4;
            var l = L(name, 15); l.style.width = 70; l.style.marginBottom = 0; r.Add(l);
            r.Add(Bar(c, out var f, 200, 10)); f.style.width = Length.Percent(v * 100);
            parent.Add(r);
        }

        public void ClearScreen() => screen.Clear();

        public void Pause(Action resume, Action restartCheckpoint, Action toTitle)
        {
            var b = OpenModal(560);
            b.Add(L("ផ្អាកហ្គេម", 32, Gold, false, true)); b.Add(Ornament(300));
            var col = new VisualElement(); col.style.width = 380;
            col.Add(B("លេងបន្ត", () => { CloseModal(); resume(); }));
            col.Add(B("របៀបលេង", () => Help(() => Pause(resume, restartCheckpoint, toTitle)), 20, new Color(0.16f, 0.22f, 0.38f)));
            col.Add(B("ត្រឡប់ទៅចំណុចចុងក្រោយ", () => { CloseModal(); restartCheckpoint(); }, 20, new Color(0.16f, 0.22f, 0.38f)));
            col.Add(B("ទៅម៉ឺនុយដើម", () => { CloseModal(); toTitle(); }, 20, new Color(0.25f, 0.25f, 0.28f)));
            b.Add(col);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 8;
            var sens = L("", 18); sens.style.width = 200; sens.style.unityTextAlign = TextAnchor.MiddleCenter; sens.style.marginBottom = 0;
            System.Action upd = () => sens.text = "ល្បឿនកណ្ដុរ៖ " + CamRig.Sensitivity.ToString("0.0");
            upd();
            var minus = B("−", () => { CamRig.Sensitivity = Mathf.Max(0.4f, CamRig.Sensitivity - 0.3f); upd(); }, 20, new Color(0.25f, 0.25f, 0.28f));
            var plus = B("+", () => { CamRig.Sensitivity = Mathf.Min(6f, CamRig.Sensitivity + 0.3f); upd(); }, 20, new Color(0.25f, 0.25f, 0.28f));
            row.Add(minus); row.Add(sens); row.Add(plus);
            b.Add(row);
        }

        public void GameOver(Action retry, Action toTitle)
        {
            var b = OpenModal(640);
            b.Add(L("វីរជនទាំងអស់សន្លប់", 32, new Color(1f, 0.55f, 0.5f), false, true)); b.Add(Ornament(300));
            b.Add(L("កុំបាក់ទឹកចិត្ត! សម្រាកបន្តិច ហើយព្យាយាមម្ដងទៀត។", 20));
            var col = new VisualElement(); col.style.width = 400;
            col.Add(B("ព្យាយាមម្ដងទៀតពីចំណុចចុងក្រោយ", () => { CloseModal(); retry(); }));
            col.Add(B("ទៅម៉ឺនុយដើម", () => { CloseModal(); toTitle(); }, 20, new Color(0.25f, 0.25f, 0.28f)));
            b.Add(col);
        }

        public void Won(int coins, float minutes, int quizRight, int quizTotal, Action again)
        {
            var b = OpenModal(760);
            b.Add(L("ជ័យជម្នះ!", 44, Gold, false, true)); b.Add(Ornament(400));
            b.Add(L("វីរជនទាំងបួនបានយកស្រូវពូជមកភូមិវិញ។ អ្នកភូមិនឹងបានសាបព្រួសទាន់រដូវវស្សា!", 21));
            b.Add(L("កាក់មាស៖ " + Kh.Num(coins) + "   ·   ពេលវេលា៖ " + Kh.Num(Mathf.RoundToInt(minutes)) + " នាទី   ·   សំណួរត្រូវ៖ " + Kh.Num(quizRight) + "/" + Kh.Num(quizTotal), 19, Gold));
            b.Add(B("លេងម្ដងទៀត", () => { CloseModal(); again(); }, 24));
        }

        public void Fade(float a) => fade.style.opacity = a;

        // ---------------- កោះឧបសគ្គ ----------------
        public void CourseHud(bool on)
        {
            heroPanel.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            helpL.text = on ? "WASD ដើរ · កណ្ដុរ បង្វិលកាមេរ៉ា · Space លោត · J/កណ្ដុរឆ្វេង រុញ (ប្រកួត) · ៥-៨ ពាក្យកំប្លែង · Enter ចាប់ផ្ដើម · Esc ម៉ឺនុយ" : Data.HelpText;
            if (!on) ClearTags();
        }

        public void Tag(object key, string text, Vector3 world, bool show)
        {
            var cam = Camera.main;
            if (!tags.TryGetValue(key, out var l))
            {
                l = L(text, 18, new Color(1f, 0.95f, 0.7f), true); l.style.position = Position.Absolute; Shadow(l);
                l.style.backgroundColor = new Color(0, 0, 0, 0.45f); Pad(l, 4); Round(l, 6);
                numLayer.Add(l); tags[key] = l;
            }
            if (l.text != text) l.text = text;
            bool vis = show && cam != null && root.panel != null && cam.WorldToViewportPoint(world).z > 0;
            l.style.display = vis ? DisplayStyle.Flex : DisplayStyle.None;
            if (!vis) return;
            var pp = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, world, cam);
            l.style.left = pp.x - 40; l.style.top = pp.y - 14;
        }
        public void RemoveTag(object key) { if (tags.TryGetValue(key, out var l)) { l.RemoveFromHierarchy(); tags.Remove(key); } }
        public void ClearTags() { foreach (var l in tags.Values) l.RemoveFromHierarchy(); tags.Clear(); }

        public void Waiting(string text)
        {
            var b = OpenModal(560);
            b.Add(L(text, 26, Gold, true));
            b.Add(L("សូមរង់ចាំបន្តិច...", 18, new Color(1, 1, 1, 0.7f)));
        }

        TextField Field(string value, int max, float width)
        {
            var tf = new TextField { value = value, maxLength = max };
            tf.style.width = width; tf.style.fontSize = 20; tf.style.marginTop = 4; tf.style.marginBottom = 4;
            tf.style.unityFontDefinition = fReg; Advanced(tf);
            var input = tf.Q(className: TextField.inputUssClassName);
            if (input != null)
            {
                input.style.backgroundColor = new Color(1f, 1f, 1f, 0.95f); input.style.color = Color.black;
                input.style.unityFontDefinition = fReg; Advanced(input); input.style.minHeight = 38; Round(input, 8); Pad(input, 6);
                input.style.fontSize = 20;
            }
            return tf;
        }

        /// <summary>ផ្ទាំងរៀបចំកោះឧបសគ្គ៖ ជ្រើសវីរជន ឈ្មោះ របៀបលេង និងការភ្ជាប់</summary>
        public void CourseLobby(int hero, string pname, CourseMode mode, Action<int, string, CourseMode, int, string> go, Action back)
        {
            screen.Clear(); ShowHud(false);
            var s = new VisualElement(); Fill(s); s.style.alignItems = Align.Center; s.style.justifyContent = Justify.Center; s.style.backgroundColor = new Color(0, 0, 0, 0.45f);
            s.pickingMode = PickingMode.Position;
            var p = Panel(1180); s.Add(p);
            p.Add(L("កោះឧបសគ្គ", 44, Gold, false, true));
            p.Add(L("ឆ្លងកាត់ក្បូនអណ្ដែត ដុំឈើយោល ដងបង្វិល ក្ដារស្រុត ស្គរលោត និងផ្លែដូងរមៀល ទៅដល់ពានមាស!", 18, new Color(1, 1, 1, 0.85f)));
            p.Add(Ornament(500));

            // វីរជន
            p.Add(L("១. ជ្រើសវីរជន", 20, Gold, true));
            var hr = new VisualElement(); hr.style.flexDirection = FlexDirection.Row;
            for (int i = 0; i < Data.Heroes.Length; i++)
            {
                int k = i;
                var b = B(Data.Heroes[i].name, () => CourseLobby(k, nameF.value == Data.Heroes[hero].name ? Data.Heroes[k].name : nameF.value, mode, go, back), 20, i == hero ? Red : new Color(0.2f, 0.2f, 0.24f));
                b.style.width = 170; b.style.marginLeft = b.style.marginRight = 6; hr.Add(b);
            }
            p.Add(hr);

            // ឈ្មោះ
            var nr = new VisualElement(); nr.style.flexDirection = FlexDirection.Row; nr.style.alignItems = Align.Center; nr.style.marginTop = 6;
            var nl = L("ឈ្មោះរបស់អ្នក៖", 20); nl.style.marginRight = 10; nr.Add(nl);
            nameF = Field(string.IsNullOrEmpty(pname) ? Data.Heroes[hero].name : pname, 16, 340); nr.Add(nameF);
            p.Add(nr);

            // របៀបលេង (សម្រាប់ម្ចាស់បន្ទប់)
            p.Add(L("២. របៀបលេង", 20, Gold, true));
            var mr = new VisualElement(); mr.style.flexDirection = FlexDirection.Row;
            string[] mn = { "ម្នាក់ឯង", "សាមគ្គី (ជួយគ្នា)", "ប្រកួត (ប្រណាំង)" };
            for (int i = 0; i < 3; i++)
            {
                var m = (CourseMode)i;
                var b = B(mn[i], () => CourseLobby(hero, nameF.value, m, go, back), 19, m == mode ? Red : new Color(0.2f, 0.2f, 0.24f));
                b.style.width = 250; b.style.marginLeft = b.style.marginRight = 6; mr.Add(b);
            }
            p.Add(mr);

            p.Add(L("៣. ចាប់ផ្ដើម", 20, Gold, true));
            var row1 = new VisualElement(); row1.style.flexDirection = FlexDirection.Row; row1.style.alignItems = Align.Center;
            var solo = B("លេងម្នាក់ឯង (គ្មានអ៊ីនធឺណិត)", () => go(hero, nameF.value, CourseMode.Solo, 0, null), 20, new Color(0.15f, 0.45f, 0.25f));
            solo.style.marginRight = 10; row1.Add(solo);
            var hostB = B("បង្កើតបន្ទប់អនឡាញ", () => go(hero, nameF.value, mode == CourseMode.Solo ? CourseMode.Coop : mode, 1, null), 20);
            row1.Add(hostB);
            p.Add(row1);

            var row2 = new VisualElement(); row2.style.flexDirection = FlexDirection.Row; row2.style.alignItems = Align.Center;
            var cl = L("លេខកូដបន្ទប់៖", 19); cl.style.marginRight = 8; row2.Add(cl);
            var codeF = Field("", 12, 220); row2.Add(codeF);
            var joinB = B("ចូលបន្ទប់", () => go(hero, nameF.value, mode, 2, codeF.value), 20, new Color(0.16f, 0.22f, 0.38f)); joinB.style.marginLeft = 10; row2.Add(joinB);
            p.Add(row2);

            var row3 = new VisualElement(); row3.style.flexDirection = FlexDirection.Row; row3.style.alignItems = Align.Center;
            var il = L("Wi-Fi តែមួយ (LAN)៖ IP", 17, new Color(1, 1, 1, 0.75f)); il.style.marginRight = 8; row3.Add(il);
            var ipF = Field("192.168.", 40, 220); row3.Add(ipF);
            var lanJ = B("ចូលតាម IP", () => go(hero, nameF.value, mode, 4, ipF.value), 17, new Color(0.25f, 0.25f, 0.28f)); lanJ.style.marginLeft = 8; row3.Add(lanJ);
            var lanH = B("បង្កើត LAN", () => go(hero, nameF.value, mode == CourseMode.Solo ? CourseMode.Coop : mode, 3, null), 17, new Color(0.25f, 0.25f, 0.28f)); lanH.style.marginLeft = 8; row3.Add(lanH);
            p.Add(row3);

            p.Add(B("ត្រឡប់ក្រោយ", back, 18, new Color(0.25f, 0.25f, 0.28f)));
            screen.Add(s);
        }
        TextField nameF;

        public void CourseResults(string[] rows, string note, bool isHost, Action again, Action leave)
        {
            var b = OpenModal(760);
            b.Add(L("លទ្ធផលកោះឧបសគ្គ", 32, Gold, false, true)); b.Add(Ornament(360));
            for (int i = 0; i < rows.Length; i++) { var l = L(rows[i], 21, i == 0 ? Gold : Color.white, i == 0); l.style.alignSelf = Align.FlexStart; l.style.marginLeft = 40; b.Add(l); }
            var n = L(note, 20, new Color(0.8f, 1f, 0.8f)); n.style.marginTop = 12; n.style.unityTextAlign = TextAnchor.MiddleCenter; b.Add(n);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
            if (isHost) { var a = B("លេងម្ដងទៀត", () => { CloseModal(); again(); }, 22); a.style.marginRight = 10; row.Add(a); }
            else row.Add(L("រង់ចាំម្ចាស់បន្ទប់ចាប់ផ្ដើមម្ដងទៀត...", 18, new Color(1, 1, 1, 0.7f)));
            var lv = B("ចាកចេញ", () => { CloseModal(); leave(); }, 20, new Color(0.25f, 0.25f, 0.28f)); lv.style.marginLeft = 10; row.Add(lv);
            b.Add(row);
        }

        public void CoursePause(Action resume, Action leave)
        {
            var b = OpenModal(560);
            b.Add(L("ផ្អាក", 32, Gold, false, true)); b.Add(Ornament(300));
            b.Add(L("(ពេលលេងអនឡាញ ហ្គេមមិនឈប់ទេ)", 16, new Color(1, 1, 1, 0.6f)));
            var col = new VisualElement(); col.style.width = 380;
            col.Add(B("លេងបន្ត", () => { CloseModal(); resume(); }));
            col.Add(B("ចាកចេញពីកោះ", () => { CloseModal(); leave(); }, 20, new Color(0.25f, 0.25f, 0.28f)));
            b.Add(col);
        }
    }
}
