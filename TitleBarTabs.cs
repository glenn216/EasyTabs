using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using HOOKPROC = Windows.Win32.UI.WindowsAndMessaging.HOOKPROC;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace EasyTabs
{
    internal enum WM : int
    {
        WM_ACTIVATE = 0x0006,
        WM_NCHITTEST = 0x0084,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONUP = 0x0205,
        WM_MBUTTONUP = 0x0208,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCMBUTTONUP = 0x00A8,
        WM_SYSCOMMAND = 0x0112
    }

    public enum HT : int
    {
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTCAPTION = 2,
        HTSYSMENU = 3,
        HTGROWBOX = 4,
        HTMENU = 5,
        HTMINBUTTON = 8,
        HTMAXBUTTON = 9,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTCLOSE = 20
    }

    internal enum WH : int
    {
        WH_MOUSE_LL = 14
    }

    [Flags]
    internal enum WS_EX : int
    {
        WS_EX_LAYERED = 0x00080000,
        WS_EX_NOACTIVATE = 0x08000000
    }

    internal enum ULW : int
    {
        ULW_COLORKEY = 0x00000001,
        ULW_ALPHA = 0x00000002,
        ULW_OPAQUE = 0x00000004
    }

    internal enum AC : int
    {
        AC_SRC_OVER = 0x00,
        AC_SRC_ALPHA = 0x01
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WTA_OPTIONS
    {
        public uint dwFlags;
        public uint dwMask;
    }

    public enum WINDOWTHEMEATTRIBUTETYPE : uint
    {
        WTA_NONCLIENT = 1
    }

    internal static class WTNCA
    {
        public const uint NODRAWCAPTION = 0x00000001;
        public const uint NODRAWICON = 0x00000002;
        public const uint VALIDBITS = NODRAWCAPTION | NODRAWICON;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SIZE
    {
        public int cx;
        public int cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [SupportedOSPlatform("windows10.0.19041")]
    internal static class User32
    {
        public static bool GetWindowRect(IntPtr hWnd, out RECT lpRect)
        {
            bool result = Windows.Win32.PInvoke.GetWindowRect(new HWND(hWnd), out Windows.Win32.Foundation.RECT nativeRect);
            lpRect = new RECT
            {
                left = nativeRect.left,
                top = nativeRect.top,
                right = nativeRect.right,
                bottom = nativeRect.bottom
            };
            return result;
        }

        public static unsafe IntPtr SetWindowsHookEx(WH idHook, HOOKPROC lpfn, IntPtr hMod, uint dwThreadId)
            => (IntPtr)Windows.Win32.PInvoke.SetWindowsHookEx((WINDOWS_HOOK_ID)idHook, lpfn, new HINSTANCE(hMod), dwThreadId).Value;

        public static bool UnhookWindowsHookEx(IntPtr hhk)
            => Windows.Win32.PInvoke.UnhookWindowsHookEx(new HHOOK(hhk));

        public static LRESULT CallNextHookEx(IntPtr hhk, int nCode, WPARAM wParam, LPARAM lParam)
            => Windows.Win32.PInvoke.CallNextHookEx(new HHOOK(hhk), nCode, wParam, lParam);

        public static uint GetDoubleClickTime() => Windows.Win32.PInvoke.GetDoubleClickTime();

        public static unsafe bool UpdateLayeredWindow(
            IntPtr hwnd,
            IntPtr hdcDst,
            ref POINT pptDst,
            ref SIZE psize,
            IntPtr hdcSrc,
            ref POINT pprSrc,
            int crKey,
            ref BLENDFUNCTION pblend,
            ULW dwFlags)
        {
            Point ptDst = new Point(pptDst.x, pptDst.y);
            Point ptSrc = new Point(pprSrc.x, pprSrc.y);
            Windows.Win32.Foundation.SIZE nativeSize = new Windows.Win32.Foundation.SIZE(psize.cx, psize.cy);

            Windows.Win32.Graphics.Gdi.BLENDFUNCTION nativeBlend = new Windows.Win32.Graphics.Gdi.BLENDFUNCTION
            {
                BlendOp = pblend.BlendOp,
                BlendFlags = pblend.BlendFlags,
                SourceConstantAlpha = pblend.SourceConstantAlpha,
                AlphaFormat = pblend.AlphaFormat
            };

            return Windows.Win32.PInvoke.UpdateLayeredWindow(
                new HWND(hwnd),
                new HDC(hdcDst),
                ptDst,
                nativeSize,
                new HDC(hdcSrc),
                ptSrc,
                (COLORREF)(uint)crKey,
                nativeBlend,
                (UPDATE_LAYERED_WINDOW_FLAGS)(uint)dwFlags);
        }

        public static unsafe IntPtr GetDC(IntPtr hWnd) => (IntPtr)Windows.Win32.PInvoke.GetDC(new HWND(hWnd)).Value;

        public static unsafe IntPtr GetWindowDC(IntPtr hWnd) => (IntPtr)Windows.Win32.PInvoke.GetWindowDC(new HWND(hWnd)).Value;

        public static int ReleaseDC(IntPtr hWnd, IntPtr hDC) => Windows.Win32.PInvoke.ReleaseDC(new HWND(hWnd), new HDC(hDC));

        public static int GetSystemMetrics(int nIndex) => Windows.Win32.PInvoke.GetSystemMetrics((SYSTEM_METRICS_INDEX)nIndex);

        public static bool ShowWindow(IntPtr hWnd, int nCmdShow) => Windows.Win32.PInvoke.ShowWindow(new HWND(hWnd), (SHOW_WINDOW_CMD)nCmdShow);
    }

    internal static class Kernel32
    {
        public static IntPtr GetModuleHandle(string lpModuleName)
            => Windows.Win32.PInvoke.GetModuleHandle(lpModuleName).DangerousGetHandle();
    }

    internal static class Gdi32
    {
        public static unsafe IntPtr CreateCompatibleDC(IntPtr hdc) => (IntPtr)Windows.Win32.PInvoke.CreateCompatibleDC(new HDC(hdc)).Value;

        public static bool DeleteDC(IntPtr hdc) => Windows.Win32.PInvoke.DeleteDC(new HDC(hdc));

        public static unsafe IntPtr SelectObject(IntPtr hdc, IntPtr hObject) => (IntPtr)Windows.Win32.PInvoke.SelectObject(new HDC(hdc), new HGDIOBJ(hObject)).Value;

        public static bool DeleteObject(IntPtr hObject) => Windows.Win32.PInvoke.DeleteObject(new HGDIOBJ(hObject));
    }

    internal static class Dwmapi
    {
        public static int DwmIsCompositionEnabled(out bool pfEnabled)
        {
            int hr = Windows.Win32.PInvoke.DwmIsCompositionEnabled(out BOOL enabled);
            pfEnabled = enabled;
            return hr;
        }

        public static int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset)
        {
            Windows.Win32.UI.Controls.MARGINS nativeMargins = new Windows.Win32.UI.Controls.MARGINS
            {
                cxLeftWidth = pMarInset.cxLeftWidth,
                cxRightWidth = pMarInset.cxRightWidth,
                cyTopHeight = pMarInset.cyTopHeight,
                cyBottomHeight = pMarInset.cyBottomHeight
            };

            return Windows.Win32.PInvoke.DwmExtendFrameIntoClientArea(new HWND(hWnd), in nativeMargins);
        }
    }

    internal static class Uxtheme
    {
        public static unsafe int SetWindowThemeAttribute(
            IntPtr hwnd,
            WINDOWTHEMEATTRIBUTETYPE type,
            ref WTA_OPTIONS options,
            uint size)
        {
            fixed (WTA_OPTIONS* pOptions = &options)
            {
                return Windows.Win32.PInvoke.SetWindowThemeAttribute(
                    new HWND(hwnd),
                    (Windows.Win32.UI.Controls.WINDOWTHEMEATTRIBUTETYPE)type,
                    pOptions,
                    size);
            }
        }
    }

    /// <summary>
    /// Base class that contains the functionality to render tabs within a WinForms application's title bar area. This is done through a borderless overlay
    /// window (<see cref="_overlay" />) rendered on top of the non-client area at the top of this window. All an implementing class will need to do is set
    /// the <see cref="TabRenderer" /> property and begin adding tabs to <see cref="Tabs" />.
    /// </summary>
    public abstract partial class TitleBarTabs : Form
    {
        public delegate void TitleBarTabCancelEventHandler(object sender, TitleBarTabCancelEventArgs e);

        public delegate void TitleBarTabEventHandler(object sender, TitleBarTabEventArgs e);

        protected bool _aeroPeekEnabled = true;
        protected int _nonClientAreaHeight;
        protected internal TitleBarTabsOverlay _overlay;
        protected Dictionary<Form, Bitmap> _previews = new Dictionary<Form, Bitmap>();
        protected TitleBarTab _previousActiveTab = null;
        protected FormWindowState? _previousWindowState;
        protected BaseTabRenderer _tabRenderer;
        protected ListWithEvents<TitleBarTab> _tabs = new ListWithEvents<TitleBarTab>();

        protected TitleBarTabs()
        {
            FormClosing += ApplicationFormClosing;

            _previousWindowState = null;
            ExitOnLastTabClose = true;
            InitializeComponent();
            SetWindowThemeAttributes(WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON);

            _tabs.CollectionModified += _tabs_CollectionModified;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            Tooltip = new ToolTip
            {
                AutoPopDelay = 5000,
                AutomaticDelay = 500
            };

            ShowTooltips = true;
        }

        internal bool IsCompositionEnabled
        {
            get
            {
                bool hasComposition;
                Dwmapi.DwmIsCompositionEnabled(out hasComposition);
                return hasComposition;
            }
        }

        public bool AeroPeekEnabled
        {
            get => _aeroPeekEnabled;
            set
            {
                _aeroPeekEnabled = value;

                if (!_aeroPeekEnabled)
                {
                    foreach (TitleBarTab tab in Tabs)
                    {
                        TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tab.Content);
                    }

                    _previews.Clear();
                }
                else
                {
                    foreach (TitleBarTab tab in Tabs)
                    {
                        CreateThumbnailPreview(tab);
                    }

                    if (SelectedTab != null)
                    {
                        TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(SelectedTab.Content);
                    }
                }
            }
        }

        public bool ShowTooltips { get; set; }

        public ToolTip Tooltip { get; set; }

        public ListWithEvents<TitleBarTab> Tabs => _tabs;

        public BaseTabRenderer TabRenderer
        {
            get => _tabRenderer;
            set
            {
                _tabRenderer = value;
                SetFrameSize();
            }
        }

        public TitleBarTab SelectedTab
        {
            get => Tabs.FirstOrDefault(t => t.Active);
            set => SelectedTabIndex = Tabs.IndexOf(value);
        }

        public int SelectedTabIndex
        {
            get => Tabs.FindIndex(t => t.Active);
            set
            {
                TitleBarTab selectedTab = SelectedTab;
                int selectedTabIndex = SelectedTabIndex;

                if (selectedTab != null && selectedTabIndex != value)
                {
                    TitleBarTabCancelEventArgs deselectingArgs = new TitleBarTabCancelEventArgs
                    {
                        Action = TabControlAction.Deselecting,
                        Tab = selectedTab,
                        TabIndex = selectedTabIndex
                    };

                    OnTabDeselecting(deselectingArgs);

                    if (deselectingArgs.Cancel)
                    {
                        return;
                    }

                    selectedTab.Active = false;

                    OnTabDeselected(
                        new TitleBarTabEventArgs
                        {
                            Tab = selectedTab,
                            TabIndex = selectedTabIndex,
                            Action = TabControlAction.Deselected
                        });
                }

                if (value != -1)
                {
                    TitleBarTabCancelEventArgs selectingArgs = new TitleBarTabCancelEventArgs
                    {
                        Action = TabControlAction.Selecting,
                        Tab = Tabs[value],
                        TabIndex = value
                    };

                    OnTabSelecting(selectingArgs);

                    if (selectingArgs.Cancel)
                    {
                        return;
                    }

                    Tabs[value].Active = true;

                    OnTabSelected(
                        new TitleBarTabEventArgs
                        {
                            Tab = Tabs[value],
                            TabIndex = value,
                            Action = TabControlAction.Selected
                        });
                }

                _overlay?.Render();
            }
        }

        public bool ExitOnLastTabClose { get; set; }

        public bool IsClosing { get; set; }

        public TitleBarTabsApplicationContext ApplicationContext { get; internal set; }

        public int NonClientAreaHeight => _nonClientAreaHeight;

        public Rectangle TabDropArea => _overlay.TabDropArea;

        private void SetWindowThemeAttributes(uint attributes)
        {
            WTA_OPTIONS options = new WTA_OPTIONS
            {
                dwFlags = attributes,
                dwMask = WTNCA.VALIDBITS
            };

            Uxtheme.SetWindowThemeAttribute(Handle, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref options, (uint)Marshal.SizeOf(typeof(WTA_OPTIONS)));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _overlay = TitleBarTabsOverlay.GetInstance(this);

            if (TabRenderer != null)
            {
                _overlay.MouseMove += TabRenderer.Overlay_MouseMove;
                _overlay.MouseUp += TabRenderer.Overlay_MouseUp;
                _overlay.MouseDown += TabRenderer.Overlay_MouseDown;
            }
        }

        protected void SetFrameSize()
        {
            if (TabRenderer == null || WindowState == FormWindowState.Minimized)
            {
                return;
            }

            int topPadding;

            if (WindowState == FormWindowState.Maximized || TabRenderer.RendersEntireTitleBar)
            {
                topPadding = TabRenderer.TabHeight - TabRenderer.TopPadding - SystemInformation.CaptionHeight;
            }
            else
            {
                topPadding = TabRenderer.TabHeight - SystemInformation.CaptionHeight;
            }

            if (!TabRenderer.IsWindows10 && WindowState == FormWindowState.Maximized)
            {
                topPadding += 1;
            }

            Padding = new Padding(Padding.Left, topPadding > 0 ? topPadding : 0, Padding.Right, Padding.Bottom);

            if (!TabRenderer.IsWindows10)
            {
                MARGINS margins = new MARGINS
                {
                    cxLeftWidth = 1,
                    cxRightWidth = 1,
                    cyBottomHeight = 1,
                    cyTopHeight = topPadding > 0 ? topPadding : 0
                };

                Dwmapi.DwmExtendFrameIntoClientArea(Handle, ref margins);
            }

            _nonClientAreaHeight = SystemInformation.CaptionHeight + (topPadding > 0 ? topPadding : 0);

            if (AeroPeekEnabled)
            {
                foreach (TabbedThumbnail preview in Tabs.Select(tab => TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content)).Where(preview => preview != null))
                {
                    _ = preview;
                }
            }
        }

        public event TitleBarTabCancelEventHandler TabDeselecting;

        public event TitleBarTabEventHandler TabDeselected;

        public event TitleBarTabCancelEventHandler TabSelecting;

        public event TitleBarTabEventHandler TabSelected;

        public event TitleBarTabEventHandler TabClicked;

        public abstract TitleBarTab CreateTab();

        protected internal void OnTabClicked(TitleBarTabEventArgs e)
        {
            TabClicked?.Invoke(this, e);
        }

        protected void OnTabDeselecting(TitleBarTabCancelEventArgs e)
        {
            if (_previousActiveTab != null && AeroPeekEnabled)
            {
                UpdateTabThumbnail(_previousActiveTab);
            }

            TabDeselecting?.Invoke(this, e);
        }

        protected void UpdateTabThumbnail(TitleBarTab tab)
        {
            TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content);

            if (preview == null)
            {
                return;
            }

            Bitmap bitmap = tab.GetImage();
            preview.SetImage(bitmap);

            if (_previews.ContainsKey(tab.Content) && _previews[tab.Content] != null)
            {
                _previews[tab.Content].Dispose();
            }

            _previews[tab.Content] = bitmap;
        }

        protected void OnTabDeselected(TitleBarTabEventArgs e)
        {
            TabDeselected?.Invoke(this, e);
        }

        protected void OnTabSelecting(TitleBarTabCancelEventArgs e)
        {
            ResizeTabContents(e.Tab);
            TabSelecting?.Invoke(this, e);
        }

        protected void OnTabSelected(TitleBarTabEventArgs e)
        {
            if (SelectedTabIndex != -1 && _previews.ContainsKey(SelectedTab.Content) && AeroPeekEnabled)
            {
                TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(SelectedTab.Content);
            }

            _previousActiveTab = SelectedTab;
            TabSelected?.Invoke(this, e);
        }

        private void preview_TabbedThumbnailBitmapRequested(object sender, TabbedThumbnailBitmapRequestedEventArgs e)
        {
            foreach (TitleBarTab tab in Tabs.Where(tab => tab.Content.Handle == e.WindowHandle && _previews.ContainsKey(tab.Content)))
            {
                TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content);
                preview.SetImage(_previews[tab.Content]);
                break;
            }
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            ResizeTabContents();
        }

        public void ResizeTabContents(TitleBarTab tab = null)
        {
            tab ??= SelectedTab;

            if (tab != null)
            {
                tab.Content.Location = new Point(0, Padding.Top - 1);
                tab.Content.Size = new Size(ClientRectangle.Width, ClientRectangle.Height - Padding.Top + 1);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        internal void ForwardMessage(ref Message m)
        {
            m.HWnd = Handle;
            WndProc(ref m);
        }

        private void preview_TabbedThumbnailActivated(object sender, TabbedThumbnailEventArgs e)
        {
            foreach (TitleBarTab tab in Tabs.Where(tab => tab.Content.Handle == e.WindowHandle))
            {
                SelectedTabIndex = Tabs.IndexOf(tab);
                TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tab.Content);
                break;
            }

            if (WindowState == FormWindowState.Minimized)
            {
                User32.ShowWindow(Handle, 3);
            }
            else
            {
                Focus();
            }
        }

        private void preview_TabbedThumbnailClosed(object sender, TabbedThumbnailEventArgs e)
        {
            foreach (TitleBarTab tab in Tabs.Where(tab => tab.Content.Handle == e.WindowHandle))
            {
                CloseTab(tab);
                break;
            }
        }

        private void _tabs_CollectionModified(object sender, ListModificationEventArgs e)
        {
            SetFrameSize();

            if (e.Modification == ListModification.ItemAdded || e.Modification == ListModification.RangeAdded)
            {
                for (int i = 0; i < e.Count; i++)
                {
                    TitleBarTab currentTab = Tabs[i + e.StartIndex];

                    currentTab.Content.TextChanged += Content_TextChanged;
                    currentTab.Closing += TitleBarTabs_Closing;

                    if (AeroPeekEnabled)
                    {
                        TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(CreateThumbnailPreview(currentTab));
                    }
                }
            }

            _overlay?.Render(true);
        }

        protected virtual TabbedThumbnail CreateThumbnailPreview(TitleBarTab tab)
        {
            TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content);

            if (preview != null)
            {
                TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tab.Content);
            }

            preview = new TabbedThumbnail(Handle, tab.Content)
            {
                Title = tab.Content.Text,
                Tooltip = tab.Content.Text
            };

            preview.SetWindowIcon((Icon)tab.Content.Icon.Clone());
            preview.TabbedThumbnailActivated += preview_TabbedThumbnailActivated;
            preview.TabbedThumbnailClosed += preview_TabbedThumbnailClosed;
            preview.TabbedThumbnailBitmapRequested += preview_TabbedThumbnailBitmapRequested;
            TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview);

            return preview;
        }

        public virtual void UpdateThumbnailPreviewIcon(TitleBarTab tab, Icon icon = null)
        {
            if (!AeroPeekEnabled)
            {
                return;
            }

            TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content);

            if (preview == null)
            {
                return;
            }

            icon ??= tab.Content.Icon;
            preview.SetWindowIcon((Icon)icon.Clone());
        }

        private void Content_TextChanged(object sender, EventArgs e)
        {
            if (AeroPeekEnabled)
            {
                TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview((Form)sender);

                if (preview != null)
                {
                    preview.Title = ((Form)sender).Text;
                }
            }

            _overlay?.Render(true);
        }

        private void TitleBarTabs_Closing(object sender, CancelEventArgs e)
        {
            if (e.Cancel)
            {
                return;
            }

            TitleBarTab tab = (TitleBarTab)sender;
            CloseTab(tab);

            if (!tab.Content.IsDisposed && AeroPeekEnabled)
            {
                TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tab.Content);
            }

            _overlay?.Render(true);
        }

        public void RedrawTabs()
        {
            _overlay?.Render(true);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (_previousWindowState != null && WindowState != _previousWindowState.Value)
            {
                SetFrameSize();
            }

            _previousWindowState = WindowState;

            base.OnSizeChanged(e);
        }

        protected override void WndProc(ref Message m)
        {
            bool callDefaultWndProc = true;

            switch ((WM)m.Msg)
            {
                case WM.WM_ACTIVATE:
                    if ((m.WParam.ToInt64() & 0x0000FFFF) != 0)
                    {
                        SetFrameSize();
                        ResizeTabContents();
                        m.Result = IntPtr.Zero;
                    }

                    break;

                case WM.WM_NCHITTEST:
                    base.WndProc(ref m);

                    HT hitResult = (HT)m.Result.ToInt32();

                    if (!(hitResult == HT.HTCLOSE || hitResult == HT.HTMINBUTTON || hitResult == HT.HTMAXBUTTON || hitResult == HT.HTMENU || hitResult == HT.HTSYSMENU))
                    {
                        m.Result = new IntPtr((int)HitTest(m));
                    }

                    callDefaultWndProc = false;

                    break;

                case WM.WM_NCLBUTTONDOWN:
                    if (((HT)m.WParam.ToInt32()) == HT.HTMINBUTTON && AeroPeekEnabled && SelectedTab != null)
                    {
                        UpdateTabThumbnail(SelectedTab);
                    }

                    break;
            }

            if (callDefaultWndProc)
            {
                base.WndProc(ref m);
            }
        }

        public virtual void AddNewTab()
        {
            TitleBarTab newTab = CreateTab();

            Tabs.Add(newTab);
            ResizeTabContents(newTab);

            SelectedTabIndex = _tabs.Count - 1;
        }

        protected virtual void CloseTab(TitleBarTab closingTab)
        {
            int removeIndex = Tabs.IndexOf(closingTab);
            int selectedTabIndex = SelectedTabIndex;

            Tabs.Remove(closingTab);

            if (selectedTabIndex > removeIndex)
            {
                SelectedTabIndex = selectedTabIndex - 1;
            }
            else if (selectedTabIndex == removeIndex)
            {
                SelectedTabIndex = Math.Min(selectedTabIndex, Tabs.Count - 1);
            }
            else
            {
                SelectedTabIndex = selectedTabIndex;
            }

            if (_previews.ContainsKey(closingTab.Content))
            {
                _previews[closingTab.Content].Dispose();
                _previews.Remove(closingTab.Content);
            }

            if (_previousActiveTab != null && closingTab.Content == _previousActiveTab.Content)
            {
                _previousActiveTab = null;
            }

            if (Tabs.Count == 0 && ExitOnLastTabClose)
            {
                Close();
            }
        }

        private HT HitTest(Message m)
        {
            int lParam = (int)m.LParam;
            Point point = new Point(lParam & 0xFFFF, lParam >> 16);

            return HitTest(point, m.HWnd);
        }

        private HT HitTest(Point point, IntPtr windowHandle)
        {
            RECT rect;
            User32.GetWindowRect(windowHandle, out rect);
            Rectangle area = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

            int row = 1;
            int column = 1;
            bool onResizeBorder = false;

            if (point.Y >= area.Top && point.Y < area.Top + SystemInformation.VerticalResizeBorderThickness + _nonClientAreaHeight - 2)
            {
                onResizeBorder = point.Y < (area.Top + SystemInformation.VerticalResizeBorderThickness);
                row = 0;
            }
            else if (point.Y < area.Bottom && point.Y > area.Bottom - SystemInformation.VerticalResizeBorderThickness)
            {
                row = 2;
            }

            if (point.X >= area.Left && point.X < area.Left + SystemInformation.HorizontalResizeBorderThickness)
            {
                column = 0;
            }
            else if (point.X < area.Right && point.X >= area.Right - SystemInformation.HorizontalResizeBorderThickness)
            {
                column = 2;
            }

            HT[,] hitTests =
            {
                {
                    onResizeBorder ? HT.HTTOPLEFT : HT.HTLEFT,
                    onResizeBorder ? HT.HTTOP : HT.HTCAPTION,
                    onResizeBorder ? HT.HTTOPRIGHT : HT.HTRIGHT
                },
                { HT.HTLEFT, HT.HTNOWHERE, HT.HTRIGHT },
                { HT.HTBOTTOMLEFT, HT.HTBOTTOM, HT.HTBOTTOMRIGHT }
            };

            return hitTests[row, column];
        }

        private void ApplicationFormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (TitleBarTab tab in Tabs.ToArray())
            {
                if (tab.Content != null)
                {
                    bool formClosed = false;

                    tab.Content.FormClosed += (_, __) =>
                    {
                        formClosed = true;
                    };

                    Invoke(new Action(() =>
                    {
                        tab.Content.Close();
                    }));

                    if (!formClosed)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
            }
        }
    }
}
