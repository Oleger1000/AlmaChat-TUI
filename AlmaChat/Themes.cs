using NStack;
using Terminal.Gui;

namespace AlmaChat
{
    public enum AppTheme 
    { 
        CyberPunk,      // Неоновый киберпанк
        HighContrast,   // Высокий контраст (accessibility)
        Catppuccin,     // Пастельные тона
        Gruvbox,        // Тёплый ретро
        Monokai,        // Классика программиста
    }

    public static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; }

        // Основные схемы
        public static ColorScheme BaseScheme { get; private set; }
        public static ColorScheme AccentScheme { get; private set; }
        public static ColorScheme InputScheme { get; private set; }
        public static ColorScheme ListScheme { get; private set; }
        public static ColorScheme ErrorScheme { get; private set; }
        public static ColorScheme NotifScheme { get; private set; }
        public static ColorScheme InfoScheme { get; private set; }
        public static ColorScheme HeaderScheme { get; private set; }
        
        // Дополнительные схемы для более богатого UI
        public static ColorScheme SuccessScheme { get; private set; }
        public static ColorScheme WarningScheme { get; private set; }
        public static ColorScheme PanelScheme { get; private set; }
        public static ColorScheme ButtonScheme { get; private set; }
        public static ColorScheme DialogScheme { get; private set; }
        
        // Символы для рамок (меняются в зависимости от темы)
        public static char BorderVertical { get; private set; } = '│';
        public static char BorderHorizontal { get; private set; } = '─';
        public static char BorderCornerTL { get; private set; } = '┌';
        public static char BorderCornerTR { get; private set; } = '┐';
        public static char BorderCornerBL { get; private set; } = '└';
        public static char BorderCornerBR { get; private set; } = '┘';
        
        // Декоративные символы
        public static string PromptSymbol { get; private set; } = "❯";
        public static string OnlineSymbol { get; private set; } = "●";
        public static string OfflineSymbol { get; private set; } = "○";
        public static string GroupSymbol { get; private set; } = "◆";
        public static string MessagePrefix { get; private set; } = "»";

        public static void Apply(AppTheme theme)
        {
            CurrentTheme = theme;
            var driver = Application.Driver;
            
            if (driver == null) return;

            switch (theme)
            {
                case AppTheme.CyberPunk:
                    ApplyCyberPunk(driver);
                    break;
                case AppTheme.HighContrast:
                    ApplyHighContrast(driver);
                    break;
                case AppTheme.Catppuccin:
                    ApplyCatppuccin(driver);
                    break;

                case AppTheme.Gruvbox:
                    ApplyGruvbox(driver);
                    break;
                case AppTheme.Monokai:
                    ApplyMonokai(driver);
                    break;

            }
        }

        // ═══════════════════════════════════════════════════════════════
        // CYBERPUNK - Неоновый стиль: чёрный фон, яркие циан/маджента/зелёный
        // ═══════════════════════════════════════════════════════════════
        private static void ApplyCyberPunk(ConsoleDriver driver)
        {
            var bg = Color.Black;
            
            BaseScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Cyan, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightCyan),
                HotNormal = driver.MakeAttribute(Color.BrightMagenta, bg),
                HotFocus = driver.MakeAttribute(Color.Black, Color.BrightMagenta),
                Disabled = driver.MakeAttribute(Color.DarkGray, bg)
            };
            
            InputScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightGreen, Color.DarkGray), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightGreen),
                HotNormal = driver.MakeAttribute(Color.BrightYellow, Color.DarkGray),
                Disabled = driver.MakeAttribute(Color.Gray, Color.DarkGray)
            };
            
            ListScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Gray, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightCyan),
                HotNormal = driver.MakeAttribute(Color.BrightMagenta, bg),
                HotFocus = driver.MakeAttribute(Color.White, Color.Magenta)
            };
            
            AccentScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightMagenta, bg),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightMagenta)
            };
            
            HeaderScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.BrightMagenta),
                Focus = driver.MakeAttribute(Color.White, Color.Magenta),
                HotNormal = driver.MakeAttribute(Color.BrightYellow, Color.BrightMagenta),
                HotFocus = driver.MakeAttribute(Color.BrightYellow, Color.Magenta)
            };
            
            InfoScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.DarkGray, bg) };
            ErrorScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightRed, bg) };
            NotifScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Black, Color.BrightYellow) };
            SuccessScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightGreen, bg) };
            WarningScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightYellow, bg) };
            
            PanelScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Cyan, bg),
                Focus = driver.MakeAttribute(Color.BrightCyan, bg)
            };
            
            ButtonScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.Cyan),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightCyan),
                HotNormal = driver.MakeAttribute(Color.BrightMagenta, Color.Cyan),
                HotFocus = driver.MakeAttribute(Color.BrightMagenta, Color.BrightCyan)
            };
            
            DialogScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightCyan, Color.DarkGray),
                Focus = driver.MakeAttribute(Color.White, Color.DarkGray)
            };

            // Киберпанк символы
            PromptSymbol = "⟫";
            OnlineSymbol = "◉";
            OfflineSymbol = "◎";
            GroupSymbol = "⬡";
            MessagePrefix = "▸";
            BorderVertical = '║';
            BorderHorizontal = '═';
        }

        // ═══════════════════════════════════════════════════════════════
        // HIGH CONTRAST - Для доступности: чистый чёрно-белый
        // ═══════════════════════════════════════════════════════════════
        private static void ApplyHighContrast(ConsoleDriver driver)
        {
            var bg = Color.Black;
            var fg = Color.White;
            
            BaseScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(fg, bg), 
                Focus = driver.MakeAttribute(bg, fg),
                HotNormal = driver.MakeAttribute(Color.BrightYellow, bg),
                HotFocus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                Disabled = driver.MakeAttribute(Color.Gray, bg)
            };
            
            InputScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.White), 
                Focus = driver.MakeAttribute(Color.White, Color.Blue),
                Disabled = driver.MakeAttribute(Color.Gray, Color.DarkGray)
            };
            
            ListScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(fg, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                HotNormal = driver.MakeAttribute(Color.BrightCyan, bg),
                HotFocus = driver.MakeAttribute(Color.Black, Color.BrightCyan)
            };
            
            AccentScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightYellow, bg),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow)
            };
            
            HeaderScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.White),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                HotNormal = driver.MakeAttribute(Color.Blue, Color.White),
                HotFocus = driver.MakeAttribute(Color.Blue, Color.BrightYellow)
            };
            
            InfoScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Gray, bg) };
            ErrorScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.White, Color.Red) };
            NotifScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Black, Color.BrightCyan) };
            SuccessScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Black, Color.BrightGreen) };
            WarningScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Black, Color.BrightYellow) };
            
            PanelScheme = BaseScheme;
            ButtonScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.Gray),
                Focus = driver.MakeAttribute(Color.Black, Color.White)
            };
            DialogScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.White, Color.DarkGray)
            };

            PromptSymbol = ">";
            OnlineSymbol = "[+]";
            OfflineSymbol = "[-]";
            GroupSymbol = "[#]";
            MessagePrefix = ":";
            BorderVertical = '|';
            BorderHorizontal = '-';
        }
        

        // ═══════════════════════════════════════════════════════════════
        // CATPPUCCIN - Пастельные розовые/голубые тона
        // ═══════════════════════════════════════════════════════════════
        private static void ApplyCatppuccin(ConsoleDriver driver)
        {
            var bg = Color.Black;
            
            BaseScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightBlue, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightMagenta),
                HotNormal = driver.MakeAttribute(Color.BrightMagenta, bg),
                HotFocus = driver.MakeAttribute(Color.White, Color.Magenta),
                Disabled = driver.MakeAttribute(Color.DarkGray, bg)
            };
            
            InputScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightMagenta, Color.DarkGray), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightMagenta),
                Disabled = driver.MakeAttribute(Color.Gray, Color.DarkGray)
            };
            
            ListScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Cyan, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightBlue),
                HotNormal = driver.MakeAttribute(Color.BrightMagenta, bg),
                HotFocus = driver.MakeAttribute(Color.BrightMagenta, Color.BrightBlue)
            };
            
            AccentScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightMagenta, bg),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightMagenta)
            };
            
            HeaderScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.White, Color.Magenta),
                Focus = driver.MakeAttribute(Color.White, Color.BrightMagenta),
                HotNormal = driver.MakeAttribute(Color.BrightYellow, Color.Magenta)
            };
            
            InfoScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Blue, bg) };
            ErrorScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightRed, bg) };
            NotifScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Black, Color.BrightCyan) };
            SuccessScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightGreen, bg) };
            WarningScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightYellow, bg) };
            
            PanelScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Magenta, bg) };
            ButtonScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.BrightBlue),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightMagenta)
            };
            DialogScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightBlue, Color.DarkGray) };

            PromptSymbol = "♥";
            OnlineSymbol = "✿";
            OfflineSymbol = "❀";
            GroupSymbol = "✦";
            MessagePrefix = "~";
            BorderVertical = '┃';
            BorderHorizontal = '━';
        }

        

        // ═══════════════════════════════════════════════════════════════
        // GRUVBOX - Тёплые ретро цвета: оранжевый/коричневый/зелёный
        // ═══════════════════════════════════════════════════════════════
        private static void ApplyGruvbox(ConsoleDriver driver)
        {
            var bg = Color.Black;
            
            BaseScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Brown, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.Brown),
                HotNormal = driver.MakeAttribute(Color.BrightYellow, bg),
                HotFocus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                Disabled = driver.MakeAttribute(Color.DarkGray, bg)
            };
            
            InputScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightYellow, Color.DarkGray), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                Disabled = driver.MakeAttribute(Color.Gray, Color.DarkGray)
            };
            
            ListScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Gray, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.Green),
                HotNormal = driver.MakeAttribute(Color.BrightRed, bg),
                HotFocus = driver.MakeAttribute(Color.White, Color.Red)
            };
            
            AccentScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightYellow, bg) };
            
            HeaderScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.Brown),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                HotNormal = driver.MakeAttribute(Color.BrightRed, Color.Brown)
            };
            
            InfoScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.DarkGray, bg) };
            ErrorScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightRed, bg) };
            NotifScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Black, Color.BrightGreen) };
            SuccessScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightGreen, bg) };
            WarningScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightYellow, bg) };
            
            PanelScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Green, bg) };
            ButtonScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.Gray),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow)
            };
            DialogScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Brown, Color.DarkGray) };

            PromptSymbol = "λ";
            OnlineSymbol = "⬤";
            OfflineSymbol = "○";
            GroupSymbol = "⌘";
            MessagePrefix = "»";
            BorderVertical = '┃';
            BorderHorizontal = '━';
        }

        // ═══════════════════════════════════════════════════════════════
        // MONOKAI - Классика: зелёный текст, розовые акценты
        // ═══════════════════════════════════════════════════════════════
        private static void ApplyMonokai(ConsoleDriver driver)
        {
            var bg = Color.Black;
            
            BaseScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.BrightGreen, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightGreen),
                HotNormal = driver.MakeAttribute(Color.BrightMagenta, bg),
                HotFocus = driver.MakeAttribute(Color.Black, Color.BrightMagenta),
                Disabled = driver.MakeAttribute(Color.Green, bg)
            };
            
            InputScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.White, Color.DarkGray), 
                Focus = driver.MakeAttribute(Color.BrightYellow, Color.DarkGray),
                Disabled = driver.MakeAttribute(Color.Gray, Color.DarkGray)
            };
            
            ListScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Green, bg), 
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                HotNormal = driver.MakeAttribute(Color.BrightMagenta, bg),
                HotFocus = driver.MakeAttribute(Color.Black, Color.BrightMagenta)
            };
            
            AccentScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightMagenta, bg) };
            
            HeaderScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.BrightGreen),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightYellow),
                HotNormal = driver.MakeAttribute(Color.Magenta, Color.BrightGreen)
            };
            
            InfoScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Gray, bg) };
            ErrorScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightRed, bg) };
            NotifScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Black, Color.BrightMagenta) };
            SuccessScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightGreen, bg) };
            WarningScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightYellow, bg) };
            
            PanelScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.Cyan, bg) };
            ButtonScheme = new ColorScheme 
            { 
                Normal = driver.MakeAttribute(Color.Black, Color.Green),
                Focus = driver.MakeAttribute(Color.Black, Color.BrightGreen)
            };
            DialogScheme = new ColorScheme { Normal = driver.MakeAttribute(Color.BrightGreen, Color.DarkGray) };

            PromptSymbol = "$";
            OnlineSymbol = "[*]";
            OfflineSymbol = "[ ]";
            GroupSymbol = "[+]";
            MessagePrefix = ">";
            BorderVertical = '|';
            BorderHorizontal = '=';
        }
        

        // ═══════════════════════════════════════════════════════════════
        // THEME SELECTOR DIALOG
        // ═══════════════════════════════════════════════════════════════
        public static void ShowSelector(Action<AppTheme> onThemeSelected)
        {
            var d = new Dialog(" ═══ THEME SELECTOR ═══ ", 60, 20);
            if (BaseScheme != null) d.ColorScheme = DialogScheme ?? BaseScheme;

            // Названия тем с эмодзи-индикаторами стиля
            var rGroup = new RadioGroup(new ustring[] {
                " ⚡ CyberPunk    │ Neon future",
                " ◐ High Contrast │ Accessibility",
                " ♥ Catppuccin    │ Pastel dreams",
                " λ Gruvbox       │ Warm retro",
                " $ Monokai       │ Hacker classic",
            }) { 
                X = 2, Y = 1,
                ColorScheme = ListScheme
            };

            rGroup.SelectedItem = (int)CurrentTheme;
            
            // Превью панель
            var previewFrame = new FrameView(" Preview ") 
            { 
                X = 2, Y = 10, 
                Width = Dim.Fill() - 4, 
                Height = 4,
                ColorScheme = PanelScheme ?? BaseScheme
            };
            
            var previewLabel = new Label("Select theme to see colors") 
            { 
                X = 1, Y = 1, 
                Width = Dim.Fill() - 2
            };
            previewFrame.Add(previewLabel);
            
            // Обновление превью при выборе
            rGroup.SelectedItemChanged += (args) => {
                var selected = (AppTheme)args.SelectedItem;
                string preview = selected switch {
                    AppTheme.CyberPunk => "█▓▒░ NEON GLOW ░▒▓█",
                    AppTheme.HighContrast => "████ BLACK & WHITE ████",
                    AppTheme.Catppuccin => "♥♥♥ Pink & Blue ♥♥♥",
                    AppTheme.Gruvbox => "▓▓▓ Warm Autumn ▓▓▓",
                    AppTheme.Monokai => ">>> GREEN TERMINAL <<<",
                    _ => "---"
                };
                previewLabel.Text = preview;
            };

            var btnApply = new Button(" ✓ Apply ") 
            { 
                X = Pos.Center() - 12, 
                Y = 15,
                ColorScheme = ButtonScheme
            };
            
            var btnCancel = new Button(" ✗ Cancel ") 
            { 
                X = Pos.Center() + 2, 
                Y = 15,
                ColorScheme = BaseScheme
            };

            btnApply.Clicked += () => {
                var selected = (AppTheme)rGroup.SelectedItem;
                Apply(selected);
                onThemeSelected?.Invoke(selected);
                Application.RequestStop();
            };
            
            btnCancel.Clicked += () => {
                Application.RequestStop();
            };

            d.Add(rGroup, previewFrame, btnApply, btnCancel);
            Application.Run(d);
        }
        
        // Вспомогательный метод для получения описания темы
        public static string GetThemeDescription(AppTheme theme) => theme switch
        {
            AppTheme.CyberPunk => "Neon cyberpunk aesthetic with cyan/magenta",
            AppTheme.HighContrast => "Maximum readability black/white theme",
            AppTheme.Catppuccin => "Soothing pastel colors",
            AppTheme.Gruvbox => "Warm retro terminal colors",
            AppTheme.Monokai => "Classic programmer theme",
            _ => "Unknown theme"
        };
    }
}