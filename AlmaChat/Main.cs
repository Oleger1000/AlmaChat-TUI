using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using AlmaChat;
using NStack;
using Terminal.Gui;
using Terminal.Gui.Graphs;

class Program
{
    // === КОНФИГУРАЦИЯ СЕТИ ===
    static string BaseUrl = "http://144.31.93.163"; 
    static string WsChatUrl = "ws://144.31.93.163/ws/chat";
    static string WsSearchUrl = "ws://144.31.93.163/ws/Search/users";
    static string WsNotifUrl = "ws://144.31.93.163/ws/notifications";

    // === КОНФИГУРАЦИЯ ПРИЛОЖЕНИЯ ===
    static string CurrentAppVersion = "dev"; 
    static string GithubOwner = "Oleger1000"; 
    static string GithubRepo = "AlmaChat-TUI"; 

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
    static HashSet<long> OnlineUsers = new(); 
    static long CurrentUserId = 0; 
    static string CurrentUserEmail = "";
    static ChatDto? ActiveChat = null;
    static DateTime AppStartTime;

    // === UI ЭЛЕМЕНТЫ ===
    static Toplevel top;
    static Window mainWin; 
    static Window authWin;
    
    // Панели (FrameView) для доступа к стилям
    static FrameView topPanel, leftPanel, rightPanel, inputPanel;

    // Элементы Dashboard
    static Label lblPing, lblRam, lblUptime, wsStatusLabel, notifLabel;
    static ProgressBar ramGraph, pingGraph; // Графики
    
    static ListView messagesListView; 
    static ListView chatListView;
    static TextField inputField;
    static MenuBar menuBar;

    static void Main()
    {
        // === GEEK ADDON: BOOT SEQUENCE ===
        Console.CursorVisible = false;
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Green;
    
        string[] bootLog = {
            "INIT: Paging disabled...",
            "KERNEL: Loading ramdisk...",
            "NET: Eth0 link up (1000Mbps/Full-Duplex)",
            "DECRYPTING KEYRING... [OK]",
            "MOUNTING VFS... [OK]",
            "STARTING ALMACHAT DAEMON...",
            "CONNECTING TO NEURAL NET..."
        };

        foreach (var line in bootLog)
        {
            Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] {line}");
            Thread.Sleep(new Random().Next(100, 300)); // Случайная задержка
            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r"); // Очистка строки (или можно оставлять скролл)
            Console.WriteLine($"[ OK ] {line}");
        }
        Thread.Sleep(500);
        // =================================
    
