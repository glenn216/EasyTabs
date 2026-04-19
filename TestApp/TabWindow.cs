using EasyTabs;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TestApp
{
    public partial class TabWindow : Form
    {
        private static readonly HttpClient FaviconHttpClient = new HttpClient();
        public readonly WebView2 WebBrowser;
        private readonly Panel _browserContainer;
        private bool faviconLoaded = false;

        protected TitleBarTabs ParentTabs
        {
            get
            {
                return ParentForm as TitleBarTabs;
            }
        }

        public TabWindow()
        {
            InitializeComponent();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            WebBrowser = new WebView2
            {
                MinimumSize = new Size(20, 20),
                Name = "webBrowser",
                Dock = DockStyle.Fill,
                TabIndex = 6
            };

            _browserContainer = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, 38),
                Name = "browserContainer",
                Size = new Size(ClientSize.Width, ClientSize.Height - 38),
                TabIndex = 5
            };

            _browserContainer.Controls.Add(WebBrowser);
            Controls.Add(_browserContainer);
            _browserContainer.SendToBack();
            toolbarBackground.BringToFront();
            urlBoxBackground.BringToFront();

            WebBrowser.CoreWebView2InitializationCompleted += WebBrowser_CoreWebView2InitializationCompleted;
            _ = InitializeWebViewAsync();
        }

        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            await WebBrowser.EnsureCoreWebView2Async(null);
            WebBrowser.Source = new Uri("about:blank");
        }

        private void WebBrowser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess || WebBrowser.CoreWebView2 == null)
            {
                return;
            }

            WebBrowser.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            WebBrowser.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            WebBrowser.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            WebBrowser.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;

            if (ParentTabs == null)
            {
                return;
            }

            ParentTabs.Invoke(new Action(() =>
            {
                ParentTabs.AddNewTab();
                TabWindow newTab = ParentTabs.SelectedTab.Content as TabWindow;

                if (newTab?.WebBrowser?.CoreWebView2 != null)
                {
                    newTab.WebBrowser.CoreWebView2.Navigate(e.Uri);
                }
                else if (newTab?.WebBrowser != null)
                {
                    newTab.WebBrowser.CoreWebView2InitializationCompleted += (_, args) =>
                    {
                        if (args.IsSuccess && newTab.WebBrowser.CoreWebView2 != null)
                        {
                            newTab.WebBrowser.CoreWebView2.Navigate(e.Uri);
                        }
                    };
                }
            }));
        }

        private async void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            string address = WebBrowser.Source?.ToString() ?? "about:blank";

            Invoke(new Action(() => urlTextBox.Text = address));

            if (address != "about:blank" && !faviconLoaded)
            {
                Uri uri = new Uri(address);

                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    try
                    {
                        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri.Scheme + "://" + uri.Host + "/favicon.ico");
                        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36");

                        using HttpResponseMessage response = await FaviconHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        await using Stream stream = await response.Content.ReadAsStreamAsync();

                        if (stream != null)
                        {
                            byte[] buffer = new byte[1024];

                            using (MemoryStream ms = new MemoryStream())
                            {
                                int read;

                                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    ms.Write(buffer, 0, read);
                                }

                                ms.Seek(0, SeekOrigin.Begin);

                                Invoke(new Action(() =>
                                {
                                    Icon = new Icon(ms);

                                    ParentTabs?.UpdateThumbnailPreviewIcon(ParentTabs.Tabs.Single(t => t.Content == this));
                                    ParentTabs?.RedrawTabs();
                                }));
                            }
                        }
                    }
                    catch
                    {
                        Invoke(new Action(() => Icon = Resources.DefaultIcon));
                    }
                }

                Invoke(new Action(() => Parent?.Refresh()));
                faviconLoaded = true;
            }
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            string title = WebBrowser.CoreWebView2?.DocumentTitle;
            Invoke(new Action(() => Text = string.IsNullOrWhiteSpace(title) ? "New Tab" : title));
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (urlTextBox.Text == "about:blank")
            {
                Invoke(new Action(() => Icon = Resources.DefaultIcon));
            }
        }

        private void backButton_MouseEnter(object sender, EventArgs e)
        {
            backButton.BackgroundImage = Resources.ButtonHoverBackground;
        }

        private void backButton_MouseLeave(object sender, EventArgs e)
        {
            backButton.BackgroundImage = null;
        }

        private void urlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string fullUrl = urlTextBox.Text;

                if (!Regex.IsMatch(fullUrl, "^[a-zA-Z0-9]+\\://"))
                {
                    fullUrl = "http://" + fullUrl;
                }

                faviconLoaded = false;

                if (WebBrowser.CoreWebView2 != null)
                {
                    WebBrowser.CoreWebView2.Navigate(fullUrl);
                }
                else
                {
                    WebBrowser.Source = new Uri(fullUrl);
                }
            }
        }

        private void forwardButton_MouseEnter(object sender, EventArgs e)
        {
            forwardButton.BackgroundImage = Resources.ButtonHoverBackground;
        }

        private void forwardButton_MouseLeave(object sender, EventArgs e)
        {
            forwardButton.BackgroundImage = null;
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            if (WebBrowser.CoreWebView2?.CanGoBack == true)
            {
                WebBrowser.CoreWebView2.GoBack();
            }
        }

        private void forwardButton_Click(object sender, EventArgs e)
        {
            if (WebBrowser.CoreWebView2?.CanGoForward == true)
            {
                WebBrowser.CoreWebView2.GoForward();
            }
        }

        private void TabWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            WebBrowser.Dispose();
        }
    }
}
