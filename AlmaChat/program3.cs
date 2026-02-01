using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NStack;
using Terminal.Gui;
using Terminal.Gui.Graphs;

// === ENUMS ===
public enum AppTheme { HighContrast, SoftDark, Catppuccin }

class Program
{
    // === КОНФИГУРАЦИЯ СЕТИ ===
    static string BaseUrl = "http://144.31.93.163"; 
    static string WsChatUrl = "ws://144.31.93.163/ws/chat";
    static string WsSearchUrl = "ws://144.31.93.163/ws/Search/users";
    static string WsNotifUrl = "ws://144.31.93.163/ws/notifications";

    static JsonSerializerOptions JsonOpts = new JsonSerializerOptions 
    { 
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    static HttpClient http = new();
    static string AuthCookieValue = ""; 

    // === WEBSOCKETS ===
    static ClientWebSocket? wsChat;
    static CancellationTokenSource? wsChatCts;

    static ClientWebSocket? wsNotif;
    static CancellationTokenSource? wsNotifCts;

    // === ДАННЫЕ ===
    static List<ChatDto> Chats = new();
    static List<MessageDto> CurrentMessages = new(); 
    static long CurrentUserId = 0; 
    static string CurrentUserEmail = "";
    static ChatDto? ActiveChat = null;
    
    // === СИСТЕМНЫЕ ДАННЫЕ ===
    static DateTime AppStartTime;

    // === UI ЭЛЕМЕНТЫ ===
    static Toplevel top;
    static Window mainWin;
    static Window authWin;
    
    // Элементы статус-бара
    static Label lblPing;
    static Label lblRam;
    static Label lblUptime;
    static Label wsStatusLabel;
    static Label notifLabel;
    
    static ListView messagesListView; 
    static ListView chatListView;
    static TextField inputField;
    static MenuBar menuBar;

    // === ЦВЕТА ===
    static AppTheme CurrentTheme = AppTheme.Catppuccin;
    static ColorScheme BaseScheme, AccentScheme, InputScheme, ListScheme, ErrorScheme, NotifScheme, InfoScheme;

    static void Main()
    {
        AppStartTime = DateTime.Now;
        try 
        {
            // Инициализация драйвера терминала
            Application.Init();
            top = Application.Top;

            // Применяем тему по умолчанию
            ApplyTheme(AppTheme.Catppuccin);
            
            ShowAuthWindow();
            
            // Запуск мониторинга ресурсов (в фоне)
            _ = StartSystemMonitor();
            
            // Запуск главного цикла
            Application.Run();
        }
        catch (Exception ex)
        {
            Application.Shutdown();
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nCRITICAL ERROR: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            Application.Shutdown();
            Console.ResetColor();
            Console.Clear();
            Console.CursorVisible = true;
            Console.WriteLine("System shutdown complete. Good luck, Operator.");
        }
    }

    // ================== THEMES & MONITORING ==================

    static void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        
        switch (theme)
        {
            case AppTheme.SoftDark:
                // Мягкий серый текст на черном
                BaseScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black), Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray), HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Black) };
                InputScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black), Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray) };
                ListScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black), Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray), HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Black) };
                AccentScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.Blue, Color.Black) };
                InfoScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black) };
                break;

            case AppTheme.Catppuccin:
                // Имитация Catppuccin (Mocha)
                var bg = Color.Black; 
                BaseScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightCyan, bg), Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightBlue), HotNormal = Application.Driver.MakeAttribute(Color.BrightMagenta, bg) };
                InputScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray), Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen) };
                ListScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightBlue, bg), Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightMagenta), HotNormal = Application.Driver.MakeAttribute(Color.BrightGreen, bg) };
                AccentScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightMagenta, bg) };
                InfoScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.Gray, bg) };
                break;

            case AppTheme.HighContrast:
            default:
                // Классический терминал
                BaseScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.White, Color.Black), Focus = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black), HotNormal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black) };
                InputScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Gray), Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightCyan) };
                ListScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black), Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen), HotNormal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black) };
                AccentScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightMagenta, Color.Black) };
                InfoScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black) };
                break;
        }

        ErrorScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black) };
        NotifScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.Black, Color.BrightYellow) };

        // Перерисовка текущих окон
        if (mainWin != null) {
            mainWin.ColorScheme = BaseScheme;
            chatListView.ColorScheme = ListScheme;
            messagesListView.ColorScheme = BaseScheme;
            inputField.ColorScheme = InputScheme;
            
            if(lblPing != null) {
                lblPing.ColorScheme = InfoScheme;
                lblRam.ColorScheme = InfoScheme;
                lblUptime.ColorScheme = InfoScheme;
            }
        }
        if (authWin != null) authWin.ColorScheme = BaseScheme;
    }

    static async Task StartSystemMonitor()
    {
        while (true)
        {
            try
            {
                // 1. Uptime
                var uptime = DateTime.Now - AppStartTime;
                string uptimeStr = $"UP: {uptime:hh\\:mm\\:ss}";

                // 2. RAM Usage
                using var proc = Process.GetCurrentProcess();
                long mem = proc.WorkingSet64 / 1024 / 1024; // MB
                string ramStr = $"RAM: {mem}MB";

                // 3. PING
                Stopwatch sw = Stopwatch.StartNew();
                string pingStr = "PING: --ms";
                try {
                    using var msg = new HttpRequestMessage(HttpMethod.Head, BaseUrl);
                    var resp = await http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
                    sw.Stop();
                    long ms = sw.ElapsedMilliseconds;
                    pingStr = $"PING: {ms}ms";
                } catch { pingStr = "PING: ERR"; }

                // UI Update
                if (mainWin != null && lblPing != null)
                {
                    Application.MainLoop.Invoke(() => {
                        lblUptime.Text = uptimeStr;
                        lblRam.Text = ramStr;
                        lblPing.Text = pingStr;
                    });
                }
            }
            catch {}
            await Task.Delay(3000); 
        }
    }

    static void ShowThemeSelector()
    {
        var d = new Dialog(" :: UI THEME :: ", 40, 10);
        var rGroup = new RadioGroup(new ustring[] { "High Contrast", "Soft Dark", "Catppuccin" }) { X = 2, Y = 1 };
        rGroup.SelectedItem = (int)CurrentTheme;

        var btnApply = new Button("Apply") { X = Pos.Center(), Y = 6 };
        btnApply.Clicked += () => {
            ApplyTheme((AppTheme)rGroup.SelectedItem);
            Application.RequestStop();
        };

        d.Add(rGroup, btnApply);
        Application.Run(d);
    }

    // ================== NOTIFICATIONS ==================

    static void SendSystemNotification(string title, string message)
    {
        Task.Run(() => 
        {
            try {
                string safeMsg = message.Replace("\"", "\\\"");
                string safeTitle = title.Replace("\"", "\\\"");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    Process.Start("notify-send", $"\"{safeTitle}\" \"{safeMsg}\"");
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    string psCommand = $"Add-Type -AssemblyName System.Windows.Forms; $notify = New-Object System.Windows.Forms.NotifyIcon; $notify.Icon = [System.Drawing.SystemIcons]::Information; $notify.Visible = $true; $notify.ShowBalloonTip(0, '{safeTitle}', '{safeMsg}', [System.Windows.Forms.ToolTipIcon]::None)";
                    Process.Start(new ProcessStartInfo { FileName = "powershell", Arguments = $"-Command \"{psCommand}\"", CreateNoWindow = true });
                }
            } catch {}
        });
    }

    static void ShowNotification(string text)
    {
        Application.MainLoop.Invoke(() => {
            try { Console.Beep(); } catch {}
            notifLabel.Text = $" 🔔 {text} ";
            notifLabel.ColorScheme = NotifScheme;
            SendSystemNotification("AlmaChat", text);

            Task.Delay(5000).ContinueWith(_ => Application.MainLoop.Invoke(() => {
                notifLabel.Text = "";
                notifLabel.ColorScheme = BaseScheme;
            }));
        });
    }

    // ================== AUTH & WINDOWS ==================

    static void ShowAuthWindow()
    {
        if (mainWin != null) top.Remove(mainWin);
        authWin = new Window(" :: SYSTEM ACCESS :: ") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = BaseScheme };
        var frame = new FrameView(" CREDENTIALS ") { X = Pos.Center(), Y = Pos.Center(), Width = 50, Height = 16, ColorScheme = BaseScheme };

        var userField = new TextField("user1@example.com") { X = 12, Y = 2, Width = 30, ColorScheme = InputScheme };
        var passField = new TextField("password") { X = 12, Y = 4, Width = 30, Secret = true, ColorScheme = InputScheme };
        var btnLogin = new Button(" [ LOGIN ] ") { X = 8, Y = 8, ColorScheme = ListScheme };
        var btnReg = new Button(" [ REGISTER ] ") { X = 24, Y = 8, ColorScheme = BaseScheme };

        btnLogin.Clicked += async () => await PerformAuth(userField.Text.ToString(), passField.Text.ToString(), false);
        btnReg.Clicked += async () => await PerformAuth(userField.Text.ToString(), passField.Text.ToString(), true);

        frame.Add(new Label("EMAIL :") { X = 2, Y = 2 }, userField, new Label("PASS  :") { X = 2, Y = 4 }, passField, btnLogin, btnReg);
        authWin.Add(frame);
        top.Add(authWin);
    }

    static async Task PerformAuth(string email, string password, bool isRegister, bool openProfileSettings = false)
    {
        string endpoint = isRegister ? "/api/Login/register" : "/api/Login/login";
        var payload = new { email = email, password = password, username = email.Split('@')[0], confirmPassword = password }; 

        try 
        {
            var res = await http.PostAsync($"{BaseUrl}{endpoint}", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            
            if (!res.IsSuccessStatusCode) { 
                string err = await res.Content.ReadAsStringAsync();
                MessageBox.ErrorQuery("ERROR", $"Status: {res.StatusCode}\n{err}", "OK"); 
                return; 
            }

            // Если это регистрация -> Переходим к подтверждению кода
            if (isRegister) { 
                Application.MainLoop.Invoke(() => ShowVerificationDialog(email, password)); 
                return; 
            }

            if (res.Headers.TryGetValues("Set-Cookie", out var cookies)) {
                AuthCookieValue = cookies.FirstOrDefault() ?? "";
                http.DefaultRequestHeaders.Remove("Cookie");
                http.DefaultRequestHeaders.Add("Cookie", AuthCookieValue);
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if(doc.RootElement.TryGetProperty("id", out var idEl)) CurrentUserId = idEl.GetInt64();
            CurrentUserEmail = email;

            top.Remove(authWin); 
            
            if (openProfileSettings)
            {
                // Вход после регистрации -> Настройка профиля
                ShowProfileDialog(isFirstSetup: true);
            }
            else
            {
                // Обычный вход
                ShowMainWindow();
            }

            await LoadChatsAsync();
            await ConnectNotifWsAsync();
        }
        catch (Exception ex) { MessageBox.ErrorQuery("CRITICAL ERROR", ex.Message, "OK"); }
    }
        
    // ================== EMAIL VERIFICATION ==================

    static void ShowVerificationDialog(string email, string password)
    {
        var d = new Dialog(" :: VERIFICATION REQUIRED :: ", 60, 10);
        
        var lblInfo = new Label($"A 6-digit code has been sent to:\n{email}") { X = 1, Y = 1, Width = Dim.Fill(), Height = 2 };
        var codeField = new TextField("") { X = Pos.Center(), Y = 4, Width = 10, ColorScheme = InputScheme };
        
        var btnConfirm = new Button(" [ CONFIRM ] ") { X = Pos.Center() - 14, Y = 7, ColorScheme = ListScheme };
        var btnResend = new Button(" [ RESEND CODE ] ") { X = Pos.Center() + 2, Y = 7, ColorScheme = ErrorScheme };

        btnConfirm.Clicked += async () => 
        {
            string code = codeField.Text.ToString().Trim();
            if (code.Length < 6) { MessageBox.ErrorQuery("Error", "Code too short", "OK"); return; }
        
            bool success = await ConfirmEmailAction(email, code);
            if (success) 
            {
                Application.RequestStop();
                MessageBox.Query("Success", "Email confirmed!", "OK");
                // Логинимся и открываем настройки профиля
                await PerformAuth(email, password, isRegister: false, openProfileSettings: true); 
            }
        };

        btnResend.Clicked += async () => await ResendCodeAction(email);

        d.Add(lblInfo, codeField, btnConfirm, btnResend);
        codeField.SetFocus();
        Application.Run(d);
    }

    static async Task<bool> ConfirmEmailAction(string email, string code)
    {
        var payload = new { email = email, code = code };
        try 
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await http.PostAsync($"{BaseUrl}/api/Login/confirm", content);
            if (!res.IsSuccessStatusCode) {
                string msg = await res.Content.ReadAsStringAsync();
                MessageBox.ErrorQuery("Verification Failed", msg, "OK");
                return false;
            }
            return true;
        }
        catch (Exception ex) { MessageBox.ErrorQuery("Network Error", ex.Message, "OK"); return false; }
    }

    static async Task ResendCodeAction(string email)
    {
        var payload = new { email = email };
        try {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await http.PostAsync($"{BaseUrl}/api/Login/resend-code", content);
            if (res.IsSuccessStatusCode) MessageBox.Query("Sent", "New code sent.", "OK");
            else MessageBox.ErrorQuery("Error", "Failed to resend.", "OK");
        } catch {}
    }

    // ================== PROFILE SETTINGS ==================

    static void ShowProfileDialog(bool isFirstSetup = false)
    {
        var d = new Dialog(" :: PROFILE SETTINGS :: ", 60, 20);
        
        var tfUser = new TextField("") { X = 14, Y = 1, Width = 40, ColorScheme = InputScheme };
        var tfFirst = new TextField("") { X = 14, Y = 3, Width = 40, ColorScheme = InputScheme };
        var tfLast = new TextField("") { X = 14, Y = 5, Width = 40, ColorScheme = InputScheme };
        var tfBio = new TextView() { X = 14, Y = 7, Width = 40, Height = 5, ColorScheme = InputScheme };

        d.Add(
            new Label("Username:") { X = 2, Y = 1 }, tfUser,
            new Label("First Name:") { X = 2, Y = 3 }, tfFirst,
            new Label("Last Name:") { X = 2, Y = 5 }, tfLast,
            new Label("Bio:") { X = 2, Y = 7 }, tfBio
        );

        var btnSave = new Button(" [ SAVE PROFILE ] ") { X = Pos.Center(), Y = 14, ColorScheme = ListScheme };
        
        if (isFirstSetup)
        {
            var btnSkip = new Button("Skip >>") { X = Pos.AnchorEnd(10), Y = 14 };
            btnSkip.Clicked += () => { Application.RequestStop(); ShowMainWindow(); };
            d.Add(btnSkip);
        }

        Task.Run(async () => {
            try {
                var res = await http.GetAsync($"{BaseUrl}/api/Users/{CurrentUserId}");
                if (res.IsSuccessStatusCode) {
                    var json = await res.Content.ReadAsStringAsync();
                    var profile = JsonSerializer.Deserialize<UserProfileDto>(json, JsonOpts);
                    if (profile != null) Application.MainLoop.Invoke(() => {
                        tfUser.Text = profile.Username ?? "";
                        tfFirst.Text = profile.FirstName ?? "";
                        tfLast.Text = profile.LastName ?? "";
                        tfBio.Text = profile.Bio ?? "";
                    });
                }
            } catch {}
        });

        btnSave.Clicked += async () => {
            var updatePayload = new {
                id = CurrentUserId,
                username = tfUser.Text.ToString(),
                email = CurrentUserEmail,
                first_name = tfFirst.Text.ToString(),
                last_name = tfLast.Text.ToString(),
                bio = tfBio.Text.ToString()
            };

            try {
                var content = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");
                var res = await http.PutAsync($"{BaseUrl}/api/Users/{CurrentUserId}", content);
                if (res.IsSuccessStatusCode) {
                    MessageBox.Query("Success", "Profile updated!", "OK");
                    Application.RequestStop();
                    if (isFirstSetup) ShowMainWindow(); 
                } else MessageBox.ErrorQuery("Error", $"Status: {res.StatusCode}", "OK");
            } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "OK"); }
        };

        d.Add(btnSave);
        Application.Run(d);
    }

    // ================== MAIN WINDOW (Redesigned) ==================

    static void ShowMainWindow()
    {
        mainWin = new Window() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = BaseScheme };

        menuBar = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_SYSTEM", new MenuItem [] { 
                new MenuItem ("_Settings", "Profile & Config", () => ShowProfileDialog(false)),
                new MenuItem ("_Themes", "Change Colors", () => ShowThemeSelector()), // Новое меню
                new MenuItem ("_Exit", "", () => Application.RequestStop())
            }),
            new MenuBarItem ("_NETWORK", new MenuItem [] {
                new MenuItem ("_Search User", "Find & Chat", () => ShowLiveSearchDialog()),
                new MenuItem ("_Create Group", "New Group", () => ShowCreateGroupDialog()),
                new MenuItem ("_Refresh", "Reload Chats", async () => await LoadChatsAsync())
            }),
            new MenuBarItem ("_CHANNEL", new MenuItem[] {
                new MenuItem ("_Info", "", () => ShowChatInfo()),
                new MenuItem ("_Rename", "", () => ShowRenameGroupDialog()),
                new MenuItem ("_Delete", "", () => DeleteCurrentChat())
            })
        });
        top.Add(menuBar);

        // ВЕРХНЯЯ ПАНЕЛЬ С ДАТЧИКАМИ
        var topBar = new View() { X = 0, Y = 1, Width = Dim.Fill(), Height = 1, ColorScheme = BaseScheme };
        
        var lblUser = new Label($" USR: {CurrentUserEmail} ") { ColorScheme = AccentScheme };
        
        // Гиковская статистика (справа налево)
        lblPing = new Label("PING: --ms") { X = Pos.AnchorEnd(38), ColorScheme = InfoScheme };
        lblRam = new Label("RAM: --MB") { X = Pos.AnchorEnd(26), ColorScheme = InfoScheme };
        lblUptime = new Label("UP: --:--") { X = Pos.AnchorEnd(14), ColorScheme = InfoScheme };
        
        wsStatusLabel = new Label(" ● ") { X = Pos.AnchorEnd(3), ColorScheme = ErrorScheme };

        notifLabel = new Label("") { X = Pos.Center(), Y = 0, Width = 40, TextAlignment = TextAlignment.Centered, ColorScheme = BaseScheme };

        topBar.Add(lblUser, notifLabel, lblPing, lblRam, lblUptime, wsStatusLabel);
        mainWin.Add(topBar, new LineView(Orientation.Horizontal){ Y = 2 });

        // ЧАТЫ
        var chatFrame = new FrameView(" [ CHANNELS ] ") { X = 0, Y = 3, Width = 30, Height = Dim.Fill(), ColorScheme = BaseScheme };
        chatListView = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = ListScheme };
        chatListView.SelectedItemChanged += async _ => await OnChatSelected();
        chatFrame.Add(chatListView);
        mainWin.Add(chatFrame);

        // СООБЩЕНИЯ
        var msgFrame = new FrameView(" [ STREAM ] ") { X = 30, Y = 3, Width = Dim.Fill(), Height = Dim.Fill() - 3, ColorScheme = BaseScheme };
        messagesListView = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = BaseScheme };
        
        messagesListView.KeyPress += (e) => {
            if (ActiveChat == null || CurrentMessages.Count == 0) return;
            var idx = messagesListView.SelectedItem;
            if (idx < 0 || idx >= CurrentMessages.Count) return;
            var msg = CurrentMessages[idx];
            if (msg.SenderId != CurrentUserId) return; 
            if (e.KeyEvent.Key == Key.DeleteChar || e.KeyEvent.Key == Key.Backspace) {
                if (MessageBox.Query("CONFIRM", "Delete message?", "Yes", "No") == 0) _ = DeleteMessage(msg.Id);
                e.Handled = true;
            }
        };
        msgFrame.Add(messagesListView);
        mainWin.Add(msgFrame);

        // ВВОД
        var inputFrame = new View() { X = 30, Y = Pos.AnchorEnd(3), Width = Dim.Fill(), Height = 3 };
        inputField = new TextField("") { X = 2, Y = 1, Width = Dim.Fill() - 10, ColorScheme = InputScheme };
        var btnSend = new Button("SEND") { X = Pos.AnchorEnd(8), Y = 1, ColorScheme = ListScheme };
        inputFrame.Add(new Label("➜") { Y=1, ColorScheme = AccentScheme }, inputField, btnSend);
        mainWin.Add(inputFrame);
        
        top.Add(mainWin);

        btnSend.Clicked += () => _ = SendMessageAction();
        inputField.KeyPress += (e) => { if(e.KeyEvent.Key == Key.Enter) { _ = SendMessageAction(); e.Handled = true; } };
    }

    // ================== LOGIC ==================

    static async Task ConnectNotifWsAsync()
    {
        wsNotifCts?.Cancel(); wsNotif?.Dispose();
        wsNotifCts = new CancellationTokenSource();
        wsNotif = new ClientWebSocket();
        if(!string.IsNullOrEmpty(AuthCookieValue)) wsNotif.Options.SetRequestHeader("Cookie", AuthCookieValue);

        try {
            await wsNotif.ConnectAsync(new Uri(WsNotifUrl), wsNotifCts.Token);
            _ = NotifReceiveLoop();
        } catch {}
    }

    static async Task NotifReceiveLoop()
    {
        var buffer = new byte[4096];
        while (wsNotif.State == WebSocketState.Open) {
            try {
                var res = await wsNotif.ReceiveAsync(new ArraySegment<byte>(buffer), wsNotifCts.Token);
                if (res.MessageType == WebSocketMessageType.Close) break;
                
                var str = Encoding.UTF8.GetString(buffer, 0, res.Count);
                using var doc = JsonDocument.Parse(str);
                var root = doc.RootElement;
                
                if(root.TryGetProperty("type", out var t)) {
                    string type = t.GetString() ?? "";
                    if(type == "ping") {
                         long ts = 0; if(root.TryGetProperty("ts", out var x)) ts = x.GetInt64();
                         var pong = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type="pong", ts=ts }));
                         await wsNotif.SendAsync(new ArraySegment<byte>(pong), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else if(type == "notification") {
                        string body = root.TryGetProperty("body", out var b) ? b.GetString() : "New Notification";
                        long chatId = 0; if(root.TryGetProperty("chat_id", out var cid)) chatId = cid.GetInt64();
                        if(ActiveChat == null || ActiveChat.Id != chatId) ShowNotification(body);
                    }
                }
            } catch { break; }
        }
    }

    static string CleanContent(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string current = input.Trim();
        if (!current.StartsWith("{")) return current;
        try {
            using var doc = JsonDocument.Parse(current);
            var root = doc.RootElement;
            if (root.TryGetProperty("content", out var c)) return c.GetString() ?? "";
            if (root.TryGetProperty("text", out var t)) return t.GetString() ?? "";
            return current;
        } catch { return current; }
    }
    
    // --- CHAT LOGIC ---

    static async Task OnChatSelected()
    {
        if (chatListView.SelectedItem < 0 || Chats.Count == 0) return;
        ActiveChat = Chats[chatListView.SelectedItem];
        CurrentMessages.Clear();
        messagesListView.SetSource(null);
        await LoadHistoryAsync(ActiveChat.Id);
        await ConnectChatWsAsync(ActiveChat.Id);
    }

    static async Task LoadHistoryAsync(long chatId)
    {
        try {
            var res = await http.GetAsync($"{BaseUrl}/api/Chat/history?chatId={chatId}");
            if (!res.IsSuccessStatusCode) return;
            var json = await res.Content.ReadAsStringAsync();
            var msgs = JsonSerializer.Deserialize<List<MessageDto>>(json, JsonOpts) ?? new();
            CurrentMessages = msgs; 
            RefreshMessageList();
        } catch {}
    }

    static void RefreshMessageList()
    {
        // ИСПРАВЛЕННАЯ ЛОГИКА ОТОБРАЖЕНИЯ ИМЕН
        var displayList = CurrentMessages.Select(m => {
            string nameToDisplay;

            if (m.SenderId == CurrentUserId)
            {
                nameToDisplay = "➜ YOU";
            }
            else
            {
                // 1. Имя из сообщения
                if (!string.IsNullOrEmpty(m.SenderName))
                    nameToDisplay = $"  {m.SenderName}";
                // 2. Имя из списка участников (если в истории пришел null)
                else
                {
                    var participant = ActiveChat?.Participants
                        .FirstOrDefault(p => p.UserId == m.SenderId.ToString());
                    
                    nameToDisplay = participant != null ? $"  {participant.Username}" : $"  User{m.SenderId}";
                }
            }

            string cleanText = CleanContent(m.Content);
            return $"{nameToDisplay}: {cleanText}";
        }).ToList();

        Application.MainLoop.Invoke(() => {
            messagesListView.SetSource(displayList);
            if(displayList.Count > 0) messagesListView.SelectedItem = displayList.Count - 1;
        });
    }

    static async Task SendMessageAction()
    {
        if (ActiveChat == null) return;
        var text = inputField.Text.ToString();
        if (string.IsNullOrWhiteSpace(text)) return;
        inputField.Text = "";
        if (wsChat == null || wsChat.State != WebSocketState.Open) { MessageBox.ErrorQuery("Error", "WS Offline", "Ok"); return; }
        try {
            var payloadObj = new { type = "message", content = text };
            var json = JsonSerializer.Serialize(payloadObj, JsonOpts);
            var bytes = Encoding.UTF8.GetBytes(json);
            await wsChat.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        } catch {}
    }

    static async Task ConnectChatWsAsync(long chatId)
    {
        wsChatCts?.Cancel(); wsChat?.Dispose();
        wsChatCts = new CancellationTokenSource();
        wsChat = new ClientWebSocket();
        if(!string.IsNullOrEmpty(AuthCookieValue)) wsChat.Options.SetRequestHeader("Cookie", AuthCookieValue);

        try {
            await wsChat.ConnectAsync(new Uri($"{WsChatUrl}?chatId={chatId}"), wsChatCts.Token);
            UpdateWsStatus(true);
            _ = ReceiveLoop();
        } catch { UpdateWsStatus(false); }
    }

    static async Task ReceiveLoop()
    {
        var buffer = new byte[8192];
        while (wsChat.State == WebSocketState.Open) {
            try {
                var res = await wsChat.ReceiveAsync(new ArraySegment<byte>(buffer), wsChatCts.Token);
                if (res.MessageType == WebSocketMessageType.Close) break;
                
                var str = Encoding.UTF8.GetString(buffer, 0, res.Count);
                using var doc = JsonDocument.Parse(str);
                var root = doc.RootElement;
                
                if(root.TryGetProperty("type", out var t)) {
                    string type = t.GetString() ?? "";

                    if(type == "ping") {
                         long ts = 0; if(root.TryGetProperty("ts", out var x)) ts = x.GetInt64();
                         var pong = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type="pong", ts=ts }));
                         await wsChat.SendAsync(new ArraySegment<byte>(pong), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else if(type == "message") {
                        long senderId = 0; if(root.TryGetProperty("sender_id", out var sid)) senderId = sid.GetInt64();
                        long msgId = 0; if(root.TryGetProperty("id", out var mid)) msgId = mid.GetInt64();
                        string content = root.TryGetProperty("content", out var c) ? c.GetString() : "";
                        string senderName = root.TryGetProperty("sender", out var sn) ? sn.GetString() : null; // null если не пришло

                        CurrentMessages.Add(new MessageDto { Id = msgId, SenderId = senderId, SenderName = senderName, Content = content });
                        RefreshMessageList();
                    }
                }
            } catch { break; }
        }
        UpdateWsStatus(false);
    }

    static void UpdateWsStatus(bool online)
    {
        Application.MainLoop.Invoke(() => {
            wsStatusLabel.Text = online ? " ● " : " ○ ";
            wsStatusLabel.ColorScheme = online ? 
                new ColorScheme{Normal=Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black)} : ErrorScheme;
        });
    }

    // --- UTILS & HELPERS ---

    static void ShowLiveSearchDialog()
    {
        var d = new Dialog(" :: LIVE SEARCH :: ", 60, 18);
        var searchField = new TextField() { X = 1, Y = 1, Width = Dim.Fill() - 2, ColorScheme = InputScheme };
        var resultsList = new ListView() { X = 1, Y = 3, Width = Dim.Fill() - 2, Height = Dim.Fill() - 2, ColorScheme = ListScheme };
        ClientWebSocket wsSearch = new ClientWebSocket();
        CancellationTokenSource cts = new CancellationTokenSource();
        List<UserDto> foundUsers = new();

        Task.Run(async () => {
            if(!string.IsNullOrEmpty(AuthCookieValue)) wsSearch.Options.SetRequestHeader("Cookie", AuthCookieValue);
            try {
                await wsSearch.ConnectAsync(new Uri(WsSearchUrl), cts.Token);
                var buffer = new byte[4096];
                while (wsSearch.State == WebSocketState.Open && !cts.Token.IsCancellationRequested) {
                    var res = await wsSearch.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (res.MessageType == WebSocketMessageType.Close) break;
                    var json = Encoding.UTF8.GetString(buffer, 0, res.Count);
                    var users = JsonSerializer.Deserialize<List<UserDto>>(json, JsonOpts) ?? new();
                    foundUsers = users;
                    Application.MainLoop.Invoke(() => resultsList.SetSource(foundUsers.Select(u => $"{u.Username} (ID:{u.Id})").ToList()));
                }
            } catch {} 
        });

        searchField.TextChanged += (txt) => {
            var q = searchField.Text.ToString();
            if(wsSearch.State == WebSocketState.Open) {
                var bytes = Encoding.UTF8.GetBytes(q);
                wsSearch.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        };

        resultsList.OpenSelectedItem += (e) => {
            var u = foundUsers[e.Item];
            _ = CreatePrivateChat(u.Username); 
            cts.Cancel();
            wsSearch.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            Application.RequestStop();
        };

        d.Add(new Label("Type to search:"), searchField, resultsList);
        Application.Run(d);
        cts.Cancel();
    }

    static async Task CreatePrivateChat(string username)
    {
        try {
            var res = await http.PostAsync($"{BaseUrl}/api/Chat/create_chat?username={username}", null);
            if(res.IsSuccessStatusCode) { await LoadChatsAsync(); MessageBox.Query("Success", $"Chat with {username} opened", "OK"); }
            else MessageBox.ErrorQuery("Err", await res.Content.ReadAsStringAsync(), "OK");
        } catch (Exception ex) { MessageBox.ErrorQuery("Ex", ex.Message, "OK"); }
    }

    static void ShowCreateGroupDialog()
    {
        var d = new Dialog(" :: NEW GROUP :: ", 50, 14);
        var nameField = new TextField("") { X = 1, Y = 2, Width = Dim.Fill() - 4 };
        var idsField = new TextField("") { X = 1, Y = 5, Width = Dim.Fill() - 4 };
        var btnCreate = new Button("Create") { X = Pos.Center(), Y = 8 };

        btnCreate.Clicked += () => {
            var name = nameField.Text.ToString();
            var idsStr = idsField.Text.ToString();
            var ids = idsStr.Split(',').Select(s => s.Trim()).Where(s => long.TryParse(s, out _)).Select(long.Parse).ToList();
            if(!string.IsNullOrWhiteSpace(name) && ids.Count > 0) _ = CreateGroup(name, ids);
            Application.RequestStop();
        };
        d.Add(new Label("Group Name:"), nameField, new Label("IDs (comma separated):") { Y = 4 }, idsField, btnCreate);
        Application.Run(d);
    }

    static async Task CreateGroup(string name, List<long> memberIds)
    {
        var payload = new { Name = name, MemberIds = memberIds };
        try {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await http.PostAsync($"{BaseUrl}/api/Chat/create_group", content);
            if(res.IsSuccessStatusCode) await LoadChatsAsync();
        } catch {}
    }

    static void ShowRenameGroupDialog()
    {
        if (ActiveChat == null || !ActiveChat.IsGroup) { MessageBox.ErrorQuery("Err", "Select a group", "OK"); return; }
        var d = new Dialog("Rename Group", 50, 8);
        var tf = new TextField(ActiveChat.Name ?? "") { X=1, Y=2, Width=Dim.Fill()-2 };
        var btn = new Button("Save"){X=Pos.Center(), Y=4};
        btn.Clicked += () => { _ = RenameGroupReq(ActiveChat.Id, tf.Text.ToString()); Application.RequestStop(); };
        d.Add(tf, btn);
        Application.Run(d);
    }

    static async Task RenameGroupReq(long chatId, string newName)
    {
        var payload = new { NewName = newName };
        try {
             var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
             var res = await http.PutAsync($"{BaseUrl}/api/Chat/group/{chatId}/rename", content);
             if(res.IsSuccessStatusCode) await LoadChatsAsync();
        } catch {}
    }

    static async Task DeleteMessage(long msgId)
    {
        try {
            var res = await http.DeleteAsync($"{BaseUrl}/api/Chat/message/{msgId}");
            if(res.IsSuccessStatusCode && ActiveChat != null) await LoadHistoryAsync(ActiveChat.Id);
        } catch {}
    }

    static async Task DeleteCurrentChat()
    {
        if(ActiveChat == null) return;
        if(MessageBox.Query("Delete", $"Delete chat?", "Yes", "No") == 1) return;
        try {
            await http.DeleteAsync($"{BaseUrl}/api/Chat/{ActiveChat.Id}");
            ActiveChat = null;
            CurrentMessages.Clear();
            messagesListView.SetSource(null);
            await LoadChatsAsync();
        } catch {}
    }

    static void ShowChatInfo()
    {
        if(ActiveChat == null) return;
        var list = ActiveChat.Participants.Select(p => $"[{p.UserId}] {p.Username}").ToList();
        var d = new Dialog($"Participants ({list.Count})", 50, 15);
        var lv = new ListView(list) { X=1, Y=1, Width=Dim.Fill()-2, Height=Dim.Fill()-2 };
        d.Add(lv);
        Application.Run(d);
    }

    static async Task LoadChatsAsync()
    {
        try {
            var res = await http.GetAsync($"{BaseUrl}/api/Chat/chats");
            if (!res.IsSuccessStatusCode) return;
            var json = await res.Content.ReadAsStringAsync();
            Chats = JsonSerializer.Deserialize<List<ChatDto>>(json, JsonOpts) ?? new();
            Application.MainLoop.Invoke(() => {
                chatListView.SetSource(Chats.Select(c => {
                    string icon = c.IsGroup ? "👥" : "👤";
                    return $"{icon} {c.DisplayName(CurrentUserId)}";
                }).ToList());
            });
        } catch {}
    }
}

// ================== DTOs ==================
public class UserDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; }
}

public class ParticipantDto
{
    [JsonPropertyName("userId")] public string UserId { get; set; } 
    [JsonPropertyName("username")] public string Username { get; set; }
}

public class ChatDto 
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("is_group")] public bool IsGroup { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("participants")] public List<ParticipantDto> Participants { get; set; } = new();

    public string DisplayName(long myId) 
    {
        if (IsGroup && !string.IsNullOrWhiteSpace(Name)) return Name;
        var other = Participants.FirstOrDefault(p => p.UserId != myId.ToString());
        return other?.Username ?? "Chat";
    }
}

public class MessageDto 
{ 
    [JsonPropertyName("id")] public long Id { get; set; } 
    [JsonPropertyName("sender_id")] public long SenderId { get; set; } 
    [JsonPropertyName("content")] public string Content { get; set; } 
    [JsonPropertyName("sender")] public string? SenderName { get; set; } 
}

public class UserProfileDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("bio")] public string? Bio { get; set; }
}