        AppStartTime = DateTime.Now;
        try 
        {
            Application.Init();
            top = Application.Top;

            // ВАЖНО: Если ThemeManager.cs не обновлен, здесь упадет ошибка
            UpdateAppTheme(AppTheme.CyberPunk);
            
            ShowAuthWindow();
            
            _ = StartSystemMonitor();
            _ = CheckForUpdatesAsync(silent: true);
            
            Application.Run();
        }
        catch (Exception ex)
        {
            Application.Shutdown();
            // Вывод ошибки в стандартную консоль
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n================ CRITICAL ERROR ================");
            Console.WriteLine(ex.ToString()); // Полный стек трейс
            Console.WriteLine("================================================");
            Console.ResetColor();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey(); // Ждем, пока пользователь прочитает
        }
        finally
        {
            // Убрали Console.Clear(), чтобы не стирать ошибку
            Application.Shutdown();
            Console.ResetColor();
            Console.CursorVisible = true;
            Console.WriteLine("System shutdown complete.");
        }
    }

    // ================== THEME & VISUALS ==================

    static void UpdateAppTheme(AppTheme theme)
    {
        ThemeManager.Apply(theme);

        if (mainWin != null) 
        {
            mainWin.ColorScheme = ThemeManager.BaseScheme;
            
            // Обновляем панели
            if(topPanel != null) topPanel.ColorScheme = ThemeManager.BaseScheme;
            if(leftPanel != null) leftPanel.ColorScheme = ThemeManager.BaseScheme;
            if(rightPanel != null) rightPanel.ColorScheme = ThemeManager.BaseScheme;
            if(inputPanel != null) inputPanel.ColorScheme = ThemeManager.BaseScheme;

            // Обновляем списки и ввод
            if(chatListView != null) chatListView.ColorScheme = ThemeManager.ListScheme;
            if(messagesListView != null) messagesListView.ColorScheme = ThemeManager.BaseScheme;
            if(inputField != null) inputField.ColorScheme = ThemeManager.InputScheme;
            
            // Обновляем инфо-элементы
            if(lblPing != null) {
                lblPing.ColorScheme = ThemeManager.InfoScheme;
                lblRam.ColorScheme = ThemeManager.InfoScheme;
                lblUptime.ColorScheme = ThemeManager.AccentScheme;
                
                // Графики получают цвета акцентов или ошибок для контраста
                if(ramGraph != null) ramGraph.ColorScheme = ThemeManager.ErrorScheme; 
                if(pingGraph != null) pingGraph.ColorScheme = ThemeManager.InputScheme;
            }
        }

        if (authWin != null) 
        {
            authWin.ColorScheme = ThemeManager.BaseScheme;
            foreach(var v in authWin.Subviews) {
                if(v is FrameView) v.ColorScheme = ThemeManager.BaseScheme;
                // Кнопки и поля обновляем при перерисовке диалога, 
                // но базовую схему задаем здесь для фона
            }
        }
        
        if(menuBar != null) menuBar.ColorScheme = ThemeManager.HeaderScheme;

        Application.Refresh();
    }

    // ================== MAIN UI LAYOUT (DASHBOARD STYLE) ==================

        static void ShowMainWindow()
    {
        // ИСПРАВЛЕНИЕ: Убрали 'Border = null'.
        mainWin = new Window() { 
            X = 0, Y = 1, 
            Width = Dim.Fill(), Height = Dim.Fill(), 
            ColorScheme = ThemeManager.BaseScheme 
        };
        
        // Правильное скрытие рамки
        if (mainWin.Border != null)
            mainWin.Border.BorderStyle = BorderStyle.None;

        // 1. MENU BAR
        menuBar = new MenuBar(new MenuBarItem[] {
            new MenuBarItem (" ALMACHAT ", new MenuItem [] { 
                new MenuItem ("_Settings", "Profile", () => ShowProfileDialog(false)),
                new MenuItem ("_Theme", "UI Style", () => ThemeManager.ShowSelector(UpdateAppTheme)),
                new MenuItem ("_Exit", "Quit App", () => Application.RequestStop())
            }),
            new MenuBarItem (" NET ", new MenuItem [] {
                new MenuItem ("_Search", "Find User", () => ShowLiveSearchDialog()),
                new MenuItem ("_New Group", "Create", () => ShowCreateGroupDialog()),
                new MenuItem ("_Refresh", "Reload", async () => await LoadChatsAsync())
            }),
            new MenuBarItem (" CHAT ", new MenuItem[] {
                new MenuItem ("_Info", "Participants", () => ShowChatInfo()),
                new MenuItem ("_Rename", "Edit Group", () => ShowRenameGroupDialog()),
                new MenuItem ("_Delete", "Remove Chat", () => DeleteCurrentChat())
            })
        });
        top.Add(menuBar);

        // 2. TOP PANEL (SYSTEM STATUS)
        topPanel = new FrameView(" SYSTEM STATUS ") { X = 0, Y = 0, Width = Dim.Fill(), Height = 5 };
        
        // User Info
        var lblUserTitle = new Label("USR:") { X = 1, Y = 0, ColorScheme = ThemeManager.InfoScheme };
        var lblUser = new Label(CurrentUserEmail) { X = 6, Y = 0, ColorScheme = ThemeManager.BaseScheme };
        var lblVer = new Label(CurrentAppVersion) { X = 6, Y = 1, ColorScheme = ThemeManager.InfoScheme };

        // Graphs & Stats (Right aligned)
        // Memory
        topPanel.Add(new Label("MEM:") { X = Pos.AnchorEnd(35), Y = 0, ColorScheme = ThemeManager.InfoScheme });
        ramGraph = new ProgressBar() { X = Pos.AnchorEnd(30), Y = 0, Width = 15, Fraction = 0f, ColorScheme = ThemeManager.ErrorScheme };
        lblRam = new Label("0 MB") { X = Pos.AnchorEnd(13), Y = 0, ColorScheme = ThemeManager.BaseScheme };

        // Network
        topPanel.Add(new Label("PING:") { X = Pos.AnchorEnd(35), Y = 1, ColorScheme = ThemeManager.InfoScheme });
        pingGraph = new ProgressBar() { X = Pos.AnchorEnd(30), Y = 1, Width = 15, Fraction = 0f, ColorScheme = ThemeManager.InputScheme };
        lblPing = new Label("0 ms") { X = Pos.AnchorEnd(13), Y = 1, ColorScheme = ThemeManager.BaseScheme };

        // Status & Uptime
        lblUptime = new Label("UP: 00:00:00") { X = Pos.AnchorEnd(13), Y = 2, ColorScheme = ThemeManager.AccentScheme };
        wsStatusLabel = new Label("OFFLINE") { X = Pos.Center(), Y = 0, ColorScheme = ThemeManager.ErrorScheme };
        notifLabel = new Label("") { X = Pos.Center(), Y = 2, Width = 40, TextAlignment = TextAlignment.Centered, ColorScheme = ThemeManager.NotifScheme };

        topPanel.Add(lblUserTitle, lblUser, lblVer, ramGraph, lblRam, pingGraph, lblPing, lblUptime, wsStatusLabel, notifLabel);
        mainWin.Add(topPanel);

        // 3. LEFT PANEL (CHANNELS)
        leftPanel = new FrameView(" CHANNELS ") { X = 0, Y = 5, Width = 30, Height = Dim.Fill(), ColorScheme = ThemeManager.BaseScheme };
        // Отступы для списка, чтобы не прилипал к рамке
        chatListView = new ListView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = ThemeManager.ListScheme };
        chatListView.SelectedItemChanged += async _ => await OnChatSelected();
        leftPanel.Add(chatListView);
        mainWin.Add(leftPanel);

        // 4. RIGHT PANEL (MESSAGES)
        rightPanel = new FrameView(" DATA STREAM ") { X = 30, Y = 5, Width = Dim.Fill(), Height = Dim.Fill() - 3, ColorScheme = ThemeManager.BaseScheme };
        // X = 1 для отступа текста сообщений
        messagesListView = new ListView() { X = 1, Y = 0, Width = Dim.Fill() - 2, Height = Dim.Fill(), ColorScheme = ThemeManager.BaseScheme };
        
        messagesListView.KeyPress += (e) => {
            if (ActiveChat == null || CurrentMessages.Count == 0) return;
            var idx = messagesListView.SelectedItem;
            if (idx >= 0 && idx < CurrentMessages.Count) {
                if (CurrentMessages[idx].SenderId == CurrentUserId && (e.KeyEvent.Key == Key.DeleteChar || e.KeyEvent.Key == Key.Backspace)) {
                    if (MessageBox.Query("CONFIRM", "Delete entity?", "Yes", "No") == 0) _ = DeleteMessage(CurrentMessages[idx].Id);
                    e.Handled = true;
                }
            }
        };
        rightPanel.Add(messagesListView);
        mainWin.Add(rightPanel);

        // 5. BOTTOM PANEL (INPUT)
        inputPanel = new FrameView() { X = 30, Y = Pos.AnchorEnd(3), Width = Dim.Fill(), Height = 3, ColorScheme = ThemeManager.BaseScheme };
        var prompt = new Label($"{ThemeManager.PromptSymbol}_") 
        { 
            X = 1, Y = 0, 
            ColorScheme = ThemeManager.AccentScheme 
        };

        inputField = new TextField("") { X = 4, Y = 0, Width = Dim.Fill() - 10, ColorScheme = ThemeManager.InputScheme };
        var btnSend = new Button("[SEND]") { X = Pos.AnchorEnd(8), Y = 0, ColorScheme = ThemeManager.ListScheme };
        
        inputPanel.Add(prompt, inputField, btnSend);
        mainWin.Add(inputPanel);
        
        top.Add(mainWin);

        // Logic
        btnSend.Clicked += () => _ = SendMessageAction();
        inputField.KeyPress += (e) => { if(e.KeyEvent.Key == Key.Enter) { _ = SendMessageAction(); e.Handled = true; } };
    }

    // ================== MONITORING ==================

    static async Task StartSystemMonitor()
    {
        while (true)
        {
            try
            {
                var uptime = DateTime.Now - AppStartTime;
                string uptimeStr = $"{uptime:hh\\:mm\\:ss}";

                using var proc = Process.GetCurrentProcess();
                long mem = proc.WorkingSet64 / 1024 / 1024; // MB
                float memFraction = Math.Min(1.0f, mem / 250.0f); // Scale relative to 250MB

                Stopwatch sw = Stopwatch.StartNew();
                long ms = 0;
                try {
                    using var msg = new HttpRequestMessage(HttpMethod.Head, BaseUrl);
                    var resp = await http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
                    sw.Stop();
                    ms = sw.ElapsedMilliseconds;
                } catch { ms = 999; }
                float pingFraction = Math.Min(1.0f, ms / 300.0f); // Scale relative to 300ms

                if (mainWin != null && lblPing != null)
                {
                    Application.MainLoop.Invoke(() => {
                        lblUptime.Text = uptimeStr;
                        lblRam.Text = $"{mem} MB";
                        if(ramGraph != null) ramGraph.Fraction = memFraction;

                        lblPing.Text = $"{ms} ms";
                        if(pingGraph != null) pingGraph.Fraction = pingFraction;
                    });
                }
            }
            catch {}
            await Task.Delay(2000); 
        }
    }

    static void UpdateWsStatus(bool online)
    {
        Application.MainLoop.Invoke(() => {
            if (wsStatusLabel != null) {
                wsStatusLabel.Text = online ? " [ ONLINE ] " : " [ OFFLINE ] ";
                wsStatusLabel.ColorScheme = online ? 
                    new ColorScheme{Normal=Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen)} : 
                    ThemeManager.ErrorScheme;
            }
        });
    }

    // ================== AUTH WINDOW ==================

    static void ShowAuthWindow()
    {
        if (mainWin != null) top.Remove(mainWin);
        
        // ИСПРАВЛЕНИЕ: Убрали 'Border = null'.
        authWin = new Window() { 
            X = 0, Y = 0, 
            Width = Dim.Fill(), Height = Dim.Fill(), 
            ColorScheme = ThemeManager.BaseScheme 
        };
        
        // Чтобы убрать рамку правильно, используем BorderStyle.None
        if (authWin.Border != null) 
            authWin.Border.BorderStyle = BorderStyle.None;
        
        // Centered Frame
        var frame = new FrameView(" ACCESS CONTROL ") { 
            X = Pos.Center(), Y = Pos.Center(), Width = 50, Height = 14, 
            ColorScheme = ThemeManager.BaseScheme 
        };

        var userField = new TextField("") { X = 12, Y = 2, Width = 30, ColorScheme = ThemeManager.InputScheme };
        var passField = new TextField("") { X = 12, Y = 4, Width = 30, Secret = true, ColorScheme = ThemeManager.InputScheme };
        
        var btnLogin = new Button(" LOGIN ") { X = 10, Y = 8, ColorScheme = ThemeManager.ListScheme };
        var btnReg = new Button(" REGISTER ") { X = 25, Y = 8, ColorScheme = ThemeManager.BaseScheme };

        btnLogin.Clicked += async () => await PerformAuth(userField.Text.ToString(), passField.Text.ToString(), false);
        btnReg.Clicked += async () => await PerformAuth(userField.Text.ToString(), passField.Text.ToString(), true);

        frame.Add(new Label("EMAIL  :") { X = 2, Y = 2 }, userField, new Label("PASS   :") { X = 2, Y = 4 }, passField, btnLogin, btnReg);
        authWin.Add(frame);
        authWin.Add(new Label("Unauthorized access is prohibited.") { X = Pos.Center(), Y = Pos.AnchorEnd(2), ColorScheme = ThemeManager.ErrorScheme });
        
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
            
            if (openProfileSettings) ShowProfileDialog(isFirstSetup: true);
            else ShowMainWindow();

            await LoadChatsAsync();
            await ConnectNotifWsAsync();
        }
        catch (Exception ex) { MessageBox.ErrorQuery("CRITICAL ERROR", ex.Message, "OK"); }
    }

    // ================== DIALOGS (STYLED) ==================

    static void ShowVerificationDialog(string email, string password)
    {
        var d = new Dialog(" :: VERIFICATION :: ", 60, 10);
        d.ColorScheme = ThemeManager.BaseScheme;
        
        var lblInfo = new Label($"Code sent to: {email}") { X = 1, Y = 1, Width = Dim.Fill() - 2 };
        var codeField = new TextField("") { X = Pos.Center(), Y = 4, Width = 10, ColorScheme = ThemeManager.InputScheme };
        
        var btnConfirm = new Button(" CONFIRM ") { X = Pos.Center() - 14, Y = 7, ColorScheme = ThemeManager.ListScheme };
        var btnResend = new Button(" RESEND ") { X = Pos.Center() + 2, Y = 7, ColorScheme = ThemeManager.ErrorScheme };

        btnConfirm.Clicked += async () => {
            string code = codeField.Text.ToString().Trim();
            if (code.Length < 6) return;
            if (await ConfirmEmailAction(email, code)) {
                Application.RequestStop();
                MessageBox.Query("Success", "Access Granted", "OK");
                await PerformAuth(email, password, isRegister: false, openProfileSettings: true); 
            }
        };
        btnResend.Clicked += async () => await ResendCodeAction(email);
        d.Add(lblInfo, codeField, btnConfirm, btnResend);
        codeField.SetFocus();
        Application.Run(d);
    }

    static void ShowLiveSearchDialog()
    {
        var d = new Dialog(" :: DATABASE SEARCH :: ", 60, 18);
        d.ColorScheme = ThemeManager.BaseScheme;

        // Отступы X=1
        var searchField = new TextField() { X = 1, Y = 1, Width = Dim.Fill() - 2, ColorScheme = ThemeManager.InputScheme };
        var resultsList = new ListView() { X = 1, Y = 3, Width = Dim.Fill() - 2, Height = Dim.Fill() - 2, ColorScheme = ThemeManager.ListScheme };
        
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
                    Application.MainLoop.Invoke(() => resultsList.SetSource(foundUsers.Select(u => $" > {u.Username} [{u.Id}]").ToList()));
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

        d.Add(new Label("Query:") { X=1, Y=0 }, searchField, resultsList);
        Application.Run(d);
        cts.Cancel();
    }

    static void ShowProfileDialog(bool isFirstSetup = false)
    {
        var d = new Dialog(" :: PROFILE CONFIG :: ", 60, 20);
        d.ColorScheme = ThemeManager.BaseScheme;
        
        var tfUser = new TextField("") { X = 14, Y = 1, Width = 40, ColorScheme = ThemeManager.InputScheme };
        var tfFirst = new TextField("") { X = 14, Y = 3, Width = 40, ColorScheme = ThemeManager.InputScheme };
        var tfLast = new TextField("") { X = 14, Y = 5, Width = 40, ColorScheme = ThemeManager.InputScheme };

        d.Add(new Label("Username:") { X = 2, Y = 1 }, tfUser,
              new Label("First Name:") { X = 2, Y = 3 }, tfFirst,
              new Label("Last Name:") { X = 2, Y = 5 }, tfLast);

        var btnSave = new Button(" SAVE DATA ") { X = Pos.Center(), Y = 14, ColorScheme = ThemeManager.ListScheme };
        
        if (isFirstSetup) {
            var btnSkip = new Button(" SKIP >> ") { X = Pos.AnchorEnd(12), Y = 14 };
            btnSkip.Clicked += () => { Application.RequestStop(); ShowMainWindow(); };
            d.Add(btnSkip);
        }

        // Загрузка данных (в фоне)
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
                    });
                }
            } catch {}
        });

        btnSave.Clicked += async () => {
            var updatePayload = new { id = CurrentUserId, username = tfUser.Text.ToString(), email = CurrentUserEmail, first_name = tfFirst.Text.ToString(), last_name = tfLast.Text.ToString() };
            try {
                var content = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");
                var res = await http.PutAsync($"{BaseUrl}/api/Users/{CurrentUserId}", content);
                if (res.IsSuccessStatusCode) {
                    MessageBox.Query("Success", "Profile updated", "OK");
                    Application.RequestStop();
                    if (isFirstSetup) ShowMainWindow(); 
                }
            } catch (Exception ex) { MessageBox.ErrorQuery("Error", ex.Message, "OK"); }
        };
        d.Add(btnSave);
        Application.Run(d);
    }

    static void ShowCreateGroupDialog()
    {
        var d = new Dialog(" :: NEW GROUP :: ", 50, 14);
        d.ColorScheme = ThemeManager.BaseScheme;
        var nameField = new TextField("") { X = 1, Y = 2, Width = Dim.Fill() - 2, ColorScheme = ThemeManager.InputScheme };
        var idsField = new TextField("") { X = 1, Y = 5, Width = Dim.Fill() - 2, ColorScheme = ThemeManager.InputScheme };
        var btnCreate = new Button(" INIT ") { X = Pos.Center(), Y = 8, ColorScheme = ThemeManager.ListScheme };
        btnCreate.Clicked += () => {
            var name = nameField.Text.ToString();
            var idsStr = idsField.Text.ToString();
            var ids = idsStr.Split(',').Select(s => s.Trim()).Where(s => long.TryParse(s, out _)).Select(long.Parse).ToList();
            if(!string.IsNullOrWhiteSpace(name) && ids.Count > 0) _ = CreateGroup(name, ids);
            Application.RequestStop();
        };
        d.Add(new Label("Group Name:"){X=1, Y=1}, nameField, new Label("IDs (comma separated):"){X=1, Y=4}, idsField, btnCreate);
        Application.Run(d);
    }

    // ================== LOGIC & HELPERS ==================

    static async Task<bool> ConfirmEmailAction(string email, string code)
    {
        var payload = new { email = email, code = code };
        try {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await http.PostAsync($"{BaseUrl}/api/Login/confirm", content);
            if (!res.IsSuccessStatusCode) {
                string msg = await res.Content.ReadAsStringAsync();
                MessageBox.ErrorQuery("Failed", msg, "OK");
                return false;
            }
            return true;
        } catch { return false; }
    }

    static async Task ResendCodeAction(string email)
    {
        try {
            var content = new StringContent(JsonSerializer.Serialize(new { email = email }), Encoding.UTF8, "application/json");
            await http.PostAsync($"{BaseUrl}/api/Login/resend-code", content);
            MessageBox.Query("Sent", "Code resent", "OK");
        } catch {}
    }

    static async Task CreatePrivateChat(string username)
    {
        try {
            var res = await http.PostAsync($"{BaseUrl}/api/Chat/create_chat?username={username}", null);
            if(res.IsSuccessStatusCode) { await LoadChatsAsync(); MessageBox.Query("OK", $"Chat with {username} active", "OK"); }
        } catch {}
    }

    static async Task CreateGroup(string name, List<long> memberIds)
    {
        try {
            var content = new StringContent(JsonSerializer.Serialize(new { Name = name, MemberIds = memberIds }), Encoding.UTF8, "application/json");
            var res = await http.PostAsync($"{BaseUrl}/api/Chat/create_group", content);
            if(res.IsSuccessStatusCode) await LoadChatsAsync();
        } catch {}
    }

    static void ShowRenameGroupDialog()
    {
        if (ActiveChat == null || !ActiveChat.IsGroup) return;
        var d = new Dialog(" :: EDIT GROUP :: ", 50, 8);
        d.ColorScheme = ThemeManager.BaseScheme;
        var tf = new TextField(ActiveChat.Name ?? "") { X=1, Y=2, Width=Dim.Fill()-2, ColorScheme = ThemeManager.InputScheme };
        var btn = new Button(" SAVE "){X=Pos.Center(), Y=4, ColorScheme = ThemeManager.ListScheme };
        btn.Clicked += () => { _ = RenameGroupReq(ActiveChat.Id, tf.Text.ToString()); Application.RequestStop(); };
        d.Add(tf, btn);
        Application.Run(d);
    }

    static async Task RenameGroupReq(long chatId, string newName)
    {
        try {
             var content = new StringContent(JsonSerializer.Serialize(new { NewName = newName }), Encoding.UTF8, "application/json");
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
        if(MessageBox.Query("Delete", $"Purge channel?", "Yes", "No") == 1) return;
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
        var list = ActiveChat.Participants.Select(p => $" > [{p.UserId}] {p.Username}").ToList();
        var d = new Dialog($" INFO: {list.Count} users ", 50, 15);
        d.ColorScheme = ThemeManager.BaseScheme;
        var lv = new ListView(list) { X=1, Y=1, Width=Dim.Fill()-2, Height=Dim.Fill()-2, ColorScheme = ThemeManager.ListScheme };
        d.Add(lv);
        Application.Run(d);
    }

    static async Task CheckForUpdatesAsync(bool silent)
    {
        try {
            using var updateClient = new HttpClient();
            updateClient.DefaultRequestHeaders.UserAgent.ParseAdd("AlmaChat-Terminal-Client");
            var res = await updateClient.GetAsync($"https://api.github.com/repos/{GithubOwner}/{GithubRepo}/releases/latest");
            if (!res.IsSuccessStatusCode) return;
            var json = await res.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubReleaseDto>(json, JsonOpts);
            if (release != null && release.TagName != CurrentAppVersion && !silent) {
                 Application.MainLoop.Invoke(() => MessageBox.Query("Update", $"New: {release.TagName}", "OK"));
            }
        } catch {}
    }

    static void SendSystemNotification(string title, string message)
    {
        Task.Run(() => {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Process.Start("notify-send", $"\"{title}\" \"{message}\"");
            } catch {}
        });
    }

    static void ShowNotification(string text)
    {
        Application.MainLoop.Invoke(() => {
            if (notifLabel != null) {
                notifLabel.Text = $" [!] {text} ";
                notifLabel.ColorScheme = ThemeManager.NotifScheme;
            }
            SendSystemNotification("AlmaChat", text);
            Task.Delay(5000).ContinueWith(_ => Application.MainLoop.Invoke(() => {
                if (notifLabel != null) { notifLabel.Text = ""; notifLabel.ColorScheme = ThemeManager.BaseScheme; }
            }));
        });
    }

    static async Task ConnectNotifWsAsync()
    {
        wsNotifCts?.Cancel(); wsNotif?.Dispose();
        wsNotifCts = new CancellationTokenSource();
        wsNotif = new ClientWebSocket();
        if(!string.IsNullOrEmpty(AuthCookieValue)) wsNotif.Options.SetRequestHeader("Cookie", AuthCookieValue);
        try { await wsNotif.ConnectAsync(new Uri(WsNotifUrl), wsNotifCts.Token); _ = NotifReceiveLoop(); } catch {}
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
                    if(type == "ping") await wsNotif.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"pong\"}"), WebSocketMessageType.Text, true, CancellationToken.None);
                    else if(type == "notification") ShowNotification(root.GetProperty("body").GetString() ?? "");
                    else if (type == "online_list") {
                        OnlineUsers = new HashSet<long>(root.GetProperty("ids").EnumerateArray().Select(x => x.GetInt64()));
                        RenderChatList();
                    }
                    else if (type == "presence") {
                         long uid = root.GetProperty("user_id").GetInt64();
                         if(root.GetProperty("is_online").GetBoolean()) OnlineUsers.Add(uid); else OnlineUsers.Remove(uid);
                         RenderChatList();
                    }
                }
            } catch { break; }
        }
    }

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
            if (res.IsSuccessStatusCode) {
                var json = await res.Content.ReadAsStringAsync();
                CurrentMessages = JsonSerializer.Deserialize<List<MessageDto>>(json, JsonOpts) ?? new();
                RefreshMessageList();
            }
        } catch {}
    }

    static void RefreshMessageList()
    {
        var displayList = CurrentMessages.Select(m => {
            string cleanText = m.Content.Trim();
            if (cleanText.StartsWith("{")) {
                try { 
                    cleanText = JsonDocument.Parse(cleanText)
                        .RootElement.GetProperty("content").GetString() ?? cleanText; 
                } catch {}
            }
        
            string prefix = ThemeManager.MessagePrefix;
        
            if (m.SenderId == CurrentUserId) 
                return $" {prefix} [YOU]: {cleanText}";

            string name = m.SenderName;
            if (string.IsNullOrEmpty(name) && ActiveChat != null) {
                var participant = ActiveChat.Participants
                    .FirstOrDefault(p => p.UserId == m.SenderId.ToString());
                if (participant != null) name = participant.Username;
            }
            if (string.IsNullOrEmpty(name)) name = $"ID:{m.SenderId}";

            return $" {prefix} [{name}]: {cleanText}";
        }).ToList();

        Application.MainLoop.Invoke(() => {
            messagesListView.SetSource(displayList);
            if(displayList.Count > 0) messagesListView.SelectedItem = displayList.Count - 1;
        });
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
                    if (t.GetString() == "ping") await wsChat.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"pong\"}"), WebSocketMessageType.Text, true, CancellationToken.None);
                    else if (t.GetString() == "message") {
                         CurrentMessages.Add(new MessageDto { 
                             Id = root.GetProperty("id").GetInt64(), 
                             SenderId = root.GetProperty("sender_id").GetInt64(), 
                             SenderName = root.TryGetProperty("sender", out var s) ? s.GetString() : null, 
                             Content = root.GetProperty("content").GetString() 
                         });
                         RefreshMessageList();
                    }
                }
            } catch { break; }
        }
        UpdateWsStatus(false);
    }

    static async Task SendMessageAction()
    {
        if (ActiveChat == null) return;
        var text = inputField.Text.ToString();
        if (string.IsNullOrWhiteSpace(text)) return;
        inputField.Text = "";
        if (wsChat == null || wsChat.State != WebSocketState.Open) { MessageBox.ErrorQuery("Err", "Offline", "Ok"); return; }
        try {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type = "message", content = text }));
            await wsChat.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        } catch {}
    }

    static async Task LoadChatsAsync()
    {
        try {
            var res = await http.GetAsync($"{BaseUrl}/api/Chat/chats");
            if (res.IsSuccessStatusCode) {
                var json = await res.Content.ReadAsStringAsync();
                Chats = JsonSerializer.Deserialize<List<ChatDto>>(json, JsonOpts) ?? new();
                RenderChatList();
            }
        } catch {}
    }

    static void RenderChatList()
    {
        Application.MainLoop.Invoke(() => {
            if (chatListView == null) return;
            chatListView.SetSource(Chats.Select(c => {
                string prefix = c.IsGroup 
                    ? $" {ThemeManager.GroupSymbol} " 
                    : (OnlineUsers.Contains(c.GetOtherUserId(CurrentUserId)) 
                        ? $" {ThemeManager.OnlineSymbol} " 
                        : $" {ThemeManager.OfflineSymbol} ");
                return $"{prefix}{c.DisplayName(CurrentUserId)}".PadRight(24);
            }).ToList());
        });
    }
}